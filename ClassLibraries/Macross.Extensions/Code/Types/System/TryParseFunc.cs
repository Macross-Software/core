namespace System
{
	/// <summary>
	/// Delegate for attempting to parse a string value into a destination Type.
	/// </summary>
	/// <typeparam name="T">Destination Type.</typeparam>
	/// <param name="input">Source string value.</param>
	/// <param name="value">The parsed value as an instance of <typeparamref name="T"/> when parsing was successful.</param>
	/// <returns>Whether or not parsing was successful.</returns>
	public delegate bool TryParseFunc<T>(string input, out T value);
}
