using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.Options;

namespace System.Diagnostics
{
	/// <summary>
	/// A class for managing listeners for <see cref="Activity"/> objects created during a trace.
	/// </summary>
	public class ActivityTraceListenerManager : IDisposable
	{
		private static readonly NoopTraceListener s_DefaultNoopTraceListener = new NoopTraceListener();

		private readonly ConcurrentDictionary<ActivityTraceId, TraceListener> _TraceListeners = new ConcurrentDictionary<ActivityTraceId, TraceListener>();
		private readonly EventWaitHandle _StopHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
		private readonly IDisposable _OptionsChangeToken;
		private ActivityTraceListenerManagerOptions? _Options;
		private Thread? _CleanupThread;
		private ActivityListener? _ActivityListener;
		private bool _HasInitialized;
		private long _LastRequestedListenerDateTimeBinary;

		/// <summary>
		/// Initializes a new instance of the <see cref="ActivityTraceListenerManager"/> class.
		/// </summary>
		/// <param name="options"><see cref="ActivityTraceListenerManagerOptions"/>.</param>
		public ActivityTraceListenerManager(
			IOptionsMonitor<ActivityTraceListenerManagerOptions> options)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));

			ApplyOptions(options.CurrentValue);
			_OptionsChangeToken = options.OnChange(ApplyOptions);
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="ActivityTraceListenerManager"/> class.
		/// </summary>
		~ActivityTraceListenerManager()
		{
			Dispose(false);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Register a listener for the specified <see cref="Activity"/>.
		/// </summary>
		/// <param name="activity"><see cref="Activity"/>.</param>
		/// <returns><see cref="IActivityTraceListener"/>.</returns>
		public IActivityTraceListener RegisterTraceListener(Activity? activity)
		{
			return activity == null
				? s_DefaultNoopTraceListener
				: RegisterTraceListener(activity.TraceId);
		}

		/// <summary>
		/// Register a listener for the specified <see cref="ActivityTraceId"/>.
		/// </summary>
		/// <param name="traceId"><see cref="ActivityTraceId"/>.</param>
		/// <returns><see cref="IActivityTraceListener"/>.</returns>
		public IActivityTraceListener RegisterTraceListener(ActivityTraceId traceId)
		{
			Interlocked.CompareExchange(ref _LastRequestedListenerDateTimeBinary, DateTime.UtcNow.ToBinary(), _LastRequestedListenerDateTimeBinary);

			EnsureInitialized();

			TraceListener listener = new TraceListener(this, traceId);

			return !_TraceListeners.TryAdd(traceId, listener)
				? throw new InvalidOperationException($"A trace listener for TraceId {traceId} is already registered.")
				: listener;
		}

		/// <summary>
		/// Releases the unmanaged resources used by this class and optionally releases the managed resources.
		/// </summary>
		/// <param name="isDisposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool isDisposing)
		{
			if (_CleanupThread != null)
			{
				_StopHandle.Set();
				_CleanupThread.Join();
				_CleanupThread = null;
			}

			if (isDisposing)
			{
				_OptionsChangeToken.Dispose();
				_StopHandle.Dispose();
				_ActivityListener?.Dispose();
			}
		}

		private void OnActivityStopped(Activity activity)
		{
			if (_TraceListeners.TryGetValue(activity.TraceId, out TraceListener? listener))
				listener.Add(activity);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void EnsureInitialized()
		{
			if (_HasInitialized)
				return;

			lock (_TraceListeners)
			{
				if (_HasInitialized)
					return;

				_CleanupThread = new Thread(CleanupThreadBody)
				{
					Name = $"{nameof(ActivityTraceListenerManager)}.Cleanup"
				};
				_CleanupThread.Start();

				// Watch out doing this in prod, it's expensive.
				_ActivityListener = new ActivityListener
				{
					ShouldListenTo = source => true, // Listens to all sources.
					Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData, // Causes all Activity objects to be created and populated.
					ActivityStopped = OnActivityStopped
				};
				ActivitySource.AddActivityListener(_ActivityListener);

				_HasInitialized = true;
			}

			_Options!.OpenedAction?.Invoke();
		}

		private void CleanupThreadBody()
		{
			while (true)
			{
				if (_StopHandle.WaitOne(TimeSpan.FromMilliseconds(_Options!.CleanupIntervalInMilliseconds!.Value)))
					break;

				DateTime lastRequestedListenerUtc = DateTime.FromBinary(_LastRequestedListenerDateTimeBinary);

				if (DateTime.UtcNow - lastRequestedListenerUtc >= TimeSpan.FromMilliseconds(_Options!.CleanupIntervalInMilliseconds!.Value))
				{
					lock (_TraceListeners)
					{
						_HasInitialized = false;
						_ActivityListener?.Dispose();
						_ActivityListener = null;
					}

					_Options.ClosedAction?.Invoke();
					break;
				}
			}
		}

		private void ApplyOptions(ActivityTraceListenerManagerOptions options)
		{
			if (!options.CleanupIntervalInMilliseconds.HasValue || options.CleanupIntervalInMilliseconds <= 0)
				options.CleanupIntervalInMilliseconds = ActivityTraceListenerManagerOptions.DefaultCleanupIntervalInMilliseconds;

			_Options = options;
			_Options.ConfiguredAction?.Invoke();
		}

		private class NoopTraceListener : IActivityTraceListener
		{
			public IReadOnlyList<Activity> CompletedActivities { get; } = new List<Activity>();

			public void Dispose()
			{
			}
		}

		private class TraceListener : IActivityTraceListener
		{
			private readonly List<Activity> _CompletedActivities;

			public ActivityTraceListenerManager Parent { get; }

			public ActivityTraceId TraceId { get; }

			public IReadOnlyList<Activity> CompletedActivities => _CompletedActivities;

			public TraceListener(ActivityTraceListenerManager parent, ActivityTraceId traceId)
			{
				Parent = parent;
				TraceId = traceId;
				_CompletedActivities = new List<Activity>();
			}

			public void Add(Activity activity)
			{
				lock (_CompletedActivities)
				{
					_CompletedActivities.Add(activity);
				}
			}

			public void Dispose()
				=> Parent._TraceListeners.TryRemove(TraceId, out _);
		}
	}
}
