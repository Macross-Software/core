using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Linq;

namespace System
{
	/// <summary>
	/// Methods extending what is provided in the System namespace for enumerations.
	/// </summary>
	public static class EnumExtensions
	{
		private class EnumDefinition
		{
			public string Name { get; }

			public string? AlternativeName { get; }

			public object Value { get; }

			public EnumDefinition(string name, string? alternativeName, object value)
			{
				Name = name;
				AlternativeName = alternativeName;
				Value = value;
			}
		}

		private static readonly Dictionary<Type, EnumDefinition[]> s_EnumDataCache = new Dictionary<Type, EnumDefinition[]>();

		/// <summary>
		/// Convert a Value Type into an Enum instance or return a pre-defined default value.
		/// </summary>
		/// <typeparam name="TValue">The source Value Type.</typeparam>
		/// <typeparam name="TEnum">The destination Enum Type.</typeparam>
		/// <param name="value">The value to convert into an Enum.</param>
		/// <param name="valueIfUndefined">The value to be used if conversion failed.</param>
		/// <returns>The parsed Enum instance or default value if conversion failed.</returns>
		public static TEnum ToEnum<TValue, TEnum>(this TValue value, TEnum valueIfUndefined)
			where TValue : struct
			where TEnum : struct, Enum
		{
			return !TryToEnum(value, out TEnum result)
				? valueIfUndefined
				: result;
		}

		/// <summary>
		/// Convert a Value Type into an Enum instance.
		/// </summary>
		/// <typeparam name="TValue">The source Value Type.</typeparam>
		/// <typeparam name="TEnum">The destination Enum Type.</typeparam>
		/// <param name="value">The value to convert into an Enum.</param>
		/// <returns>The parsed Enum instance.</returns>
		/// <exception cref="ArgumentException">The value provided could not be converted.</exception>
		public static TEnum ToEnum<TValue, TEnum>(this TValue value)
			where TValue : struct
			where TEnum : struct, Enum
		{
			return !TryToEnum(value, out TEnum result)
				? throw new ArgumentException($"Value [{value}] cannot be converted to [{typeof(TEnum).FullName}] Enum Type.")
				: result;
		}

		/// <summary>
		/// Attempt to convert a Value Type into an Enum instance.
		/// </summary>
		/// <typeparam name="TValue">The source Value Type.</typeparam>
		/// <typeparam name="TEnum">The destination Enum Type.</typeparam>
		/// <param name="value">The value to convert into an Enum.</param>
		/// <param name="result">The parsed Enum instance, if successful.</param>
		/// <returns>Whether or not parsing was successful.</returns>
		public static bool TryToEnum<TValue, TEnum>(this TValue value, out TEnum result)
			where TValue : struct
			where TEnum : struct, Enum
		{
			Type EnumType = typeof(TEnum);

			if (Enum.IsDefined(EnumType, value))
			{
				result = (TEnum)Enum.ToObject(EnumType, value);
				return true;
			}

			result = default;
			return false;
		}

		/// <summary>
		/// Convert a string value into an Enum instance or return a pre-defined default value.
		/// </summary>
		/// <typeparam name="T">The destination Enum Type.</typeparam>
		/// <param name="value">The string to convert into an Enum.</param>
		/// <param name="valueIfUndefined">The value to be used if conversion failed.</param>
		/// <returns>The parsed Enum instance or default value if conversion failed.</returns>
		public static T ToEnum<T>(this string value, T valueIfUndefined)
			where T : struct, Enum
		{
			return !TryToEnum(value, out T result)
				? valueIfUndefined
				: result;
		}

		/// <summary>
		/// Convert a string value into an Enum instance.
		/// </summary>
		/// <typeparam name="T">The destination Enum Type.</typeparam>
		/// <param name="value">The string to convert into an Enum.</param>
		/// <returns>The parsed Enum instance.</returns>
		/// <exception cref="ArgumentException">The value provided could not be converted.</exception>
		public static T ToEnum<T>(this string value)
			where T : struct, Enum
		{
			return !TryToEnum(value, out T result)
				? throw new ArgumentException($"Value [{value}] cannot be converted to [{typeof(T).FullName}] Enum Type.")
				: result;
		}

		/// <summary>
		/// Attempt to convert a string value into an Enum instance.
		/// </summary>
		/// <remarks>
		/// Conversion is case-insensitive and will match either defined Enum name or <see cref="EnumMemberAttribute.Value"/>, if used to decorate Enum definitions.
		/// </remarks>
		/// <typeparam name="T">The destination Enum Type.</typeparam>
		/// <param name="value">The string to convert into an Enum.</param>
		/// <param name="result">The parsed Enum instance, if successful.</param>
		/// <returns>Whether or not parsing was successful.</returns>
		public static bool TryToEnum<T>(this string value, out T result)
			where T : struct, Enum
		{
			Type EnumType = typeof(T);

			if (!s_EnumDataCache.TryGetValue(EnumType, out EnumDefinition[] EnumDefinitions))
			{
				lock (s_EnumDataCache)
				{
					if (!s_EnumDataCache.TryGetValue(EnumType, out EnumDefinitions))
					{
						string[] Names = Enum.GetNames(EnumType);
						Array Values = Enum.GetValues(EnumType);
						FieldInfo[] Fields = EnumType.GetFields(BindingFlags.Public | BindingFlags.Static);

						EnumDefinitions = new EnumDefinition[Names.Length];
						for (int i = 0; i < Names.Length; i++)
						{
							string Name = Names[i];

							EnumMemberAttribute? EnumMember = Fields.FirstOrDefault(f => f.Name == Name)?.GetCustomAttribute<EnumMemberAttribute>(true);

							EnumDefinitions[i] = new EnumDefinition(Name, EnumMember?.Value, Values.GetValue(i));
						}

						s_EnumDataCache.Add(EnumType, EnumDefinitions);
					}
				}
			}

			foreach (EnumDefinition EnumDefinition in EnumDefinitions)
			{
				if (string.Compare(value, EnumDefinition.Name, StringComparison.OrdinalIgnoreCase) == 0)
				{
					result = (T)EnumDefinition.Value;
					return true;
				}
				if (EnumDefinition.AlternativeName != null && string.Compare(value, EnumDefinition.AlternativeName, StringComparison.OrdinalIgnoreCase) == 0)
				{
					result = (T)EnumDefinition.Value;
					return true;
				}
			}

			result = default;
			return false;
		}

		/// <summary>
		/// Converts a bit field Enum instance into the defined names for all flagged values.
		/// </summary>
		/// <remarks>
		/// Enum Type must be decorated with <see cref="FlagsAttribute"/>.
		/// </remarks>
		/// <param name="value">Enum bit field instance.</param>
		/// <returns>List of strings matching defined Enum names flagged on the instance.</returns>
		public static IEnumerable<string> ToStrings(this Enum value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			Type EnumType = value.GetType();
#if DEBUG
			if (EnumType.GetCustomAttributes(typeof(FlagsAttribute), false).Length == 0)
				throw new InvalidOperationException($"Enum type [{EnumType.FullName}] is not decorated with the Flags Attribute.");
#endif
			Collection<string> Values = new Collection<string>();
			foreach (object Flag in Enum.GetValues(EnumType))
			{
				if (Convert.ToInt32(Flag, CultureInfo.InvariantCulture) == 0)
				{
					if (Convert.ToInt32(value, CultureInfo.InvariantCulture) == 0)
						Values.Add(Enum.GetName(EnumType, Flag));
					continue;
				}
				Enum enumValue = (Enum)Enum.ToObject(EnumType, Flag);
				if (value.HasFlag(enumValue))
					Values.Add(Enum.GetName(EnumType, Flag));
			}
			return Values;
		}
	}
}
