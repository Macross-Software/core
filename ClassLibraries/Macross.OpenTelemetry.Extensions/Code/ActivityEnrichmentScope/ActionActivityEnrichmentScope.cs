using System;
using System.Diagnostics;

namespace Macross.OpenTelemetry.Extensions
{
	internal sealed class ActionActivityEnrichmentScope : ActivityEnrichmentScopeBase
	{
		public Action<Activity>? EnrichmentAction { get; private set; }

		public ActionActivityEnrichmentScope(Action<Activity> enrichmentAction)
		{
			EnrichmentAction = enrichmentAction ?? throw new ArgumentNullException(nameof(enrichmentAction));
		}

		public override void Enrich(Activity activity)
			=> EnrichmentAction?.Invoke(activity);

		protected override void Dispose(bool isDisposing)
		{
			EnrichmentAction = null;

			base.Dispose(isDisposing);
		}
	}
}
