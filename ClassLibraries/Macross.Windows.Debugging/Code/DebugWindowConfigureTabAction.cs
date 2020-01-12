namespace Macross.Windows.Debugging
{
	/// <summary>
	/// A delegate for configuring <see cref="DebugWindowTabPage"/>s as they are added to the launched <see cref="DebugWindow"/>.
	/// </summary>
	/// <param name="debugWindowTab"><see cref="DebugWindowTabPage"/> being created.</param>
	public delegate void DebugWindowConfigureTabAction(DebugWindowTabPage debugWindowTab);
}
