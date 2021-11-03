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
		private readonly ActivityContext _ActivityContext = new(
			ActivityTraceId.CreateRandom(),
			ActivitySpanId.CreateRandom(),
			ActivityTraceFlags.Recorded,
			isRemote: true);

		private readonly ActivitySource _ActivitySource = new(nameof(ActivityEnrichmentScopeProcessorTests));

		public void Dispose()
			=> _ActivitySource.Dispose();

		[TestMethod]
		public void ActivityNotSampledWithoutRegistration()
		{
			using TestHarness testHarness = new(_ActivitySource.Name);

			Activity? activity = _ActivitySource.StartActivity("Test", ActivityKind.Server, _ActivityContext);

			Assert.IsNull(activity);
		}

		[TestMethod]
		public void ActivitySampledWithRegistration()
		{
			using TestHarness testHarness = new(_ActivitySource.Name);

			using (IActivityTraceListener registration = testHarness.ActivityTraceListenerManager.RegisterTraceListener(_ActivityContext.TraceId))
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

			{
				Activity? activity = _ActivitySource.StartActivity("Test", ActivityKind.Server, _ActivityContext);

				Assert.IsNull(activity);
			}
		}

		[TestMethod]
		public void ActivityNotSampledWithRegistrationAndAutomaticSamplingDisabled()
		{
			using TestHarness testHarness = new(_ActivitySource.Name, o => o.AutomaticallySampleChildren = false);

			using IActivityTraceListener registration = testHarness.ActivityTraceListenerManager.RegisterTraceListener(_ActivityContext.TraceId);

			Activity? activity = _ActivitySource.StartActivity("Test", ActivityKind.Server, _ActivityContext);

			Assert.IsNull(activity);
		}

		private class TestHarness : IDisposable
		{
			private readonly TestActivityProcessor _TestActivityProcessor = new(collectEndedSpans: true);
			private readonly TracerProvider _TracerProvider;
			private readonly ServiceProvider _ServiceProvider;

			public ActivityTraceListenerManager ActivityTraceListenerManager { get; }

			public TestHarness(string sourceName, Action<ActivityTraceListenerManagerOptions>? configureOptions = null)
			{
				_TracerProvider = Sdk.CreateTracerProviderBuilder()
					.AddSource(sourceName)
					.SetActivityTraceListenerSampler(new AlwaysOffSampler())
					.AddProcessor(_TestActivityProcessor)
					.Build();

				IServiceCollection services = new ServiceCollection()
					.AddSingleton(typeof(TracerProvider), _TracerProvider)
					.AddOptions()
					.AddActivityTraceListener();

				if (configureOptions != null)
					services.Configure(configureOptions);

				_ServiceProvider = services.BuildServiceProvider();

				ActivityTraceListenerManager = _ServiceProvider.GetRequiredService<ActivityTraceListenerManager>();
			}

			public void Dispose()
			{
				_TracerProvider.Dispose();
				_TestActivityProcessor.Dispose();
				_ServiceProvider.Dispose();
				ActivityTraceListenerManager.Dispose();
			}
		}
	}
}
