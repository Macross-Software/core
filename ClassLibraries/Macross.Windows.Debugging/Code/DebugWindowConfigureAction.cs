namespace Macross.Windows.Debugging
{
	/// <summary>
	/// A delegate for configuring <see cref="DebugWindow"/> created at runtime.
	/// </summary>
	/// <param name="debugWindow"><see cref="DebugWindow"/> being created.</param>
	public delegate void DebugWindowConfigureAction(DebugWindow debugWindow);
}
