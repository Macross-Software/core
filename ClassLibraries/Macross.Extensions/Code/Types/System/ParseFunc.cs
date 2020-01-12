namespace System
{
	/// <summary>
	/// Delegate for parsing a string value into a destination Type.
	/// </summary>
	/// <typeparam name="T">Destination Type.</typeparam>
	/// <param name="input">Source string value.</param>
	/// <returns>Parsed value in the destination Type.</returns>
	public delegate T ParseFunc<T>(string input);
}