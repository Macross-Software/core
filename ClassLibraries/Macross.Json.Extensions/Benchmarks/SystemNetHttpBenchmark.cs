using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

using BenchmarkDotNet.Attributes;

namespace JsonBenchmarks
{
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

			public byte[]? LargeData { get; set; }
		}

		private static readonly Schema s_Instance = new Schema
		{
			Name = "Test Instance",
			Timestamp = DateTime.UtcNow,
			Id = Guid.NewGuid(),
			LargeData = new byte[1024 * 8]
		};

		private HttpListener? _Server;
		private HttpClient? _Client;

		[GlobalSetup]
		public void GlobalSetup()
		{
			_Server = new HttpListener();
			_Server.Prefixes.Add("http://localhost:8018/benchmark/");
			_Server.Start();

			_Client = new HttpClient();

			ThreadPool.QueueUserWorkItem(ProcessRequests, null);
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

		private void ProcessRequests(object? state)
		{
			try
			{
				while (true)
				{
					HttpListenerContext Context = _Server!.GetContext();

					Context.Response.StatusCode = 200;
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
			string Json = JsonSerializer.Serialize(s_Instance);

			using StringContent Content = new StringContent(Json);

			using HttpResponseMessage response = await _Client!.PostAsync(
				new Uri("http://localhost:8018/benchmark/"),
				Content).ConfigureAwait(false);

			response.EnsureSuccessStatusCode();
		}

		[Benchmark]
		public async Task PostJsonUsingStreamContent()
		{
			using MemoryStream Stream = new MemoryStream();

			await JsonSerializer.SerializeAsync(Stream, s_Instance).ConfigureAwait(false);

			using StreamContent Content = new StreamContent(Stream);

			using HttpResponseMessage response = await _Client!.PostAsync(
				new Uri("http://localhost:8018/benchmark/"),
				Content).ConfigureAwait(false);

			response.EnsureSuccessStatusCode();
		}

		[Benchmark]
		public async Task PostJsonUsingJsonContent()
		{
			using JsonContent<Schema> Content = new JsonContent<Schema>(s_Instance);

			using HttpResponseMessage response = await _Client!.PostAsync(
				new Uri("http://localhost:8018/benchmark/"),
				Content).ConfigureAwait(false);

			response.EnsureSuccessStatusCode();
		}
	}
}
