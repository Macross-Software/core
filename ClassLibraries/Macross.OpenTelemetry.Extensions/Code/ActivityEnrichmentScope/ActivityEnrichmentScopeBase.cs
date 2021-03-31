using System;
using System.Diagnostics;

using OpenTelemetry.Context;

namespace Macross.OpenTelemetry.Extensions
{
	internal abstract class ActivityEnrichmentScopeBase : IDisposable
	{
		private static readonly RuntimeContextSlot<ActivityEnrichmentScopeBase?> s_RuntimeContextSlot = RuntimeContext.RegisterSlot<ActivityEnrichmentScopeBase?>("otel.activity_enrichment_scope");

		public static ActivityEnrichmentScopeBase? Current => s_RuntimeContextSlot.Get();

		private bool _Disposed;

		public ActivityEnrichmentScopeBase? Parent { get; private set; }

		public ActivityEnrichmentScopeBase? Child { get; private set; }

		protected ActivityEnrichmentScopeBase()
		{
			Parent = Current;
			if (Parent != null)
			{
				Parent.Child = this;
			}

			s_RuntimeContextSlot.Set(this);
		}

		public void Dispose()
		{
			if (!_Disposed)
			{
				Dispose(true);
				_Disposed = true;
			}
		}

		public abstract void Enrich(Activity activity);

		protected virtual void Dispose(bool isDisposing)
		{
			if (Parent?.Child == this)
			{
				Parent.Child = Child;
			}

			if (Child?.Parent == this)
			{
				Child.Parent = Parent;
			}

			if (s_RuntimeContextSlot.Get() == this)
			{
				s_RuntimeContextSlot.Set(Parent);
			}

			Parent = null;
			Child = null;
		}
	}
}