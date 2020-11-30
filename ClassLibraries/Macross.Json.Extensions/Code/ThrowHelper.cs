using System;
using System.Diagnostics;
#if !NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text.Json;

namespace Macross.Json.Extensions
{
	internal static class ThrowHelper
	{
		// Unfortunately, System.Text.Json.ThrowHelper is internal so we have to use reflection to throw a good exception
		// that includes the JSONPath, line number and byte position in line of where the conversion error occurred.
		private static readonly Action<Type>? s_ThrowJsonExceptionDeserializeUnableToConvertValue =
			(Action<Type>?)typeof(JsonException)
				.Assembly
				.GetType("System.Text.Json.ThrowHelper")?
				.GetMethod("ThrowJsonException_DeserializeUnableToConvertValue", new[] { typeof(Type) })?
				.CreateDelegate(typeof(Action<Type>));

		/// <summary>
		/// Throw a <see cref="JsonException"/> using the internal <c>System.Text.Json.ThrowHelper</c> class that will eventually include
		/// the JSONPath, line number, and byte position in line.
		/// <para>
		/// If the internal <c>System.Text.Json.ThrowHelper</c> method is not available or throws an error, a standard <see cref="JsonException"/>
		/// that does not include additional information will be thrown instead.
		/// Here is what the final message of the exception looks like:
		/// The JSON value could not be converted to {0}. Path: $.{JSONPath} | LineNumber: {LineNumber} | BytePositionInLine: {BytePositionInLine}.
		/// </para>
		/// </summary>
		/// <param name="propertyType">Property type.</param>
		/// <param name="fallbackValue">Value that could not be parsed for the fallback exception message.</param>
#if !NETSTANDARD2_0
		[DoesNotReturn]
#endif
		public static void ThrowJsonException_DeserializeUnableToConvertValue(Type propertyType, string fallbackValue)
		{
			Debug.Assert(s_ThrowJsonExceptionDeserializeUnableToConvertValue != null);

			s_ThrowJsonExceptionDeserializeUnableToConvertValue?.Invoke(propertyType);

			// This should only execute if s_ThrowJsonExceptionDeserializeUnableToConvertValue could not be bound at runtime.
			throw new JsonException($"The JSON value '{fallbackValue}' could not be converted to {propertyType}.");
		}
	}
}