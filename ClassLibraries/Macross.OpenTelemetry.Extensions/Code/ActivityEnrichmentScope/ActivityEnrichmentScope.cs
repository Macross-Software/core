using Macross.OpenTelemetry.Extensions;

namespace System.Diagnostics
{
	/// <summary>
	/// A class for enriching the data of <see cref="Activity"/> objects.
	/// </summary>
	public static class ActivityEnrichmentScope
	{
		/// <summary>
		/// Registers an action that will be called to enrich the next <see cref="Activity"/> processed under the current scope if it has been sampled.
		/// </summary>
		/// <param name="enrichmentAction">Action to be called.</param>
		/// <returns><see cref="IDisposable"/> to cancel the enrichment scope.</returns>
		public static IDisposable Begin(Action<Activity> enrichmentAction)
			=> new ActionActivityEnrichmentScope(enrichmentAction);

		/// <summary>
		/// Registers an <see cref="ActivityEnricher{TState}"/> that will be called to enrich the next <see cref="Activity"/> processed under the current scope if it has been sampled.
		/// </summary>
		/// <typeparam name="TState">State type.</typeparam>
		/// <param name="activityEnricher"><see cref="ActivityEnricher{TState}"/> to be called.</param>
		/// <param name="state">The state to pass to the <see cref="ActivityEnricher{TState}"/>.</param>
		/// <returns><see cref="IDisposable"/> to cancel the enrichment scope.</returns>
		public static IDisposable Begin<TState>(ActivityEnricher<TState> activityEnricher, TState state)
			=> new GenericActivityEnrichmentScope<TState>(activityEnricher, state);
	}
}
