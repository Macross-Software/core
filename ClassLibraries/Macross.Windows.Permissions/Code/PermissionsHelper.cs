using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Macross.Windows.Permissions
{
	internal static class PermissionsHelper
	{
		public static IntPtr OpenSecurityPolicy(string? machineName)
		{
			NativeMethods.LsaUnicodeString? systemName = !string.IsNullOrEmpty(machineName)
				? new NativeMethods.LsaUnicodeString(machineName)
				: null;

			NativeMethods.LsaObjectAttributes objectAttributes = default;

			uint ntStatus = NativeMethods.LsaOpenPolicy(ref systemName, ref objectAttributes, 2064, out IntPtr PolicyHandle);
			if (ntStatus != 0)
				throw new Win32Exception(NativeMethods.LsaNtStatusToWinError(ntStatus));
			return PolicyHandle;
		}

		public static void CloseSecurityPolicy(IntPtr policyHandle)
		{
			uint ntStatus = NativeMethods.LsaClose(policyHandle);
			if (ntStatus != 0)
				throw new Win32Exception(NativeMethods.LsaNtStatusToWinError(ntStatus));
		}

		public static IEnumerable<string> QueryGrantedAccountPermissions(IntPtr policyHandle, byte[] accountSid)
		{
			ICollection<string> GrantedAccountPermissions = new Collection<string>();

			uint ntStatus = NativeMethods.LsaEnumerateAccountRights(policyHandle, accountSid, out IntPtr lpUserRights, out uint RightsCount);
			switch (ntStatus)
			{
				case NativeMethods.LSA_STATUS_OBJECT_NAME_NOT_FOUND:
					return GrantedAccountPermissions;
				case 0:
					try
					{
						IntPtr lpCurrentUserRight = lpUserRights;
						for (int index = 0; index < RightsCount; ++index)
						{
							NativeMethods.LsaUnicodeString UserPermission = Marshal.PtrToStructure<NativeMethods.LsaUnicodeString>(lpCurrentUserRight);
							if (!string.IsNullOrEmpty(UserPermission.lpBuffer))
								GrantedAccountPermissions.Add(UserPermission.lpBuffer);
							lpCurrentUserRight = (IntPtr)((long)lpCurrentUserRight + NativeMethods.LsaUnicodeString.SizeOf);
						}
					}
					finally
					{
						ntStatus = NativeMethods.LsaFreeMemory(lpUserRights);
					}
					if (ntStatus != 0)
						throw new Win32Exception(NativeMethods.LsaNtStatusToWinError(ntStatus));
					return GrantedAccountPermissions;
				default:
					throw new Win32Exception(NativeMethods.LsaNtStatusToWinError(ntStatus));
			}
		}

		public static void GrantAccountPermission(IntPtr policyHandle, byte[] accountSid, string permissionName)
		{
			NativeMethods.LsaUnicodeString UserPermission = new NativeMethods.LsaUnicodeString(permissionName);
			uint ntStatus = NativeMethods.LsaAddAccountRights(policyHandle, accountSid, UserPermission, 1);
			if (ntStatus != 0)
				throw new Win32Exception(NativeMethods.LsaNtStatusToWinError(ntStatus));
		}

		public static void RemoveAccountPermission(IntPtr policyHandle, byte[] accountSid, string permissionName)
		{
			NativeMethods.LsaUnicodeString UserPermission = new NativeMethods.LsaUnicodeString(permissionName);
			uint ntStatus = NativeMethods.LsaRemoveAccountRights(policyHandle, accountSid, false, UserPermission, 1);
			if (ntStatus != 0)
				throw new Win32Exception(NativeMethods.LsaNtStatusToWinError(ntStatus));
		}

		public static byte[] GetAccountSecurityIdentifier(string? machineName, string accountName)
		{
			if (accountName.Substring(0, 2) == ".\\")
			{
				StringBuilder lpBuffer = !string.IsNullOrEmpty(machineName) ? new StringBuilder(machineName) : GetComputerName();
				lpBuffer.Append(accountName.Substring(1));
				accountName = lpBuffer.ToString();
			}

			uint sidLength = 0;
			uint domainNameLength = 0;

			if (NativeMethods.LookupAccountName(
				null,
				accountName,
				null,
				ref sidLength,
				null,
				ref domainNameLength,
				out _))
			{
				throw new InvalidOperationException();
			}

			int ErrorCode = Marshal.GetLastWin32Error();
			if (ErrorCode != NativeMethods.ERROR_INSUFFICIENT_BUFFER)
				throw new Win32Exception(ErrorCode);

			byte[] sid = new byte[sidLength];
			StringBuilder domainName = new StringBuilder((int)domainNameLength);

			if (!NativeMethods.LookupAccountName(
				null,
				accountName,
				sid,
				ref sidLength,
				domainName,
				ref domainNameLength,
				out _))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}

			return sid;
		}

		public static StringBuilder GetComputerName()
		{
			uint nSize = 0;
			if (NativeMethods.GetComputerName(null, ref nSize))
				throw new InvalidOperationException();

			int ErrorCode = Marshal.GetLastWin32Error();
			if (ErrorCode != NativeMethods.ERROR_BUFFER_OVERFLOW)
				throw new Win32Exception(ErrorCode);

			StringBuilder lpBuffer = new StringBuilder((int)nSize - 1);
			if (!NativeMethods.GetComputerName(lpBuffer, ref nSize))
				throw new Win32Exception(Marshal.GetLastWin32Error());
			return lpBuffer;
		}
	}
}
