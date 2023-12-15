using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace Macross.Windows.ServiceManagement
{
	/// <summary>
	/// Describes the status of a Windows Service registered with Service Control Manager.
	/// </summary>
	public class Service
	{
		private readonly object _SyncObject = new object();

		private bool _FullConfigurationLoaded;
		private string _Description = string.Empty;
		private ServiceStartMode _StartMode = ServiceStartMode.Manual;
		private ServiceAccount _Account = ServiceAccount.User;
		private string? _Username;
		private string _BinaryPathAndArguments = string.Empty;

		/// <summary>
		/// Gets the name of the computer on which this service resides.
		/// </summary>
		public string MachineName { get; }

		/// <summary>
		/// Gets the primary ServiceName of the service.
		/// </summary>
		public string ServiceName { get; }

		/// <summary>
		/// Gets the friendly DisplayName of the service.
		/// </summary>
		public string DisplayName { get; private set; }

		/// <summary>
		/// Gets the current <see cref="ServiceState"/> of the service.
		/// </summary>
		public ServiceState State { get; private set; }

		/// <summary>
		/// Gets the Description of the service.
		/// </summary>
		public string Description
		{
			get
			{
				EnsureLoadedConfiguration();
				return _Description;
			}
		}

		/// <summary>
		/// Gets the <see cref="ServiceStartMode"/> of the service.
		/// </summary>
		public ServiceStartMode StartMode
		{
			get
			{
				EnsureLoadedConfiguration();
				return _StartMode;
			}
		}

		/// <summary>
		/// Gets the <see cref="ServiceAccount"/> of the service.
		/// </summary>
		public ServiceAccount Account
		{
			get
			{
				EnsureLoadedConfiguration();
				return _Account;
			}
		}

		/// <summary>
		/// Gets the user account under which the service application will run when <see cref="ServiceAccount.User"/> is used.
		/// </summary>
		public string? Username
		{
			get
			{
				EnsureLoadedConfiguration();
				return _Username;
			}
		}

		/// <summary>
		/// Gets the fully qualified path to the service binary file.
		/// </summary>
		/// <remarks>
		/// The path can also include arguments for an auto-start service. These arguments are passed to the service entry point (typically the main function).
		/// </remarks>
		public string BinaryPathAndArguments
		{
			get
			{
				EnsureLoadedConfiguration();
				return _BinaryPathAndArguments;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Service"/> class.
		/// </summary>
		/// <param name="machineName">The computer on which the service resides.</param>
		/// <param name="serviceName">Service name.</param>
		/// <param name="displayName">Service display name.</param>
		/// <param name="serviceState">Service state.</param>
		public Service(string machineName, string serviceName, string displayName, ServiceState serviceState)
		{
			MachineName = machineName;
			ServiceName = serviceName;
			DisplayName = displayName;
			State = serviceState;
		}

		/// <summary>
		/// Starts a service.
		/// </summary>
		public void Start() => Start(null);

		/// <summary>
		/// Starts a service.
		/// </summary>
		/// <param name="arguments">An array of arguments to pass to the service when it starts.</param>
		public void Start(string?[]? arguments)
		{
			arguments ??= Array.Empty<string>();

			using SafeServiceHandle ServiceManager = ServiceHelper.OpenServiceManager(
				MachineName,
				NativeMethods.SC_MANAGER_CONNECT);

			using SafeServiceHandle ServiceHandle = ServiceHelper.OpenService(
				ServiceManager,
				ServiceName,
				NativeMethods.SERVICE_START);

			IntPtr[] ArumentPointers = new IntPtr[arguments.Length];
			int ArgumentIndex = 0;
			try
			{
				for (ArgumentIndex = 0; ArgumentIndex < arguments.Length; ArgumentIndex++)
				{
					if (arguments[ArgumentIndex] == null)
						throw new ArgumentException($"Argument at index {ArgumentIndex} is null which cannot be passed to service.", nameof(arguments));
					ArumentPointers[ArgumentIndex] = Marshal.StringToHGlobalUni(arguments[ArgumentIndex]);
				}

				GCHandle gcHandle = GCHandle.Alloc(ArumentPointers, GCHandleType.Pinned);
				try
				{
					if (!NativeMethods.StartService(ServiceHandle, (uint)arguments.Length, gcHandle.AddrOfPinnedObject()))
						throw new Win32Exception(Marshal.GetLastWin32Error());
				}
				finally
				{
					if (gcHandle.IsAllocated)
						gcHandle.Free();
				}
			}
			finally
			{
				for (int i = 0; i < ArgumentIndex; i++)
					Marshal.FreeHGlobal(ArumentPointers[i]);
			}
		}

		/// <summary>
		/// Suspends a service's operation.
		/// </summary>
		public void Pause() => ControlService(NativeMethods.SERVICE_PAUSE_CONTINUE, NativeMethods.SERVICE_CONTROL_PAUSE);

		/// <summary>
		/// Continues a service after it has been paused.
		/// </summary>
		public void Continue() => ControlService(NativeMethods.SERVICE_PAUSE_CONTINUE, NativeMethods.SERVICE_CONTROL_CONTINUE);

		/// <summary>
		/// Stops a service.
		/// </summary>
		public void Stop() => ControlService(NativeMethods.SERVICE_STOP, NativeMethods.SERVICE_CONTROL_STOP);

		/// <summary>
		/// Configure a service.
		/// </summary>
		/// <param name="displayName"><see cref="ServiceConfiguration.DisplayName"/>.</param>
		/// <param name="description"><see cref="ServiceConfiguration.Description"/>.</param>
		/// <param name="startMode"><see cref="ServiceConfiguration.StartMode"/>.</param>
		/// <param name="account"><see cref="ServiceConfiguration.Account"/>.</param>
		/// <param name="username"><see cref="ServiceConfiguration.Username"/>.</param>
		/// <param name="password"><see cref="ServiceConfiguration.Password"/>.</param>
		/// <param name="binaryPathAndArguments">The fully qualified path to the service binary file optionally including arguments to be passed when starting the service. Specify null to leave the current value unchanged.</param>
		public void Configure(
			string? displayName,
			string? description,
			ServiceStartMode startMode,
			ServiceAccount account,
			string? username,
			SecureString? password,
			string? binaryPathAndArguments = null)
		{
			string DisplayName = string.IsNullOrEmpty(displayName)
				? ServiceName
				: displayName;

			using SafeServiceHandle ServiceManager = ServiceHelper.OpenServiceManager(
				MachineName,
				NativeMethods.SC_MANAGER_CONNECT);

			using SafeServiceHandle ServiceHandle = ServiceHelper.OpenService(
				ServiceManager,
				ServiceName,
				NativeMethods.SERVICE_CHANGE_CONFIG);

			string? ConvertedUsername = ServiceController.ConvertServiceConfigurationToWindowsUsername(
				account,
				username);

			IntPtr PasswordPointer = password != null
				? Marshal.SecureStringToGlobalAllocUnicode(password)
				: IntPtr.Zero;

			try
			{
				if (!NativeMethods.ChangeServiceConfig(
					ServiceHandle,
					dwServiceType: NativeMethods.SERVICE_NO_CHANGE,
					(uint)(startMode == ServiceStartMode.AutomaticDelayedStart ? ServiceStartMode.Automatic : startMode),
					dwErrorControl: NativeMethods.SERVICE_NO_CHANGE,
					binaryPathAndArguments,
					lpLoadOrderGroup: null,
					lpdwTagId: IntPtr.Zero,
					lpDependencies: null,
					ConvertedUsername,
					PasswordPointer,
					DisplayName))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error());
				}
			}
			finally
			{
				if (PasswordPointer != IntPtr.Zero)
					Marshal.ZeroFreeGlobalAllocUnicode(PasswordPointer);
			}

			this.DisplayName = DisplayName;

			ServiceHelper.ChangeServiceDescription(ServiceHandle, description);

			ServiceHelper.ChangeServiceDelayedAutoStartConfiguration(ServiceHandle, startMode == ServiceStartMode.AutomaticDelayedStart);

			lock (_SyncObject)
			{
				_FullConfigurationLoaded = false;
			}
		}

		/// <summary>
		/// Deletes a service.
		/// </summary>
		public void Delete()
		{
			using SafeServiceHandle ServiceManager = ServiceHelper.OpenServiceManager(
				MachineName,
				NativeMethods.SC_MANAGER_CONNECT);

			using SafeServiceHandle ServiceHandle = ServiceHelper.OpenService(
				ServiceManager,
				ServiceName,
				NativeMethods.DELETE);

			if (!NativeMethods.DeleteService(ServiceHandle))
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		/// <summary>
		/// Refresh the status information read from the Service Control Manager managing the service.
		/// </summary>
		public void Refresh()
		{
			using SafeServiceHandle ServiceManager = ServiceHelper.OpenServiceManager(
				MachineName,
				NativeMethods.SC_MANAGER_CONNECT);

			uint dwAccess = NativeMethods.SERVICE_QUERY_STATUS;
			if (_FullConfigurationLoaded)
				dwAccess |= NativeMethods.SERVICE_QUERY_CONFIG;

			using SafeServiceHandle ServiceHandle = ServiceHelper.OpenService(
				ServiceManager,
				ServiceName,
				dwAccess);

			RefreshStatus(ServiceHandle);

			if ((dwAccess & NativeMethods.SERVICE_QUERY_CONFIG) > 0)
				RefreshConfiguration(ServiceHandle);
		}

		/// <summary>
		/// Waits for the service to reach the specified state.
		/// </summary>
		/// <param name="state">The <see cref="ServiceState"/> to wait for.</param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
		/// <returns><see cref="Task"/> for the asynchronous operation.</returns>
		public Task WaitForState(ServiceState state, CancellationToken? cancellationToken = null)
			=> WaitForStates(state, null, cancellationToken);

		/// <summary>
		/// Waits for the service to reach one of the specified states.
		/// </summary>
		/// <param name="states">A list of one or more <see cref="ServiceState"/>s to wait for.</param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
		/// <returns><see cref="Task{TResult}"/> for the asynchronous operation.</returns>
		public Task<ServiceState> WaitForStates(IEnumerable<ServiceState> states, CancellationToken? cancellationToken = null)
		{
			if (!states.Any())
				throw new ArgumentException("At least one ServiceState must be provided.", nameof(states));

			return WaitForStates(null, states, cancellationToken);
		}

		private Task<ServiceState> WaitForStates(ServiceState? state, IEnumerable<ServiceState>? states, CancellationToken? cancellationToken = null)
		{
			cancellationToken ??= CancellationToken.None;

			return Task.Run(async () =>
			{
				while (true)
				{
					Refresh();

					if ((state.HasValue && State == state)
						|| (states != null && states.Contains(State)))
					{
						return State;
					}

					await Task.Delay(250, cancellationToken.Value).ConfigureAwait(false);
				}
			});
		}

		private void EnsureLoadedConfiguration()
		{
			if (_FullConfigurationLoaded)
				return;

			lock (_SyncObject)
			{
				if (_FullConfigurationLoaded)
					return;

				using SafeServiceHandle ServiceManager = ServiceHelper.OpenServiceManager(
					MachineName,
					NativeMethods.SC_MANAGER_CONNECT);

				using SafeServiceHandle ServiceHandle = ServiceHelper.OpenService(
					ServiceManager,
					ServiceName,
					NativeMethods.SERVICE_QUERY_CONFIG | NativeMethods.SERVICE_QUERY_STATUS);

				RefreshConfiguration(ServiceHandle);

				_FullConfigurationLoaded = true;
			}
		}

		private void RefreshStatus(SafeServiceHandle serviceHandle)
		{
			NativeMethods.ServiceStatusInfo Status = ServiceHelper.ReadServiceStatus(serviceHandle);

			State = Status.dwCurrentState;
		}

		private void RefreshConfiguration(SafeServiceHandle serviceHandle)
		{
			(ServiceConfiguration ServiceConfiguration, string BinaryPath) = ServiceHelper.ReadServiceConfiguration(serviceHandle, ServiceName);

			_Description = ServiceConfiguration.Description ?? string.Empty;
			_StartMode = ServiceConfiguration.StartMode;
			_Account = ServiceConfiguration.Account;
			_Username = ServiceConfiguration.Username;

			_BinaryPathAndArguments = BinaryPath;
		}

		private void ControlService(uint dwAccess, uint dwControl)
		{
			using SafeServiceHandle ServiceManager = ServiceHelper.OpenServiceManager(
				MachineName,
				NativeMethods.SC_MANAGER_CONNECT);

			using SafeServiceHandle ServiceHandle = ServiceHelper.OpenService(
				ServiceManager,
				ServiceName,
				dwAccess);

			NativeMethods.ServiceStatusInfo Status = default;

			if (!NativeMethods.ControlService(ServiceHandle, dwControl, ref Status))
				throw new Win32Exception(Marshal.GetLastWin32Error());

			State = Status.dwCurrentState;
		}
	}
}
