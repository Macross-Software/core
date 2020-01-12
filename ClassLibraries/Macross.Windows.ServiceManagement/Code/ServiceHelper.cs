using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Macross.Windows.ServiceManagement
{
	internal static class ServiceHelper
	{
		public static SafeServiceHandle OpenServiceManager(string machineName, uint dwAccess)
		{
			SafeServiceHandle ServiceManager = NativeMethods.OpenSCManager(machineName, null, dwAccess);
			if (ServiceManager.IsInvalid)
				throw new Win32Exception(Marshal.GetLastWin32Error());
			return ServiceManager;
		}

		public static IEnumerable<Service> FindServices(string machineName)
		{
			using SafeServiceHandle ServiceManager = OpenServiceManager(machineName, NativeMethods.SC_MANAGER_ENUMERATE_SERVICE);

			uint ResumeHandle = 0;

			NativeMethods.EnumServicesStatus(ServiceManager, NativeMethods.SERVICE_WIN32, NativeMethods.SERVICE_STATE_ALL, IntPtr.Zero, 0, out uint BytesNeeded, out uint ServicesReturned, ref ResumeHandle);

			int ErrorCode = Marshal.GetLastWin32Error();
			if (ErrorCode != NativeMethods.ERROR_MORE_DATA)
				throw new Win32Exception(ErrorCode);

			IntPtr ptr = Marshal.AllocHGlobal((int)BytesNeeded);
			try
			{
				if (!NativeMethods.EnumServicesStatus(ServiceManager, NativeMethods.SERVICE_WIN32, NativeMethods.SERVICE_STATE_ALL, ptr, BytesNeeded, out BytesNeeded, out ServicesReturned, ref ResumeHandle))
					throw new Win32Exception(Marshal.GetLastWin32Error());

				Collection<Service> Status = new Collection<Service>();

				for (int index = 0; index < ServicesReturned; ++index)
				{
					IntPtr itemPtr = (IntPtr)((long)ptr + (index * NativeMethods.EnumServiceStatusInfo.SizeOf));
					NativeMethods.EnumServiceStatusInfo EnumServiceStatus = new NativeMethods.EnumServiceStatusInfo();
					Marshal.PtrToStructure(itemPtr, EnumServiceStatus);

					Status.Add(new Service(
						machineName,
						EnumServiceStatus.lpServiceName ?? string.Empty,
						EnumServiceStatus.lpDisplayName ?? string.Empty,
						EnumServiceStatus.ServiceStatus.dwCurrentState));
				}

				return Status;
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}
		}

		public static SafeServiceHandle OpenService(SafeServiceHandle serviceManagerHandle, string serviceName, uint dwAccess)
		{
			SafeServiceHandle ServiceHandle = NativeMethods.OpenService(serviceManagerHandle, serviceName, dwAccess);
			if (ServiceHandle.IsInvalid)
				throw new Win32Exception(Marshal.GetLastWin32Error());
			return ServiceHandle;
		}

		public static void ChangeServiceDescription(SafeServiceHandle serviceHandle, string? description)
		{
			NativeMethods.ServiceDescriptionInfo ServiceDescriptionInfo = new NativeMethods.ServiceDescriptionInfo
			{
				lpDescription = description
			};

			IntPtr lpInfo = Marshal.AllocHGlobal(NativeMethods.ServiceDescriptionInfo.SizeOf);

			try
			{
				Marshal.StructureToPtr(ServiceDescriptionInfo, lpInfo, false);

				if (!NativeMethods.ChangeServiceConfig2(
						serviceHandle,
						NativeMethods.SERVICE_CONFIG_DESCRIPTION,
						lpInfo))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error());
				}
			}
			finally
			{
				Marshal.FreeHGlobal(lpInfo);
			}
		}

		public static void ChangeServiceDelayedAutoStartConfiguration(SafeServiceHandle serviceHandle, bool useDelayedAutoStart)
		{
			NativeMethods.ServiceDelayedAutoStartInfo ServiceDelayedAutoStartInfo = new NativeMethods.ServiceDelayedAutoStartInfo
			{
				fDelayedAutostart = useDelayedAutoStart
			};

			IntPtr lpInfo = Marshal.AllocHGlobal(NativeMethods.ServiceDelayedAutoStartInfo.SizeOf);

			try
			{
				Marshal.StructureToPtr(ServiceDelayedAutoStartInfo, lpInfo, false);

				if (!NativeMethods.ChangeServiceConfig2(
					serviceHandle,
					NativeMethods.SERVICE_CONFIG_DELAYED_AUTO_START_INFO,
					lpInfo))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error());
				}
			}
			finally
			{
				Marshal.FreeHGlobal(lpInfo);
			}
		}

		public static NativeMethods.ServiceStatusInfo ReadServiceStatus(SafeServiceHandle serviceHandle)
		{
			NativeMethods.ServiceStatusInfo ServiceStatus = default;

			if (!NativeMethods.QueryServiceStatus(serviceHandle, ref ServiceStatus))
				throw new Win32Exception(Marshal.GetLastWin32Error());

			return ServiceStatus;
		}

		public static (ServiceConfiguration ServiceConfiguration, string BinaryPath) ReadServiceConfiguration(SafeServiceHandle serviceHandle, string serviceName)
		{
			NativeMethods.ServiceConfigurationInfo ServiceConfiguration = ReadServiceConfiguration(serviceHandle);

			(ServiceAccount Account, string? Username) = ServiceController.ConvertWindowsUsernameToServiceConfiguration(ServiceConfiguration.lpServiceStartName);

			ServiceStartMode StartMode = ReadServiceDelayedAutoStartConfiguration(serviceHandle)
				? ServiceStartMode.AutomaticDelayedStart
				: (ServiceStartMode)ServiceConfiguration.dwStartType;

			return (
				new ServiceConfiguration
				{
					ServiceName = serviceName,
					DisplayName = ServiceConfiguration.lpDisplayName,
					Description = ReadServiceDescription(serviceHandle),
					StartMode = StartMode,
					Account = Account,
					Username = Username
				},
				ServiceConfiguration.lpBinaryPathName ?? string.Empty);
		}

		private static NativeMethods.ServiceConfigurationInfo ReadServiceConfiguration(SafeServiceHandle serviceHandle)
		{
			NativeMethods.QueryServiceConfig(serviceHandle, IntPtr.Zero, 0, out uint BytesNeeded);

			int ErrorCode = Marshal.GetLastWin32Error();
			if (ErrorCode != NativeMethods.ERROR_INSUFFICIENT_BUFFER)
				throw new Win32Exception(ErrorCode);

			IntPtr ptr = Marshal.AllocHGlobal((int)BytesNeeded);
			try
			{
				if (!NativeMethods.QueryServiceConfig(serviceHandle, ptr, BytesNeeded, out BytesNeeded))
					throw new Win32Exception(Marshal.GetLastWin32Error());

				NativeMethods.ServiceConfigurationInfo ServiceConfiguration = new NativeMethods.ServiceConfigurationInfo();

				Marshal.PtrToStructure(ptr, ServiceConfiguration);

				return ServiceConfiguration;
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}
		}

		private static string? ReadServiceDescription(SafeServiceHandle serviceHandle)
		{
			NativeMethods.QueryServiceConfig2(serviceHandle, NativeMethods.SERVICE_CONFIG_DESCRIPTION, IntPtr.Zero, 0, out uint BytesNeeded);

			int ErrorCode = Marshal.GetLastWin32Error();
			if (ErrorCode != NativeMethods.ERROR_INSUFFICIENT_BUFFER)
				throw new Win32Exception(ErrorCode);

			IntPtr ptr = Marshal.AllocHGlobal((int)BytesNeeded);
			try
			{
				if (!NativeMethods.QueryServiceConfig2(serviceHandle, NativeMethods.SERVICE_CONFIG_DESCRIPTION, ptr, BytesNeeded, out BytesNeeded))
					throw new Win32Exception(Marshal.GetLastWin32Error());

				NativeMethods.ServiceDescriptionInfo ServiceDescription = new NativeMethods.ServiceDescriptionInfo();

				Marshal.PtrToStructure(ptr, ServiceDescription);

				return ServiceDescription.lpDescription;
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}
		}

		private static bool ReadServiceDelayedAutoStartConfiguration(SafeServiceHandle serviceHandle)
		{
			NativeMethods.QueryServiceConfig2(serviceHandle, NativeMethods.SERVICE_CONFIG_DELAYED_AUTO_START_INFO, IntPtr.Zero, 0, out uint BytesNeeded);

			int ErrorCode = Marshal.GetLastWin32Error();
			if (ErrorCode != NativeMethods.ERROR_INSUFFICIENT_BUFFER)
				throw new Win32Exception(ErrorCode);

			IntPtr ptr = Marshal.AllocHGlobal((int)BytesNeeded);
			try
			{
				if (!NativeMethods.QueryServiceConfig2(serviceHandle, NativeMethods.SERVICE_CONFIG_DELAYED_AUTO_START_INFO, ptr, BytesNeeded, out BytesNeeded))
					throw new Win32Exception(Marshal.GetLastWin32Error());

				NativeMethods.ServiceDelayedAutoStartInfo ServiceDelayedAutoStart = new NativeMethods.ServiceDelayedAutoStartInfo();

				Marshal.PtrToStructure(ptr, ServiceDelayedAutoStart);

				return ServiceDelayedAutoStart.fDelayedAutostart;
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}
		}
	}
}
