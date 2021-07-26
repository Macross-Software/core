using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Globalization;

using Macross.Json.Extensions;

namespace System.Text.Json.Serialization
{
	internal class JsonStringEnumMemberConverterHelper<TEnum>
		where TEnum : struct, Enum
	{
		private class EnumInfo
		{
#pragma warning disable SA1401 // Fields should be private
			public string Name;
			public TEnum EnumValue;
			public ulong RawValue;
#pragma warning restore SA1401 // Fields should be private

			public EnumInfo(string name, TEnum enumValue, ulong rawValue)
			{
				Name = name;
				EnumValue = enumValue;
				RawValue = rawValue;
			}
		}

		private const BindingFlags EnumBindings = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
		private const int MaximumAutoGrowthCacheSize = 64;

#if NETSTANDARD2_0
		private static readonly string[] s_Split = new string[] { ", " };
#endif

		private readonly bool _AllowIntegerValues;
		private readonly ulong? _DeserializationFailureFallbackValueRaw;
		private readonly TEnum? _DeserializationFailureFallbackValue;
		private readonly Type _EnumType;
		private readonly TypeCode _EnumTypeCode;
		private readonly bool _IsFlags;
		private readonly object _TransformedToRawCopyLockObject = new();
		private readonly object _RawToTransformedCopyLockObject = new();
		private Dictionary<TEnum, EnumInfo> _RawToTransformed;
		private Dictionary<string, EnumInfo> _TransformedToRaw;

		public JsonStringEnumMemberConverterHelper(JsonStringEnumMemberConverterOptions? options)
		{
			_EnumType = typeof(TEnum);

			JsonStringEnumMemberConverterOptions? computedOptions
				= _EnumType.GetCustomAttribute<JsonStringEnumMemberConverterOptionsAttribute>(false)?.Options
				?? options;

			_AllowIntegerValues = computedOptions?.AllowIntegerValues ?? true;
			_EnumTypeCode = Type.GetTypeCode(_EnumType);
			_IsFlags = _EnumType.IsDefined(typeof(FlagsAttribute), true);

			ulong? deserializationFailureFallbackValue = computedOptions?.ConvertedDeserializationFailureFallbackValue;

			string[] builtInNames = _EnumType.GetEnumNames();
			Array builtInValues = _EnumType.GetEnumValues();

			int numberOfBuiltInNames = builtInNames.Length;

			_RawToTransformed = new Dictionary<TEnum, EnumInfo>(numberOfBuiltInNames);
			_TransformedToRaw = new Dictionary<string, EnumInfo>(numberOfBuiltInNames);

			for (int i = 0; i < numberOfBuiltInNames; i++)
			{
				Enum? enumValue = (Enum?)builtInValues.GetValue(i);
				if (enumValue == null)
					continue;
				ulong rawValue = JsonStringEnumMemberConverter.GetEnumValue(_EnumTypeCode, enumValue);

				string name = builtInNames[i];
				FieldInfo field = _EnumType.GetField(name, EnumBindings)!;

				string transformedName = field.GetCustomAttribute<EnumMemberAttribute>(true)?.Value ??
										 field.GetCustomAttribute<JsonPropertyNameAttribute>(true)?.Name ??
										 computedOptions?.NamingPolicy?.ConvertName(name) ??
										 name;

				if (enumValue is not TEnum typedValue)
					throw new NotSupportedException();

				if (deserializationFailureFallbackValue.HasValue && rawValue == deserializationFailureFallbackValue)
				{
					_DeserializationFailureFallbackValueRaw = deserializationFailureFallbackValue;
					_DeserializationFailureFallbackValue = typedValue;
				}

				_RawToTransformed[typedValue] = new EnumInfo(transformedName, typedValue, rawValue);
				_TransformedToRaw[transformedName] = new EnumInfo(name, typedValue, rawValue);
			}

			if (deserializationFailureFallbackValue.HasValue && !_DeserializationFailureFallbackValue.HasValue)
				throw new JsonException($"JsonStringEnumMemberConverter could not find a definition on Enum type {_EnumType} matching deserializationFailureFallbackValue '{deserializationFailureFallbackValue}'.");
		}

		public TEnum Read(ref Utf8JsonReader reader)
		{
			JsonTokenType token = reader.TokenType;

			if (token == JsonTokenType.String)
			{
				string enumString = reader.GetString()!;

				Dictionary<string, EnumInfo> transformedToRaw = _TransformedToRaw;

				// Case sensitive search attempted first.
				if (transformedToRaw.TryGetValue(enumString, out EnumInfo? enumInfo))
					return enumInfo.EnumValue;

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
						if (transformedToRaw.TryGetValue(flagValue, out enumInfo))
						{
							calculatedValue |= enumInfo.RawValue;
						}
						else
						{
							// Case insensitive search attempted second.
							bool matched = false;
							foreach (KeyValuePair<string, EnumInfo> enumItem in transformedToRaw)
							{
								if (string.Equals(enumItem.Key, flagValue, StringComparison.OrdinalIgnoreCase))
								{
									calculatedValue |= enumItem.Value.RawValue;
									matched = true;
									break;
								}
							}

							if (!matched)
							{
								if (_DeserializationFailureFallbackValueRaw.HasValue)
									calculatedValue |= _DeserializationFailureFallbackValueRaw.Value;
								else
									throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(_EnumType, flagValue);
							}
						}
					}

					TEnum enumValue = (TEnum)Enum.ToObject(_EnumType, calculatedValue);
					if (transformedToRaw.Count < MaximumAutoGrowthCacheSize)
					{
						lock (_TransformedToRawCopyLockObject)
						{
							if (!_TransformedToRaw.ContainsKey(enumString) && _TransformedToRaw.Count < MaximumAutoGrowthCacheSize)
							{
								Dictionary<string, EnumInfo> transformedToRawCopy = new(_TransformedToRaw);
								transformedToRawCopy[enumString] = new EnumInfo(enumString, enumValue, calculatedValue);
								_TransformedToRaw = transformedToRawCopy;
							}
						}
					}
					return enumValue;
				}

				// Case insensitive search attempted second.
				foreach (KeyValuePair<string, EnumInfo> enumItem in transformedToRaw)
				{
					if (string.Equals(enumItem.Key, enumString, StringComparison.OrdinalIgnoreCase))
					{
						return enumItem.Value.EnumValue;
					}
				}

				return _DeserializationFailureFallbackValue ?? throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(_EnumType, enumString);
			}

			if (token != JsonTokenType.Number || !_AllowIntegerValues)
			{
				if (_DeserializationFailureFallbackValue.HasValue)
				{
					reader.Skip();
					return _DeserializationFailureFallbackValue.Value;
				}
				throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(_EnumType);
			}

			switch (_EnumTypeCode)
			{
				case TypeCode.Int32:
					if (reader.TryGetInt32(out int int32))
					{
						return (TEnum)Enum.ToObject(_EnumType, int32);
					}
					break;
				case TypeCode.Int64:
					if (reader.TryGetInt64(out long int64))
					{
						return (TEnum)Enum.ToObject(_EnumType, int64);
					}
					break;
				case TypeCode.Int16:
					if (reader.TryGetInt16(out short int16))
					{
						return (TEnum)Enum.ToObject(_EnumType, int16);
					}
					break;
				case TypeCode.Byte:
					if (reader.TryGetByte(out byte ubyte8))
					{
						return (TEnum)Enum.ToObject(_EnumType, ubyte8);
					}
					break;
				case TypeCode.UInt32:
					if (reader.TryGetUInt32(out uint uint32))
					{
						return (TEnum)Enum.ToObject(_EnumType, uint32);
					}
					break;
				case TypeCode.UInt64:
					if (reader.TryGetUInt64(out ulong uint64))
					{
						return (TEnum)Enum.ToObject(_EnumType, uint64);
					}
					break;
				case TypeCode.UInt16:
					if (reader.TryGetUInt16(out ushort uint16))
					{
						return (TEnum)Enum.ToObject(_EnumType, uint16);
					}
					break;
				case TypeCode.SByte:
					if (reader.TryGetSByte(out sbyte byte8))
					{
						return (TEnum)Enum.ToObject(_EnumType, byte8);
					}
					break;
			}

			throw ThrowHelper.GenerateJsonException_DeserializeUnableToConvertValue(_EnumType);
		}

		public void Write(Utf8JsonWriter writer, TEnum value)
		{
			Dictionary<TEnum, EnumInfo> rawToTransformed = _RawToTransformed;
			if (rawToTransformed.TryGetValue(value, out EnumInfo? enumInfo))
			{
				writer.WriteStringValue(enumInfo.Name);
				return;
			}

			ulong rawValue = JsonStringEnumMemberConverter.GetEnumValue(_EnumTypeCode, value);

			if (_IsFlags)
			{
				ulong calculatedValue = 0;

				StringBuilder Builder = new();
				foreach (KeyValuePair<TEnum, EnumInfo> enumItem in rawToTransformed)
				{
					enumInfo = enumItem.Value;
					if (!value.HasFlag(enumInfo.EnumValue)
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
					string finalName = Builder.ToString();
					if (rawToTransformed.Count < MaximumAutoGrowthCacheSize)
					{
						lock (_RawToTransformedCopyLockObject)
						{
							if (!_RawToTransformed.ContainsKey(value) && _RawToTransformed.Count < MaximumAutoGrowthCacheSize)
							{
								Dictionary<TEnum, EnumInfo> rawToTransformedCopy = new(_RawToTransformed);
								rawToTransformedCopy[value] = new EnumInfo(finalName, value, rawValue);
								_RawToTransformed = rawToTransformedCopy;
							}
						}
					}
					writer.WriteStringValue(finalName);
					return;
				}
			}

			if (!_AllowIntegerValues)
				throw new JsonException($"Enum type {_EnumType} does not have a mapping for integer value '{rawValue.ToString(CultureInfo.CurrentCulture)}'.");

			switch (_EnumTypeCode)
			{
				case TypeCode.Int32:
					writer.WriteNumberValue((int)rawValue);
					break;
				case TypeCode.Int64:
					writer.WriteNumberValue((long)rawValue);
					break;
				case TypeCode.Int16:
					writer.WriteNumberValue((short)rawValue);
					break;
				case TypeCode.Byte:
					writer.WriteNumberValue((byte)rawValue);
					break;
				case TypeCode.UInt32:
					writer.WriteNumberValue((uint)rawValue);
					break;
				case TypeCode.UInt64:
					writer.WriteNumberValue(rawValue);
					break;
				case TypeCode.UInt16:
					writer.WriteNumberValue((ushort)rawValue);
					break;
				case TypeCode.SByte:
					writer.WriteNumberValue((sbyte)rawValue);
					break;
				default:
					throw new JsonException(); // GetEnumValue should have already thrown.
			}
		}
	}
}
