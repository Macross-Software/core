namespace System.Diagnostics
{
	/// <summary>
	/// A callback function for enriching an <see cref="Activity"/> using some known state.
	/// </summary>
	/// <typeparam name="TState">State type.</typeparam>
	/// <param name="activity"><see cref="Activity"/> being enriched.</param>
	/// <param name="state">State.</param>
	public delegate void ActivityEnricher<TState>(Activity activity, TState state);
}
