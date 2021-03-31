using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Macross.OpenTelemetry.Extensions.Tests
{
	[TestClass]
	public sealed class ActivityEnrichmentScopeProcessorTests : IDisposable
	{
		private static readonly Action<Activity> s_EnrichmentAction = (a) => a.AddTag("enriched", true);
		private static readonly ActivityEnricher<string> s_Enricher = (a, s) => a.AddTag(s, true);
		private readonly ActivitySource _ActivitySource = new ActivitySource(nameof(ActivityEnrichmentScopeProcessorTests));
		private readonly TestActivityProcessor _TestActivityProcessor = new TestActivityProcessor(collectEndedSpans: true);
		private readonly IDisposable _Sdk;

		public ActivityEnrichmentScopeProcessorTests()
		{
			_Sdk = Sdk.CreateTracerProviderBuilder()
				.AddSource(_ActivitySource.Name)
				.SetSampler(new AlwaysOnSampler())
				.AddActivityEnrichmentScopeProcessor()
				.AddProcessor(_TestActivityProcessor)
				.Build();
		}

		public void Dispose()
		{
			_Sdk.Dispose();
			_TestActivityProcessor.Dispose();
			_ActivitySource.Dispose();
		}

		[TestMethod]
		public void SpanNotEnrichedTest()
		{
			using IDisposable reset = _TestActivityProcessor.ResetWhenDone();

			Activity activity = _ActivitySource.StartActivity("Test", ActivityKind.Internal)!;
			activity.Stop();

			Assert.IsNull(ActivityEnrichmentScopeBase.Current);
			Assert.AreEqual(1, _TestActivityProcessor.EndedActivityObjects.Count);
			Assert.IsFalse(_TestActivityProcessor.EndedActivityObjects[0].TagObjects.Any());
		}

		[TestMethod]
		public void SpansEnrichedActionMethodTest()
		{
			using IDisposable reset = _TestActivityProcessor.ResetWhenDone();

			Activity activity;

			using (ActivityEnrichmentScope.Begin(s_EnrichmentAction))
			{
				activity = _ActivitySource.StartActivity("Test1", ActivityKind.Internal)!;
				activity.Stop();

				activity = _ActivitySource.StartActivity("Test2", ActivityKind.Internal)!;
				activity.Stop();
			}

			activity = _ActivitySource.StartActivity("Test3", ActivityKind.Internal)!;
			activity.Stop();

			Assert.IsNull(ActivityEnrichmentScopeBase.Current);
			Assert.AreEqual(3, _TestActivityProcessor.EndedActivityObjects.Count);
			Assert.IsTrue(_TestActivityProcessor.EndedActivityObjects[0].TagObjects.Any(i => i.Key == "enriched" && (i.Value as bool?) == true));
			Assert.IsTrue(_TestActivityProcessor.EndedActivityObjects[1].TagObjects.Any(i => i.Key == "enriched" && (i.Value as bool?) == true));
			Assert.IsFalse(_TestActivityProcessor.EndedActivityObjects[2].TagObjects.Any());
		}

		[TestMethod]
		public void SpansEnrichedGenericMethodTest()
		{
			using IDisposable reset = _TestActivityProcessor.ResetWhenDone();

			Activity activity;

			using (ActivityEnrichmentScope.Begin(s_Enricher, "enrichment_state"))
			{
				activity = _ActivitySource.StartActivity("Test1", ActivityKind.Internal)!;
				activity.Stop();

				activity = _ActivitySource.StartActivity("Test2", ActivityKind.Internal)!;
				activity.Stop();
			}

			activity = _ActivitySource.StartActivity("Test3", ActivityKind.Internal)!;
			activity.Stop();

			Assert.IsNull(ActivityEnrichmentScopeBase.Current);
			Assert.AreEqual(3, _TestActivityProcessor.EndedActivityObjects.Count);
			Assert.IsTrue(_TestActivityProcessor.EndedActivityObjects[0].TagObjects.Any(i => i.Key == "enrichment_state" && (i.Value as bool?) == true));
			Assert.IsTrue(_TestActivityProcessor.EndedActivityObjects[1].TagObjects.Any(i => i.Key == "enrichment_state" && (i.Value as bool?) == true));
			Assert.IsFalse(_TestActivityProcessor.EndedActivityObjects[2].TagObjects.Any());
		}

		[TestMethod]
		public void DisposeOutOfOrderTest1()
		{
			using ActivityEnrichmentScopeBase scope1 = (ActivityEnrichmentScopeBase)ActivityEnrichmentScope.Begin(s_EnrichmentAction);
			using ActivityEnrichmentScopeBase scope2 = (ActivityEnrichmentScopeBase)ActivityEnrichmentScope.Begin(s_EnrichmentAction);
			using ActivityEnrichmentScopeBase scope3 = (ActivityEnrichmentScopeBase)ActivityEnrichmentScope.Begin(s_EnrichmentAction);

			/* Close order: 2, 1, 3 */

			Assert.IsNotNull(scope1);
			Assert.IsNotNull(scope2);
			Assert.IsNotNull(scope3);

			Assert.IsNull(scope1.Parent);
			Assert.AreEqual(scope1.Child, scope2);
			Assert.AreEqual(scope2.Parent, scope1);
			Assert.AreEqual(scope2.Child, scope3);
			Assert.AreEqual(scope3.Parent, scope2);
			Assert.IsNull(scope3.Child);

			Assert.AreEqual(scope3, ActivityEnrichmentScopeBase.Current);

			DisposeAndVerify(scope2);

			Assert.IsNull(scope1.Parent);
			Assert.AreEqual(scope1.Child, scope3);
			Assert.AreEqual(scope3.Parent, scope1);
			Assert.IsNull(scope3.Child);

			Assert.AreEqual(scope3, ActivityEnrichmentScopeBase.Current);

			DisposeAndVerify(scope1);

			Assert.IsNull(scope3.Parent);
			Assert.IsNull(scope3.Child);

			Assert.AreEqual(scope3, ActivityEnrichmentScopeBase.Current);

			DisposeAndVerify(scope3);

			Assert.IsNull(ActivityEnrichmentScopeBase.Current);
		}

		[TestMethod]
		public void DisposeOutOfOrderTest2()
		{
			using ActivityEnrichmentScopeBase scope1 = (ActivityEnrichmentScopeBase)ActivityEnrichmentScope.Begin(s_EnrichmentAction);
			using ActivityEnrichmentScopeBase scope2 = (ActivityEnrichmentScopeBase)ActivityEnrichmentScope.Begin(s_EnrichmentAction);
			using ActivityEnrichmentScopeBase scope3 = (ActivityEnrichmentScopeBase)ActivityEnrichmentScope.Begin(s_EnrichmentAction);

			/* Close order: 2, 3, 1 */

			Assert.IsNotNull(scope1);
			Assert.IsNotNull(scope2);
			Assert.IsNotNull(scope3);

			Assert.IsNull(scope1.Parent);
			Assert.AreEqual(scope1.Child, scope2);
			Assert.AreEqual(scope2.Parent, scope1);
			Assert.AreEqual(scope2.Child, scope3);
			Assert.AreEqual(scope3.Parent, scope2);
			Assert.IsNull(scope3.Child);

			Assert.AreEqual(scope3, ActivityEnrichmentScopeBase.Current);

			DisposeAndVerify(scope2);

			Assert.IsNull(scope1.Parent);
			Assert.AreEqual(scope1.Child, scope3);
			Assert.AreEqual(scope3.Parent, scope1);
			Assert.IsNull(scope3.Child);

			Assert.AreEqual(scope3, ActivityEnrichmentScopeBase.Current);

			DisposeAndVerify(scope3);

			Assert.IsNull(scope1.Parent);
			Assert.IsNull(scope1.Child);

			Assert.AreEqual(scope1, ActivityEnrichmentScopeBase.Current);

			DisposeAndVerify(scope1);

			Assert.IsNull(ActivityEnrichmentScopeBase.Current);
		}

		[TestMethod]
		public void DisposeOutOfOrderTest3()
		{
			using ActivityEnrichmentScopeBase scope1 = (ActivityEnrichmentScopeBase)ActivityEnrichmentScope.Begin(s_EnrichmentAction);
			using ActivityEnrichmentScopeBase scope2 = (ActivityEnrichmentScopeBase)ActivityEnrichmentScope.Begin(s_EnrichmentAction);
			using ActivityEnrichmentScopeBase scope3 = (ActivityEnrichmentScopeBase)ActivityEnrichmentScope.Begin(s_EnrichmentAction);

			/* Close order: 1, 2, 3 */

			Assert.IsNotNull(scope1);
			Assert.IsNotNull(scope2);
			Assert.IsNotNull(scope3);

			Assert.IsNull(scope1.Parent);
			Assert.AreEqual(scope1.Child, scope2);
			Assert.AreEqual(scope2.Parent, scope1);
			Assert.AreEqual(scope2.Child, scope3);
			Assert.AreEqual(scope3.Parent, scope2);
			Assert.IsNull(scope3.Child);

			Assert.AreEqual(scope3, ActivityEnrichmentScopeBase.Current);

			DisposeAndVerify(scope1);

			Assert.IsNull(scope2.Parent);
			Assert.AreEqual(scope2.Child, scope3);
			Assert.AreEqual(scope3.Parent, scope2);
			Assert.IsNull(scope3.Child);

			Assert.AreEqual(scope3, ActivityEnrichmentScopeBase.Current);

			DisposeAndVerify(scope2);

			Assert.IsNull(scope3.Parent);
			Assert.IsNull(scope3.Child);

			Assert.AreEqual(scope3, ActivityEnrichmentScopeBase.Current);

			DisposeAndVerify(scope3);

			Assert.IsNull(ActivityEnrichmentScopeBase.Current);
		}

		private static void EnsureActivityMatchesTags(Activity activity, params KeyValuePair<string, object>[] tags)
		{
			IEnumerable<KeyValuePair<string, object?>> tagObjects = activity.TagObjects;

			Assert.AreEqual(tags.Length, tagObjects.Count());

			foreach (KeyValuePair<string, object> tag in tags)
			{
				Assert.IsTrue(tagObjects.Any(i => i.Key == tag.Key && i.Value?.ToString() == tag.Value.ToString()));
			}
		}

		private static void DisposeAndVerify(ActivityEnrichmentScopeBase enrichmentScope)
		{
			enrichmentScope.Dispose();

			if (enrichmentScope is ActionActivityEnrichmentScope actionActivityEnrichmentScope)
				Assert.IsNull(actionActivityEnrichmentScope.EnrichmentAction);
			else if (enrichmentScope is GenericActivityEnrichmentScope<string> genericActivityEnrichmentScope)
				Assert.IsNull(genericActivityEnrichmentScope.ActivityEnricher);
			else
				throw new NotSupportedException();
			Assert.IsNull(enrichmentScope.Parent);
			Assert.IsNull(enrichmentScope.Child);
		}
	}
}
