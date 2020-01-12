using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Macross.Windows.Permissions
{
#pragma warning disable SA1401 // Fields should be private
	internal static class NativeMethods
	{
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetComputerName(
			StringBuilder? lpBuffer,
			ref uint lpdwSize);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LookupAccountName(
			string? lpcwstrSystemName,
			string lpcwstrAccountName,
			[MarshalAs(UnmanagedType.LPArray)]
			byte[]? lpSid,
			ref uint lpdwCbSid,
			StringBuilder? lpwstrReferencedDomainName,
			ref uint lpdwCchReferencedDomainName,
			out uint dwPeUse);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public class LsaUnicodeString
		{
			public static int SizeOf { get; } = Marshal.SizeOf(typeof(LsaUnicodeString));

			public ushort wLength;
			public ushort wMaximumLength;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string? lpBuffer;

			public LsaUnicodeString(string value)
			{
				lpBuffer = value;
				wLength = (ushort)(lpBuffer.Length * 2);
				wMaximumLength = wLength;
			}

			public LsaUnicodeString()
			{
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct LsaObjectAttributes
		{
			public uint dwLength;
			public IntPtr hRootDirectory;
			public LsaUnicodeString lpObjectName;
			public uint dwAttributes;
			public IntPtr lpSecurityDescriptor;
			public IntPtr lpSecurityQualityOfService;
		}

		public const uint LSA_STATUS_OBJECT_NAME_NOT_FOUND = 0xc0000034;

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
		public static extern uint LsaOpenPolicy(
			ref LsaUnicodeString? lpSystemName,
			ref LsaObjectAttributes lpObjectAttributes,
			uint dwDesiredAccess,
			out IntPtr hPolicy);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
		public static extern uint LsaClose(IntPtr hPolicy);

		[DllImport("advapi32.dll")]
		public static extern uint LsaFreeMemory(IntPtr lpBuffer);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
		public static extern int LsaNtStatusToWinError(uint dwNtStatus);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
		public static extern uint LsaAddAccountRights(
			IntPtr hPolicy,
			byte[] lpAccountSid,
			LsaUnicodeString lpUserRights,
			uint dwCountOfRights);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
		public static extern uint LsaRemoveAccountRights(
			IntPtr hPolicy,
			byte[] lpAccountSid,
			[MarshalAs(UnmanagedType.U1)] bool bAllRights,
			LsaUnicodeString lpUserRights,
			uint dwCountOfRights);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
		public static extern uint LsaEnumerateAccountRights(
			IntPtr hPolicy,
			byte[] lpAccountSid,
			out IntPtr lpLsaUnicodeStringUserRights,
			out uint dwCountOfRights);

		public const uint ERROR_BUFFER_OVERFLOW = 111;
		public const uint ERROR_INSUFFICIENT_BUFFER = 122;
	}
#pragma warning restore SA1401 // Fields should be private
}
