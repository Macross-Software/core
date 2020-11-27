using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// <see cref="JsonConverterFactory"/> to convert <see cref="IPEndPoint"/> to and from strings.
	/// </summary>
	public class JsonIPEndPointConverter : JsonConverter<IPEndPoint>
	{
		private static readonly Regex s_IPEndPointRegex = new Regex("^[[]?(.*?)[]]?:(\\d+)$", RegexOptions.Compiled | RegexOptions.Singleline);

		/// <inheritdoc/>
		public override IPEndPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String)
				throw new JsonException();

			Match match = s_IPEndPointRegex.Match(reader.GetString()!);
			if (!match.Success)
				throw new JsonException();

			try
			{
				return new IPEndPoint(IPAddress.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
			}
			catch (Exception ex)
			{
				throw new JsonException("Unexpected value format, unable to parse IPEndPoint.", ex);
			}
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options) =>
#pragma warning disable CA1062 // Don't perform checks for performance. Trust our callers will be nice.
			writer.WriteStringValue(value.ToString());
#pragma warning restore CA1062 // Validate arguments of public methods

	}
}
