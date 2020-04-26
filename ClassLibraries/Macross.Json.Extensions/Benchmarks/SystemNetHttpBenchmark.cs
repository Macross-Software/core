using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

using BenchmarkDotNet.Attributes;

namespace JsonBenchmarks
{
	[MinIterationTime(1000)]
	[MemoryDiagnoser]
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
	public class SystemNetHttpBenchmark
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
	{
		private class Schema
		{
			public string? Name { get; set; }

			public DateTime Timestamp { get; set; }

			public Guid Id { get; set; }

			public byte[]? Data { get; set; }
		}

		private static readonly Schema s_LargeInstance = new Schema
		{
			Name = "Test Large Instance",
			Timestamp = DateTime.UtcNow,
			Id = Guid.NewGuid(),
			Data = new byte[1024 * 8]
		};

		private static readonly Schema s_SmallInstance = new Schema
		{
			Name = "Test Small Instance",
			Timestamp = DateTime.UtcNow,
			Id = Guid.NewGuid(),
			Data = new byte[32]
		};

		private static readonly MediaTypeHeaderValue s_JsonContentType = new MediaTypeHeaderValue("application/json")
		{
			CharSet = "utf-8",
		};

		private HttpListener? _Server;
		private HttpClient? _Client;

		[Params(1000)]
		public int NumberOfRequestsPerIteration { get; set; }

		[GlobalSetup]
		public void GlobalSetup()
		{
			// Warm up Json engine internal type cache.
			JsonSerializer.Serialize(s_LargeInstance);

			_Server = new HttpListener();
			_Server.Prefixes.Add("http://localhost:8018/benchmark/");
			_Server.Start();

			_Client = new HttpClient();

			_ = Task.Run(ProcessRequests);
		}

		[GlobalCleanup]
		public void GlobalCleanup()
		{
			if (_Server != null)
			{
				_Server.Stop();
				_Server.Close();
			}

			_Client?.Dispose();
		}

		private async Task ProcessRequests()
		{
			try
			{
				while (true)
				{
					HttpListenerContext Context = await _Server!.GetContextAsync().ConfigureAwait(false);

					Context.Response.StatusCode = 200;
					Context.Request.InputStream.CopyTo(Context.Response.OutputStream); // Echo back the JSON that was sent.
					Context.Response.Close();
				}
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch
#pragma warning restore CA1031 // Do not catch general exception types
			{
			}
		}

		[Benchmark]
		public async Task PostJsonUsingStringContent()
		{
			for (int i = 0; i < NumberOfRequestsPerIteration; i++)
			{
				Schema instance = i % 2 == 0 ? s_LargeInstance : s_SmallInstance;

				string Json = JsonSerializer.Serialize(instance);

				using StringContent Content = new StringContent(Json, Encoding.UTF8, "application/json");

				using HttpResponseMessage response = await _Client!.PostAsync(
					new Uri("http://localhost:8018/benchmark/"),
					Content).ConfigureAwait(false);

				response.EnsureSuccessStatusCode();

				using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
				Schema responseObject = await JsonSerializer.DeserializeAsync<Schema>(responseStream).ConfigureAwait(false);

				if (instance.Id != responseObject.Id || instance.Data?.Length != responseObject.Data?.Length)
					throw new InvalidOperationException();
			}
		}

		[Benchmark]
		public async Task PostJsonUsingStreamContent()
		{
			for (int i = 0; i < NumberOfRequestsPerIteration; i++)
			{
				Schema instance = i % 2 == 0 ? s_LargeInstance : s_SmallInstance;

				using MemoryStream Stream = new MemoryStream();

				await JsonSerializer.SerializeAsync(Stream, instance).ConfigureAwait(false);

				Stream.Seek(0, SeekOrigin.Begin);

				using StreamContent Content = new StreamContent(Stream);

				Content.Headers.ContentType = s_JsonContentType;

				using HttpResponseMessage response = await _Client!.PostAsync(
					new Uri("http://localhost:8018/benchmark/"),
					Content).ConfigureAwait(false);

				response.EnsureSuccessStatusCode();

				using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
				Schema responseObject = await JsonSerializer.DeserializeAsync<Schema>(responseStream).ConfigureAwait(false);

				if (instance.Id != responseObject.Id || instance.Data?.Length != responseObject.Data?.Length)
					throw new InvalidOperationException();
			}
		}

		[Benchmark]
		public async Task PostJsonUsingJsonContent()
		{
			for (int i = 0; i < NumberOfRequestsPerIteration; i++)
			{
				Schema instance = i % 2 == 0 ? s_LargeInstance : s_SmallInstance;

				using JsonContent<Schema> Content = new JsonContent<Schema>(instance);

				using HttpResponseMessage response = await _Client!.PostAsync(
					new Uri("http://localhost:8018/benchmark/"),
					Content).ConfigureAwait(false);

				response.EnsureSuccessStatusCode();

				using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
				Schema responseObject = await JsonSerializer.DeserializeAsync<Schema>(responseStream).ConfigureAwait(false);

				if (instance.Id != responseObject.Id || instance.Data?.Length != responseObject.Data?.Length)
					throw new InvalidOperationException();
			}
		}
	}
}
