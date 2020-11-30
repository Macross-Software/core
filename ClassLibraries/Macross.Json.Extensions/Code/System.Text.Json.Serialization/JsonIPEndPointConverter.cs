using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

using Macross.Json.Extensions;

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
				throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(IPEndPoint));

			string value = reader.GetString()!;

			Match match = s_IPEndPointRegex.Match(value);
			if (!match.Success)
				throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(IPEndPoint), value);

			try
			{
				return new IPEndPoint(
					IPAddress.Parse(match.Groups[1].Value),
					int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
			}
			catch (Exception ex)
			{
				throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(IPEndPoint), value, ex);
			}
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options) =>
#pragma warning disable CA1062 // Don't perform checks for performance. Trust our callers will be nice.
			writer.WriteStringValue(value.ToString());
#pragma warning restore CA1062 // Validate arguments of public methods

	}
}
