using System;
using System.Runtime.InteropServices;

namespace Macross.Impersonation
{
	internal static class NativeMethods
	{
		public const int ERROR_INVALID_PARAMETER = 87;
		public const int ERROR_NO_TOKEN = 1008;

		public enum LogonType : uint
		{
			INTERACTIVE = 2,
			NEW_CREDENTIALS = 9,
		}

		public enum LogonProviderType : uint
		{
			DEFAULT = 0,
			WINNT50 = 3,
		}

		public enum ImpersonationLevel : uint
		{
			SECURITY_ANONYMOUS = 0,
			SECURITY_IDENTIFICATION = 1,
			SECURITY_IMPERSONATION = 2,
			SECURITY_DELEGATION = 3,
		}

		[Flags]
		public enum DuplicateOptions : uint
		{
			CLOSE_SOURCE = 0x00000001, // Closes the source handle. This occurs regardless of any error status returned.
			SAME_ACCESS = 0x00000002, // Ignores the dwDesiredAccess parameter. The duplicate handle has the same access as the source handle.
		}

		[Flags]
		public enum ThreadAccess : uint
		{
			TERMINATE = 0x0001,
			SUSPEND_RESUME = 0x0002,
			GET_CONTEXT = 0x0008,
			SET_CONTEXT = 0x0010,
			SET_INFORMATION = 0x0020,
			QUERY_INFORMATION = 0x0040,
			SET_THREAD_TOKEN = 0x0080,
			IMPERSONATE = 0x0100,
			DIRECT_IMPERSONATION = 0x0200,
		}

		[Flags]
		public enum TokenAccess : uint
		{
			READ = 0x00020008,
			IMPERSONATE = 0x0004,
			MAXIMUM_ALLOWED = 0x2000000,
		}

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LogonUser(
			string lpszUserName,
			string lpszDomain,
			string lpszPassword,
			LogonType dwLogonType,
			LogonProviderType dwLogonProvider,
			out IntPtr phToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LogonUser(
			string lpszUserName,
			string lpszDomain,
			IntPtr pPassword,
			LogonType dwLogonType,
			LogonProviderType dwLogonProvider,
			out IntPtr phToken);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DuplicateToken(
			IntPtr hToken,
			ImpersonationLevel impersonationLevel,
			out IntPtr phNewToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RevertToSelf();

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool OpenThreadToken(
			IntPtr hThread,
			TokenAccess dwDesiredAccess,
			[MarshalAs(UnmanagedType.Bool)]
			bool bOpenAsSelf,
			out IntPtr phToken);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetThreadToken(IntPtr phThread, IntPtr hToken);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetThreadToken(ref IntPtr phThread, IntPtr hToken);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DuplicateHandle(
			IntPtr hSourceProcess,
			IntPtr hSource,
			IntPtr hTargetProcess,
			out IntPtr phTarget,
			uint dwDesiredAccess,
			[MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
			DuplicateOptions dwOptions);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(
			IntPtr handle);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetCurrentThread();

		[DllImport("kernel32.dll")]
		public static extern uint GetCurrentThreadId();

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr OpenThread(
			ThreadAccess dwDesiredAccess,
			[MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
			uint dwThreadId);
	}
}
