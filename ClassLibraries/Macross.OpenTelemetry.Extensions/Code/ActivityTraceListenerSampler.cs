using System;
using System.Diagnostics;

using OpenTelemetry.Trace;

namespace Macross.OpenTelemetry.Extensions
{
	internal sealed class ActivityTraceListenerSampler : Sampler
	{
		private readonly Sampler _InnerSampler;

		internal ActivityTraceListenerManager? ActivityTraceListenerManager { get; set; }

		public ActivityTraceListenerSampler(Sampler innerSampler)
		{
			_InnerSampler = innerSampler ?? throw new ArgumentNullException(nameof(innerSampler));
		}

		public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
		{
			ActivityTraceListenerManager? activityTraceListenerManager = ActivityTraceListenerManager;
			return activityTraceListenerManager != null
				&& activityTraceListenerManager.IsTraceIdRegistered(samplingParameters.TraceId)
				? new SamplingResult(SamplingDecision.RecordAndSample)
				: _InnerSampler.ShouldSample(samplingParameters);
		}
	}
}
