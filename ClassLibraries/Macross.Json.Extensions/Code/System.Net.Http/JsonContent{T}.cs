using System.Text.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;

namespace System.Net.Http
{
	/// <summary>
	/// Provides an <see cref="HttpContent"/> implementation for writing an object as JSON.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	public class JsonContent<T> : HttpContent
		where T : class
	{
		private static readonly MediaTypeHeaderValue s_JsonHeader = new MediaTypeHeaderValue("application/json")
		{
			CharSet = "utf-8",
		};

		private readonly T? _Instance;
		private readonly JsonSerializerOptions? _Options;

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonContent{T}"/> class.
		/// </summary>
		/// <param name="instance">Instance to be serialized.</param>
		/// <param name="options"><see cref="JsonSerializerOptions"/>.</param>
		public JsonContent(T? instance, JsonSerializerOptions? options = null)
		{
			_Instance = instance;
			_Options = options;

			Headers.ContentType = s_JsonHeader;
		}

		/// <inheritdoc/>
		protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
			=> JsonSerializer.SerializeAsync(stream, _Instance, _Options);

		/// <inheritdoc/>
		protected override bool TryComputeLength(out long length)
		{
			// We can't know the length of the content being pushed to the output stream.
			length = -1;
			return false;
		}
	}
}