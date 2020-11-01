using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;

namespace Macross.Windows.ServiceManagement
{
	/// <summary>
	/// Class for managing Windows services by communicating with Service Control Manager.
	/// </summary>
	public class ServiceController
	{
		/// <summary>
		/// The Windows API (advapi32) username for LocalService.
		/// </summary>
		public const string LocalServiceWindowsUsername = "NT AUTHORITY\\LocalService";

		/// <summary>
		/// The Windows API (advapi32) username for NetworkService.
		/// </summary>
		public const string NetworkServiceWindowsUsername = "NT AUTHORITY\\NetworkService";

		/// <summary>
		/// Converts the supplied <see cref="ServiceConfiguration"/> format username values into the username expected by the Windows API (advapi32).
		/// </summary>
		/// <param name="account"><see cref="ServiceConfiguration.Account"/>.</param>
		/// <param name="username"><see cref="ServiceConfiguration.Username"/>.</param>
		/// <returns>Converted value.</returns>
		public static string? ConvertServiceConfigurationToWindowsUsername(ServiceAccount account, string? username)
		{
			return account switch
			{
				ServiceAccount.LocalSystem => null,
				ServiceAccount.User => username,
				ServiceAccount.LocalService => LocalServiceWindowsUsername,
				ServiceAccount.NetworkService => NetworkServiceWindowsUsername,
				_ => throw new NotSupportedException($"{nameof(ServiceAccount)}.{account} is not supported."),
			};
		}

		/// <summary>
		/// Converts the supplied Windows API (advapi32) format username into the <see cref="ServiceConfiguration"/> format username values.
		/// </summary>
		/// <param name="username">Windows service username.</param>
		/// <returns>Converted values.</returns>
		public static (ServiceAccount ServiceAccount, string? Username) ConvertWindowsUsernameToServiceConfiguration(string? username)
		{
			return string.IsNullOrEmpty(username) || string.Equals("LocalSystem", username, StringComparison.OrdinalIgnoreCase)
				? (ServiceAccount.LocalSystem, null)
				: string.Equals(NetworkServiceWindowsUsername, username, StringComparison.OrdinalIgnoreCase)
				? (ServiceAccount.NetworkService, null)
				: string.Equals(LocalServiceWindowsUsername, username, StringComparison.OrdinalIgnoreCase)
				? (ServiceAccount.LocalService, null)
				: (ServiceAccount.User, username);
		}

		/// <summary>
		/// Gets the name of the computer being managed.
		/// </summary>
		public string MachineName { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceController"/> class.
		/// </summary>
		public ServiceController()
			: this(null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceController"/> class.
		/// </summary>
		/// <param name="machineName">The name of the computer being managed.</param>
		public ServiceController(string? machineName)
		{
			MachineName = string.IsNullOrEmpty(machineName) ? "." : machineName!;
		}

		/// <summary>
		/// Find services on the machine currently being managed.
		/// </summary>
		/// <returns>List of <see cref="Service"/>s retrieved.</returns>
		public IEnumerable<Service> FindServices()
		{
			try
			{
				return ServiceHelper.FindServices(MachineName);
			}
			catch (Exception ServiceException)
			{
				throw new InvalidOperationException("Services could not be read. If requesting from a remote computer, verify a firewall is not blocking 'Remote Service Management' ports.", ServiceException);
			}
		}

		/// <summary>
		/// Find services matching a predicate on the machine currently being managed.
		/// </summary>
		/// <param name="predicate">A function to test each element for a condition.</param>
		/// <returns>List of <see cref="Service"/>s retrieved and matching the predicate.</returns>
		public IEnumerable<Service> FindServices(Func<Service, bool> predicate)
			=> FindServices().Where(predicate);

		/// <summary>
		/// Find services matching a name on the machine currently being managed.
		/// </summary>
		/// <param name="serviceName">Service name.</param>
		/// <param name="stringComparison">Type of <see cref="StringComparison"/> to use. Default value: <see cref="StringComparison.CurrentCulture"/>.</param>
		/// <returns>List of <see cref="Service"/>s retrieved and matching the service name.</returns>
		public IEnumerable<Service> FindServicesByServiceName(string serviceName, StringComparison stringComparison = StringComparison.CurrentCulture)
			=> FindServices().Where(s => string.Equals(serviceName, s.ServiceName, stringComparison));

		/// <summary>
		/// Creates a service on the machine currently being managed.
		/// </summary>
		/// <param name="serviceConfiguration"><see cref="ServiceConfiguration"/> describing the service to be created.</param>
		/// <param name="binaryPathAndArguments">The fully qualified path to the service binary file optionally including arguments to be passed when starting the service.</param>
		/// <returns>Created <see cref="Service"/>.</returns>
		public Service Create(ServiceConfiguration serviceConfiguration, string binaryPathAndArguments)
		{
			if (serviceConfiguration == null)
				throw new ArgumentNullException(nameof(serviceConfiguration));

			if (string.IsNullOrEmpty(serviceConfiguration.ServiceName))
				throw new InvalidOperationException("ServiceName is required.");

			return Create(
				serviceConfiguration.ServiceName,
				serviceConfiguration.DisplayName,
				serviceConfiguration.Description,
				serviceConfiguration.StartMode,
				serviceConfiguration.Account,
				serviceConfiguration.Username,
				serviceConfiguration.Password,
				binaryPathAndArguments);
		}

		/// <summary>
		/// Creates a service on the machine currently being managed.
		/// </summary>
		/// <param name="serviceName"><see cref="ServiceConfiguration.ServiceName"/>.</param>
		/// <param name="displayName"><see cref="ServiceConfiguration.DisplayName"/>.</param>
		/// <param name="description"><see cref="ServiceConfiguration.Description"/>.</param>
		/// <param name="startMode"><see cref="ServiceConfiguration.StartMode"/>.</param>
		/// <param name="account"><see cref="ServiceConfiguration.Account"/>.</param>
		/// <param name="username"><see cref="ServiceConfiguration.Username"/>.</param>
		/// <param name="password"><see cref="ServiceConfiguration.Password"/>.</param>
		/// <param name="binaryPathAndArguments">The fully qualified path to the service binary file optionally including arguments to be passed when starting the service.</param>
		/// <returns>Created <see cref="Service"/>.</returns>
		public Service Create(
			string serviceName,
			string? displayName,
			string? description,
			ServiceStartMode startMode,
			ServiceAccount account,
			string? username,
			SecureString? password,
			string binaryPathAndArguments)
		{
			string DisplayName = string.IsNullOrEmpty(displayName)
				? serviceName
				: displayName;

			using SafeServiceHandle ServiceManager = ServiceHelper.OpenServiceManager(
				MachineName,
				NativeMethods.SC_MANAGER_CREATE_SERVICE);

			string? ConvertedUsername = ConvertServiceConfigurationToWindowsUsername(account, username);

			IntPtr PasswordPointer = password != null
				? Marshal.SecureStringToGlobalAllocUnicode(password)
				: IntPtr.Zero;

			try
			{
				using SafeServiceHandle ServiceHandle = NativeMethods.CreateService(
					ServiceManager,
					serviceName,
					DisplayName,
					NativeMethods.SERVICE_CHANGE_CONFIG,
					NativeMethods.SERVICE_WIN32_OWN_PROCESS,
					(uint)(startMode == ServiceStartMode.AutomaticDelayedStart ? ServiceStartMode.Automatic : startMode),
					NativeMethods.SERVICE_ERROR_NORMAL,
					binaryPathAndArguments,
					lpLoadOrderGroup: null,
					lpdwTagId: IntPtr.Zero,
					lpDependencies: null,
					ConvertedUsername,
					PasswordPointer);

				if (ServiceHandle.IsInvalid)
					throw new Win32Exception(Marshal.GetLastWin32Error());

				if (description != null)
					ServiceHelper.ChangeServiceDescription(ServiceHandle, description);

				if (startMode == ServiceStartMode.AutomaticDelayedStart)
					ServiceHelper.ChangeServiceDelayedAutoStartConfiguration(ServiceHandle, true);
			}
			finally
			{
				if (PasswordPointer != IntPtr.Zero)
					Marshal.ZeroFreeGlobalAllocUnicode(PasswordPointer);
			}

			return new Service(MachineName, serviceName, DisplayName, ServiceState.Stopped);
		}
	}
}
