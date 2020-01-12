using System;
using System.Runtime.InteropServices;

namespace Macross.Windows.ServiceManagement
{
#pragma warning disable SA1401 // Fields should be private
	internal static class NativeMethods
	{
		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern SafeServiceHandle OpenSCManager(string lpMachineName, string? lpDatabaseName, uint dwAccess);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnumServicesStatus(
			SafeServiceHandle hSCManager,
			uint dwServiceType,
			uint dwServiceState,
			IntPtr lpServices,
			uint cbBufSize,
			out uint pcbBytesNeeded,
			out uint lpServicesReturned,
			ref uint lpResumeHandle);

		/*[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern SafeServiceHandle CreateService(
			 SafeServiceHandle hSCManager,
			 string lpServiceName,
			 string lpDisplayName,
			 uint dwDesiredAccess,
			 uint dwServiceType,
			 uint dwStartType,
			 uint dwErrorControl,
			 string lpBinaryPathName,
			 string? lpLoadOrderGroup,
			 IntPtr lpdwTagId,
			 string? lpDependencies,
			 string? lpServiceStartName,
			 string? lpPassword);*/

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern SafeServiceHandle CreateService(
			 SafeServiceHandle hSCManager,
			 string lpServiceName,
			 string lpDisplayName,
			 uint dwDesiredAccess,
			 uint dwServiceType,
			 uint dwStartType,
			 uint dwErrorControl,
			 string lpBinaryPathName,
			 string? lpLoadOrderGroup,
			 IntPtr lpdwTagId,
			 string? lpDependencies,
			 string? lpServiceStartName,
			 IntPtr lpPassword);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern SafeServiceHandle OpenService(
			SafeServiceHandle hSCManager,
			string lpServiceName,
			uint dwDesiredAccess);

		/*[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ChangeServiceConfig(
			SafeServiceHandle hService,
			uint dwServiceType,
			uint dwStartType,
			uint dwErrorControl,
			string? lpBinaryPathName,
			string? lpLoadOrderGroup,
			IntPtr lpdwTagId,
			string? lpDependencies,
			string? lpServiceStartName,
			string? lpPassword,
			string? lpDisplayName);*/

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ChangeServiceConfig(
			SafeServiceHandle hService,
			uint dwServiceType,
			uint dwStartType,
			uint dwErrorControl,
			string? lpBinaryPathName,
			string? lpLoadOrderGroup,
			IntPtr lpdwTagId,
			string? lpDependencies,
			string? lpServiceStartName,
			IntPtr lpPassword,
			string? lpDisplayName);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ChangeServiceConfig2(
			SafeServiceHandle hService,
			uint dwInfoLevel,
			IntPtr lpInfo);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteService(SafeServiceHandle hService);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool StartService(SafeServiceHandle hService, uint dwNumServiceArgs, IntPtr lpServiceArgVectors);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ControlService(
			SafeServiceHandle hService,
			uint dwControl,
			ref ServiceStatusInfo lpServiceStatus);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseServiceHandle(IntPtr hSCObject);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryServiceConfig(SafeServiceHandle hService, IntPtr lpServiceConfig, uint cbBufSize, out uint pcbBytesNeeded);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryServiceConfig2(SafeServiceHandle hService, uint dwInfoLevel, IntPtr lpBuffer, uint cbBufSize, out uint pcbBytesNeeded);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool QueryServiceStatus(SafeServiceHandle hService, ref ServiceStatusInfo lpServiceStatus);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public class EnumServiceStatusInfo
		{
			public static int SizeOf { get; } = Marshal.SizeOf(typeof(EnumServiceStatusInfo));

			[MarshalAs(UnmanagedType.LPWStr)]
			public string? lpServiceName;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string? lpDisplayName;
			public ServiceStatusInfo ServiceStatus;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class ServiceConfigurationInfo
		{
			[MarshalAs(UnmanagedType.U4)]
			public uint dwServiceType;
			[MarshalAs(UnmanagedType.U4)]
			public uint dwStartType;
			[MarshalAs(UnmanagedType.U4)]
			public uint dwErrorControl;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string? lpBinaryPathName;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string? lpLoadOrderGroup;
			[MarshalAs(UnmanagedType.U4)]
			public uint dwTagID;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string? lpDependencies;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string? lpServiceStartName;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string? lpDisplayName;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class ServiceDelayedAutoStartInfo
		{
			public static int SizeOf { get; } = Marshal.SizeOf(typeof(ServiceDelayedAutoStartInfo));

			[MarshalAs(UnmanagedType.Bool)]
			public bool fDelayedAutostart;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public class ServiceDescriptionInfo
		{
			public static int SizeOf { get; } = Marshal.SizeOf(typeof(ServiceDescriptionInfo));

			[MarshalAs(UnmanagedType.LPWStr)]
			public string? lpDescription;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ServiceStatusInfo
		{
			public static int SizeOf { get; } = Marshal.SizeOf(typeof(ServiceStatusInfo));

			[MarshalAs(UnmanagedType.U4)]
			public uint dwServiceType;
			[MarshalAs(UnmanagedType.U4)]
			public ServiceState dwCurrentState;
			[MarshalAs(UnmanagedType.U4)]
			public uint dwControlsAccepted;
			[MarshalAs(UnmanagedType.U4)]
			public uint dwWin32ExitCode;
			[MarshalAs(UnmanagedType.U4)]
			public uint dwServiceSpecificExitCode;
			[MarshalAs(UnmanagedType.U4)]
			public uint dwCheckPoint;
			[MarshalAs(UnmanagedType.U4)]
			public uint dwWaitHint;
		}

		public const uint SC_MANAGER_ALL_ACCESS = 0x000F003F;
		public const uint SC_MANAGER_ENUMERATE_SERVICE = 0x00004;
		public const uint SC_MANAGER_CONNECT = 0x0001;
		public const uint SC_MANAGER_CREATE_SERVICE = 0x0002;
		public const uint SERVICE_STATE_ALL = 0x00000003;

		public const uint SERVICE_ALL_ACCESS = 0xF01FF;
		public const uint SERVICE_QUERY_CONFIG = 0x00001;
		public const uint SERVICE_QUERY_STATUS = 0x00004;
		public const uint SERVICE_CHANGE_CONFIG = 0x00002;
		public const uint SERVICE_START = 0x0010;
		public const uint SERVICE_STOP = 0x0020;
		public const uint SERVICE_PAUSE_CONTINUE = 0x0040;
		public const uint DELETE = 0x10000;

		public const uint SERVICE_NO_CHANGE = 0xffffffff;
		public const uint SERVICE_CONFIG_DESCRIPTION = 1;
		public const uint SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3;

		public const uint SERVICE_WIN32 = 0x00000030;
		public const uint SERVICE_WIN32_OWN_PROCESS = 0x00000010;
		public const uint SERVICE_WIN32_SHARE_PROCESS = 0x00000020;

		public const uint SERVICE_CONTROL_STOP = 0x00000001;
		public const uint SERVICE_CONTROL_PAUSE = 0x00000002;
		public const uint SERVICE_CONTROL_CONTINUE = 0x00000003;

		public const uint SERVICE_ERROR_NORMAL = 0x00000001;

		public const uint ERROR_MORE_DATA = 234;
		public const uint ERROR_INSUFFICIENT_BUFFER = 122;
	}
#pragma warning restore SA1401 // Fields should be private
}
