using System;
using System.Collections.Generic;
using System.Diagnostics;

using OpenTelemetry;

namespace Macross.OpenTelemetry.Extensions.Tests
{
	public class TestActivityProcessor : BaseProcessor<Activity>
	{
		private readonly bool _CollectEndedSpans;

		public Action<Activity>? StartAction { get; set; }

		public Action<Activity>? EndAction { get; set; }

		public bool ShutdownCalled { get; private set; }

		public bool ForceFlushCalled { get; private set; }

		public bool DisposedCalled { get; private set; }

		public List<Activity> EndedActivityObjects { get; } = new List<Activity>();

		public TestActivityProcessor(Action<Activity>? onStartAction = null, Action<Activity>? onEndAction = null, bool collectEndedSpans = false)
		{
			StartAction = onStartAction;
			EndAction = onEndAction;
			_CollectEndedSpans = collectEndedSpans;
		}

		public override void OnStart(Activity span)
			=> StartAction?.Invoke(span);

		public override void OnEnd(Activity span)
		{
			EndAction?.Invoke(span);

			if (_CollectEndedSpans)
			{
				EndedActivityObjects.Add(span);
			}
		}

		protected override bool OnShutdown(int timeoutMilliseconds)
		{
			ShutdownCalled = true;

			return base.OnShutdown(timeoutMilliseconds);
		}

		protected override bool OnForceFlush(int timeoutMilliseconds)
			=> base.OnForceFlush(timeoutMilliseconds);

		public IDisposable ResetWhenDone() => new ResetScope(this);

		public void Reset()
		{
			EndedActivityObjects.Clear();
			ShutdownCalled = false;
			ForceFlushCalled = false;
			DisposedCalled = false;
		}

		protected override void Dispose(bool disposing)
		{
			DisposedCalled = true;

			base.Dispose(disposing);
		}

		private class ResetScope : IDisposable
		{
			private readonly TestActivityProcessor _TestActivityProcessor;

			public ResetScope(TestActivityProcessor testActivityProcessor)
			{
				_TestActivityProcessor = testActivityProcessor;
			}

			public void Dispose()
				=> _TestActivityProcessor.Reset();
		}
	}
}