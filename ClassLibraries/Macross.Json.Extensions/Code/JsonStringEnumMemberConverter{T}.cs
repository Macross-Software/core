using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
#pragma warning disable CA1812 // Remove class never instantiated
	internal class JsonStringEnumMemberConverter<T> : JsonConverter<T>
#pragma warning restore CA1812 // Remove class never instantiated
	{
#if NETSTANDARD2_0
		private static readonly string[] s_Split = new string[] { ", " };
#endif

		private class EnumInfo
		{
#pragma warning disable SA1401 // Fields should be private
			public string Name;
			public Enum EnumValue;
			public ulong RawValue;
#pragma warning restore SA1401 // Fields should be private

			public EnumInfo(string name, Enum enumValue, ulong rawValue)
			{
				Name = name;
				EnumValue = enumValue;
				RawValue = rawValue;
			}
		}

		private readonly bool _AllowIntegerValues;
		private readonly Type? _UnderlyingType;
		private readonly Type _EnumType;
		private readonly TypeCode _EnumTypeCode;
		private readonly bool _IsFlags;
		private readonly Dictionary<ulong, EnumInfo> _RawToTransformed;
		private readonly Dictionary<string, EnumInfo> _TransformedToRaw;

		public JsonStringEnumMemberConverter(JsonNamingPolicy? namingPolicy, bool allowIntegerValues, Type? underlyingType)
		{
			Debug.Assert(
				(typeof(T).IsEnum && underlyingType == null)
				|| (Nullable.GetUnderlyingType(typeof(T)) == underlyingType),
				"Generic type is invalid.");

			_AllowIntegerValues = allowIntegerValues;
			_UnderlyingType = underlyingType;
			_EnumType = _UnderlyingType ?? typeof(T);
			_EnumTypeCode = Type.GetTypeCode(_EnumType);
			_IsFlags = _EnumType.IsDefined(typeof(FlagsAttribute), true);

			string[] builtInNames = _EnumType.GetEnumNames();
			Array builtInValues = _EnumType.GetEnumValues();

			_RawToTransformed = new Dictionary<ulong, EnumInfo>();
			_TransformedToRaw = new Dictionary<string, EnumInfo>();

			for (int i = 0; i < builtInNames.Length; i++)
			{
				Enum enumValue = (Enum)builtInValues.GetValue(i);
				ulong rawValue = GetEnumValue(enumValue);

				string name = builtInNames[i];

				string transformedName;
				if (namingPolicy == null)
				{
					FieldInfo field = _EnumType.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!;
					EnumMemberAttribute enumMemberAttribute = field.GetCustomAttribute<EnumMemberAttribute>(true);
					transformedName = enumMemberAttribute?.Value ?? name;
				}
				else
				{
					transformedName = namingPolicy.ConvertName(name) ?? name;
				}

				_RawToTransformed[rawValue] = new EnumInfo(transformedName, enumValue, rawValue);
				_TransformedToRaw[transformedName] = new EnumInfo(name, enumValue, rawValue);
			}
		}

		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			JsonTokenType token = reader.TokenType;

			// Note: There is no check for token == JsonTokenType.Null because Json serializer won't call the converter in that case.
			if (token == JsonTokenType.String)
			{
				string enumString = reader.GetString();

				// Case sensitive search attempted first.
				if (_TransformedToRaw.TryGetValue(enumString, out EnumInfo enumInfo))
					return (T)Enum.ToObject(_EnumType, enumInfo.RawValue);

				if (_IsFlags)
				{
					ulong calculatedValue = 0;

#if NETSTANDARD2_0
					string[] flagValues = enumString.Split(s_Split, StringSplitOptions.None);
#else
					string[] flagValues = enumString.Split(", ");
#endif
					foreach (string flagValue in flagValues)
					{
						// Case sensitive search attempted first.
						if (_TransformedToRaw.TryGetValue(flagValue, out enumInfo))
						{
							calculatedValue |= enumInfo.RawValue;
						}
						else
						{
							// Case insensitive search attempted second.
							bool matched = false;
							foreach (KeyValuePair<string, EnumInfo> enumItem in _TransformedToRaw)
							{
								if (string.Equals(enumItem.Key, flagValue, StringComparison.OrdinalIgnoreCase))
								{
									calculatedValue |= enumItem.Value.RawValue;
									matched = true;
									break;
								}
							}

							if (!matched)
								throw new JsonException($"Unknown flag value {flagValue}.");
						}
					}

					return (T)Enum.ToObject(_EnumType, calculatedValue);
				}

				// Case insensitive search attempted second.
				foreach (KeyValuePair<string, EnumInfo> enumItem in _TransformedToRaw)
				{
					if (string.Equals(enumItem.Key, enumString, StringComparison.OrdinalIgnoreCase))
					{
						return (T)Enum.ToObject(_EnumType, enumItem.Value.RawValue);
					}
				}

				throw new JsonException($"Unknown value {enumString}.");
			}

			if (token != JsonTokenType.Number || !_AllowIntegerValues)
				throw new JsonException();

			switch (_EnumTypeCode)
			{
				// Switch cases ordered by expected frequency.
				case TypeCode.Int32:
					if (reader.TryGetInt32(out int int32))
					{
						return (T)Enum.ToObject(_EnumType, int32);
					}
					break;
				case TypeCode.UInt32:
					if (reader.TryGetUInt32(out uint uint32))
					{
						return (T)Enum.ToObject(_EnumType, uint32);
					}
					break;
				case TypeCode.UInt64:
					if (reader.TryGetUInt64(out ulong uint64))
					{
						return (T)Enum.ToObject(_EnumType, uint64);
					}
					break;
				case TypeCode.Int64:
					if (reader.TryGetInt64(out long int64))
					{
						return (T)Enum.ToObject(_EnumType, int64);
					}
					break;

				case TypeCode.SByte:
					if (reader.TryGetSByte(out sbyte byte8))
					{
						return (T)Enum.ToObject(_EnumType, byte8);
					}
					break;
				case TypeCode.Byte:
					if (reader.TryGetByte(out byte ubyte8))
					{
						return (T)Enum.ToObject(_EnumType, ubyte8);
					}
					break;
				case TypeCode.Int16:
					if (reader.TryGetInt16(out short int16))
					{
						return (T)Enum.ToObject(_EnumType, int16);
					}
					break;
				case TypeCode.UInt16:
					if (reader.TryGetUInt16(out ushort uint16))
					{
						return (T)Enum.ToObject(_EnumType, uint16);
					}
					break;
			}

			throw new JsonException();
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			// Note: There is no check for value == null because Json serializer won't call the converter in that case.
			ulong rawValue = GetEnumValue(value!);

			if (_RawToTransformed.TryGetValue(rawValue, out EnumInfo enumInfo))
			{
				writer.WriteStringValue(enumInfo.Name);
				return;
			}

			if (_IsFlags)
			{
				ulong calculatedValue = 0;

				StringBuilder Builder = new StringBuilder();
				foreach (KeyValuePair<ulong, EnumInfo> enumItem in _RawToTransformed)
				{
					enumInfo = enumItem.Value;
					if (!(value as Enum)!.HasFlag(enumInfo.EnumValue)
						|| enumInfo.RawValue == 0) // Definitions with 'None' should hit the cache case.
					{
						continue;
					}

					// Track the value to make sure all bits are represented.
					calculatedValue |= enumInfo.RawValue;

					if (Builder.Length > 0)
						Builder.Append(", ");
					Builder.Append(enumInfo.Name);
				}
				if (calculatedValue == rawValue)
				{
					writer.WriteStringValue(Builder.ToString());
					return;
				}
			}

			if (!_AllowIntegerValues)
				throw new JsonException();

			switch (_EnumTypeCode)
			{
				case TypeCode.Int32:
					writer.WriteNumberValue((int)rawValue);
					break;
				case TypeCode.UInt32:
					writer.WriteNumberValue((uint)rawValue);
					break;
				case TypeCode.UInt64:
					writer.WriteNumberValue(rawValue);
					break;
				case TypeCode.Int64:
					writer.WriteNumberValue((long)rawValue);
					break;
				case TypeCode.Int16:
					writer.WriteNumberValue((short)rawValue);
					break;
				case TypeCode.UInt16:
					writer.WriteNumberValue((ushort)rawValue);
					break;
				case TypeCode.Byte:
					writer.WriteNumberValue((byte)rawValue);
					break;
				case TypeCode.SByte:
					writer.WriteNumberValue((sbyte)rawValue);
					break;
				default:
					throw new JsonException();
			}
		}

		private ulong GetEnumValue(object value)
		{
			return _EnumTypeCode switch
			{
				TypeCode.Int32 => (ulong)(int)value,
				TypeCode.UInt32 => (uint)value,
				TypeCode.UInt64 => (ulong)value,
				TypeCode.Int64 => (ulong)(long)value,
				TypeCode.SByte => (ulong)(sbyte)value,
				TypeCode.Byte => (byte)value,
				TypeCode.Int16 => (ulong)(short)value,
				TypeCode.UInt16 => (ushort)value,
				_ => throw new JsonException(),
			};
		}
	}
}
