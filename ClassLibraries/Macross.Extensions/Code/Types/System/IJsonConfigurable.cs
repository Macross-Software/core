namespace System
{
	/// <summary>
	/// Marks objects that can be configured from a JSON string.
	/// </summary>
	public interface IJsonConfigurable
	{
		/// <summary>
		/// Apply JSON string configuration to the current instance.
		/// </summary>
		/// <param name="configuration">JSON string configuration.</param>
		void ApplyJsonConfiguration(string configuration);
	}
}
