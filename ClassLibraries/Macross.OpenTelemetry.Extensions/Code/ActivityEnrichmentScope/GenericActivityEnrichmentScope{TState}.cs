using System;
using System.Diagnostics;

namespace Macross.OpenTelemetry.Extensions
{
	internal sealed class GenericActivityEnrichmentScope<TState> : ActivityEnrichmentScopeBase
	{
		private readonly TState _State;

		public ActivityEnricher<TState>? ActivityEnricher { get; private set; }

		public GenericActivityEnrichmentScope(ActivityEnricher<TState> activityEnricher, TState state)
		{
			ActivityEnricher = activityEnricher ?? throw new ArgumentNullException(nameof(activityEnricher));
			_State = state;
		}

		public override void Enrich(Activity activity)
			=> ActivityEnricher?.Invoke(activity, _State);

		protected override void Dispose(bool isDisposing)
		{
			ActivityEnricher = null;

			base.Dispose(isDisposing);
		}
	}
}