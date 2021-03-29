using System;
using System.Diagnostics;

using OpenTelemetry;

namespace Macross.OpenTelemetry.Extensions
{
	internal sealed class ActivityEnrichmentScopeProcessor : BaseProcessor<Activity>
	{
		/// <inheritdoc/>
		public override void OnEnd(Activity activity)
		{
			ActivityEnrichmentScopeBase? scope = ActivityEnrichmentScopeBase.Current;
			while (scope != null)
			{
				try
				{
					scope.Enrich(activity);
				}
#pragma warning disable CA1031 // Do not catch general exception types
				catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
				{
					OpenTelemetryExtensionsEventSource.Log.SpanProcessorException(nameof(ActivityEnrichmentScopeProcessor), ex);
				}

				ActivityEnrichmentScopeBase? nextParent = scope.Parent;

				if (scope.EnrichmentTarget == EnrichmentScopeTarget.FirstChild)
				{
					scope.Dispose();
				}

				scope = nextParent;
			}
		}
	}
}
