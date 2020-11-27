#if NETSTANDARD2_0 || NETSTANDARD2_1
using System.Text.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;

namespace System.Net.Http.Json
{
	/// <summary>
	/// Provides an <see cref="HttpContent"/> implementation for writing an object as JSON.
	/// </summary>
	public class JsonContent : HttpContent
	{
		private static readonly MediaTypeHeaderValue s_JsonHeader = new MediaTypeHeaderValue("application/json")
		{
			CharSet = "utf-8",
		};

		private readonly object? _Instance;
		private readonly Type _InstanceType;
		private readonly JsonSerializerOptions? _Options;

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonContent"/> class.
		/// </summary>
		/// <param name="instance">Instance to be serialized.</param>
		/// <param name="instanceType">Type of the instance to be serialized.</param>
		/// <param name="mediaType"><see cref="MediaTypeHeaderValue"/>.</param>
		/// <param name="options"><see cref="JsonSerializerOptions"/>.</param>
		private JsonContent(object? instance, Type instanceType, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null)
		{
			if (instanceType == null)
				throw new ArgumentNullException(nameof(instanceType));

			if (instance != null && !instanceType.IsAssignableFrom(instance.GetType()))
				throw new ArgumentException($"The specified type {instanceType} must derive from the specific value's type {instance.GetType()}.");

			_Instance = instance;
			_InstanceType = instanceType;
			_Options = options;

			Headers.ContentType = mediaType ?? s_JsonHeader;
		}

		/// <summary>
		/// Creates a new instance of the <see cref="JsonContent"/> class that will contain the inputValue serialized as JSON.
		/// </summary>
		/// <typeparam name="T">The type of the value to serialize.</typeparam>
		/// <param name="inputValue">The value to serialize.</param>
		/// <param name="mediaType">The media type to use for the content.</param>
		/// <param name="options">Options to control the behavior during serialization.</param>
		/// <returns><see cref="JsonContent"/>.</returns>
		public static JsonContent Create<T>(T inputValue, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null)
			=> new JsonContent(inputValue, typeof(T), mediaType, options);

		/// <summary>
		/// Creates a new instance of the <see cref="JsonContent"/> class that will contain the inputValue serialized as JSON.
		/// </summary>
		/// <param name="inputValue">The value to serialize.</param>
		/// <param name="inputType">The type of the value to serialize.</param>
		/// <param name="mediaType">The media type to use for the content.</param>
		/// <param name="options">Options to control the behavior during serialization.</param>
		/// <returns><see cref="JsonContent"/>.</returns>
		public static JsonContent Create(object? inputValue, Type inputType, MediaTypeHeaderValue? mediaType = null, JsonSerializerOptions? options = null)
			=> new JsonContent(inputValue, inputType, mediaType, options);

		/// <inheritdoc/>
		protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
			=> JsonSerializer.SerializeAsync(stream, _Instance, _InstanceType, _Options);

		/// <inheritdoc/>
		protected override bool TryComputeLength(out long length)
		{
			// We can't know the length of the content being pushed to the output stream.
			length = -1;
			return false;
		}
	}
}
#endif