using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Macross.OpenTelemetry.Extensions.Tests
{
	[TestClass]
	public sealed class ActivityTraceListenerManagerTests : IDisposable
	{
		private readonly ActivityContext _ActivityContext = new ActivityContext(
			ActivityTraceId.CreateRandom(),
			ActivitySpanId.CreateRandom(),
			ActivityTraceFlags.Recorded,
			isRemote: true);

		private readonly ActivitySource _ActivitySource = new ActivitySource(nameof(ActivityEnrichmentScopeProcessorTests));
		private readonly TestActivityProcessor _TestActivityProcessor = new TestActivityProcessor(collectEndedSpans: true);
		private readonly TracerProvider _TracerProvider;
		private readonly ServiceProvider _ServiceProvider;
		private readonly ActivityTraceListenerManager _ActivityTraceListenerManager;

		public ActivityTraceListenerManagerTests()
		{
			_TracerProvider = Sdk.CreateTracerProviderBuilder()
				.AddSource(_ActivitySource.Name)
				.SetActivityTraceListenerSampler(new AlwaysOffSampler())
				.AddProcessor(_TestActivityProcessor)
				.Build();

			IServiceCollection services = new ServiceCollection();
			services.AddSingleton(typeof(TracerProvider), _TracerProvider);
			services.AddOptions();
			services.AddActivityTraceListener();

			_ServiceProvider = services.BuildServiceProvider();

			_ActivityTraceListenerManager = _ServiceProvider.GetRequiredService<ActivityTraceListenerManager>();
		}

		public void Dispose()
		{
			_TracerProvider.Dispose();
			_TestActivityProcessor.Dispose();
			_ActivitySource.Dispose();
			_ServiceProvider.Dispose();
			_ActivityTraceListenerManager.Dispose();
		}

		[TestMethod]
		public void ActivityNotSampledWithoutRegistration()
		{
			Activity? activity = _ActivitySource.StartActivity("Test", ActivityKind.Server, _ActivityContext);

			Assert.IsNull(activity);
		}

		[TestMethod]
		public void ActivitySampledWithRegistration()
		{
			using (IActivityTraceListener registration = _ActivityTraceListenerManager.RegisterTraceListener(_ActivityContext.TraceId))
			{
				Activity? activity = _ActivitySource.StartActivity("Test", ActivityKind.Server, _ActivityContext);

				Assert.IsNotNull(activity);
				Assert.IsTrue(activity.IsAllDataRequested);
				Assert.IsTrue(activity.Recorded);

				activity.Stop();

				Activity? activity2 = _ActivitySource.StartActivity("Test", ActivityKind.Server);
				/* Note: This could be a bug in OpenTelemetry. If root sampler
				is AlwaysOffSampler then StartActivity returns null. But if
				AlwaysOffSampler is used as an inner sampler, it creates
				propagation-only spans. */
				Assert.IsNotNull(activity2);
				Assert.IsFalse(activity2.IsAllDataRequested);
				Assert.IsFalse(activity2.Recorded);

				Assert.AreEqual(1, registration.CompletedActivities.Count);
				Assert.IsTrue(registration.CompletedActivities.Contains(activity));
			}

			ActivityNotSampledWithoutRegistration();
		}
	}
}
