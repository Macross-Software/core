#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
using System.Buffers;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
#else
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
#endif

using Macross.Json.Extensions;

namespace System.Text.Json.Serialization
{
	/// <summary>
	/// <see cref="JsonConverterFactory"/> to convert <see cref="IPEndPoint"/> to and from strings.
	/// </summary>
	public class JsonIPEndPointConverter : JsonConverter<IPEndPoint>
	{
#if NETSTANDARD2_0
		private static readonly Regex s_IPEndPointRegex = new("^[[]?(.*?)[]]?:(\\d+)$", RegexOptions.Compiled | RegexOptions.Singleline);
#endif

		/// <inheritdoc/>
		public override IPEndPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String)
				throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(IPEndPoint));

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
			Span<char> charData = stackalloc char[53];
			int count = Encoding.UTF8.GetChars(
				reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan,
				charData);

			int addressLength = count;
			int lastColonPos = charData.LastIndexOf(':');

			if (lastColonPos > 0)
			{
				if (charData[lastColonPos - 1] == ']')
				{
					addressLength = lastColonPos;
				}
				else if (charData[..lastColonPos].LastIndexOf(':') == -1)
				{
					// Look to see if this is IPv4 with a port (IPv6 will have another colon)
					addressLength = lastColonPos;
				}
			}

			if (!IPAddress.TryParse(charData[..addressLength], out IPAddress? address))
				throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(IPEndPoint));

			uint port = 0;
			return addressLength == charData.Length ||
				(uint.TryParse(charData[(addressLength + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out port) && port <= IPEndPoint.MaxPort)
				? new IPEndPoint(address, (int)port)
				: throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(typeof(IPEndPoint));
#else
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
#endif
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options)
#pragma warning disable CA1062 // Don't perform checks for performance. Trust our callers will be nice.
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
		{
			bool isIpv6 = value.AddressFamily == AddressFamily.InterNetworkV6;
			Span<char> data = isIpv6
				? stackalloc char[21]
				: stackalloc char[53];
			int offset = 0;
			if (isIpv6)
			{
				data[0] = '[';
				offset++;
			}
			if (!value.Address.TryFormat(data[offset..], out int addressCharsWritten))
				throw new JsonException($"IPEndPoint [{value}] could not be written to JSON.");
			offset += addressCharsWritten;
			if (isIpv6)
			{
				data[offset++] = ']';
			}
			data[offset++] = ':';
			if (!value.Port.TryFormat(data[offset..], out int portCharsWritten))
				throw new JsonException($"IPEndPoint [{value}] could not be written to JSON.");
			writer.WriteStringValue(data[..(offset + portCharsWritten)]);
		}
#else
			=> writer.WriteStringValue(value.ToString());
#endif
#pragma warning restore CA1062 // Validate arguments of public methods
	}
}
