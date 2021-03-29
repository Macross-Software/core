namespace System.Diagnostics
{
	/// <summary>
	/// Describes the target of an enrichment scope.
	/// </summary>
	public enum EnrichmentScopeTarget
	{
		/// <summary>
		/// The first child created under the scope will be enriched and then the scope will automatically be closed.
		/// </summary>
		FirstChild,

		/// <summary>
		/// All child objects created under the scope will be enriched until the scope is closed.
		/// </summary>
		AllChildren,
	}
}