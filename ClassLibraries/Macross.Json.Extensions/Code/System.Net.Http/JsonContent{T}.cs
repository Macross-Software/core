using System.Text.Json;
using System.Net.Http.Headers;

namespace System.Net.Http
{
	/// <summary>
	/// Provides an <see cref="HttpContent"/> implementation for writing an object as JSON.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	public class JsonContent<T> : PushStreamContent
		where T : class
	{
		private static readonly MediaTypeHeaderValue s_JsonHeader = new MediaTypeHeaderValue("application/json")
		{
			CharSet = "utf-8",
		};

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonContent{T}"/> class.
		/// </summary>
		/// <param name="instance">Instance to be serialized.</param>
		/// <param name="options"><see cref="JsonSerializerOptions"/>.</param>
		public JsonContent(T? instance, JsonSerializerOptions? options = null)
			: base(stream => JsonSerializer.SerializeAsync(stream, instance, options))
		{
			Headers.ContentType = s_JsonHeader;
		}
	}
}