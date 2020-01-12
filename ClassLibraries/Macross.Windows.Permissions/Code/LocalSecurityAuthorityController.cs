using System;
using System.Collections.Generic;
using System.Linq;

namespace Macross.Windows.Permissions
{
	/// <summary>
	/// Class for managing Windows permissions by communicating with the Local Security Authority (LSA).
	/// </summary>
	public class LocalSecurityAuthorityController : IDisposable
	{
		/// <summary>
		/// Gets the Windows Security Identifier (SID) for a given account name.
		/// </summary>
		/// <param name="accountName">Name of the account to query. Use fully qualified domain_name\user_name format.</param>
		/// <param name="machineName">The name of the machine to use for locally-scoped accounts (.\\name). Defaults to local machine name.</param>
		/// <returns>Security Identifier (SID) in binary form.</returns>
		public static byte[] GetSecurityIdentifierForAccount(string accountName, string? machineName = null)
		{
			if (string.IsNullOrEmpty(accountName))
				throw new ArgumentNullException(nameof(accountName));

			try
			{
				return PermissionsHelper.GetAccountSecurityIdentifier(machineName, accountName);
			}
			catch (Exception Exception)
			{
				throw new InvalidOperationException($"SecurityIdentifier could not be determined for [{accountName}] account.", Exception);
			}
		}

		/// <summary>
		/// "Logon as a service" permission name.
		/// </summary>
		public const string LogonAsServicePermissionName = "SeServiceLogonRight";

		private readonly object _SyncObject = new object();
		private IntPtr? _PolicyHandle;

		/// <summary>
		/// Gets the name of the computer being managed.
		/// </summary>
		public string? MachineName { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalSecurityAuthorityController"/> class.
		/// </summary>
		public LocalSecurityAuthorityController()
			: this(null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalSecurityAuthorityController"/> class.
		/// </summary>
		/// <param name="machineName">The name of the computer being managed.</param>
		public LocalSecurityAuthorityController(string? machineName)
		{
			MachineName = machineName;
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="LocalSecurityAuthorityController"/> class.
		/// </summary>
		~LocalSecurityAuthorityController()
		{
			Dispose(false);
		}

		/// <inheritdoc cref="IDisposable.Dispose"/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Query permissions granted to a specific user account on the machine currently being managed.
		/// </summary>
		/// <param name="accountName">Name of the account to query. Use fully qualified domain_name\user_name format.</param>
		/// <returns>Granted permissions.</returns>
		public IEnumerable<string> QueryGrantedAccountPermissions(string accountName)
		{
			if (string.IsNullOrEmpty(accountName))
				throw new ArgumentNullException(nameof(accountName));

			return QueryGrantedAccountPermissions(GetSecurityIdentifierForAccount(accountName, MachineName));
		}

		/// <summary>
		/// Query permissions granted to a specific user account on the machine currently being managed.
		/// </summary>
		/// <param name="accountSecurityIdentifier">SecurityIdentifier of the account to query. Use fully qualified domain_name\user_name format.</param>
		/// <returns>Granted permissions.</returns>
		public IEnumerable<string> QueryGrantedAccountPermissions(byte[] accountSecurityIdentifier)
		{
			if (accountSecurityIdentifier == null)
				throw new ArgumentNullException(nameof(accountSecurityIdentifier));

			EnsureOpenSecurityPolicy();

			return PermissionsHelper.QueryGrantedAccountPermissions(
				_PolicyHandle!.Value,
				accountSecurityIdentifier);
		}

		/// <summary>
		/// Checks if a specific user account on the machine currently being managed is granted a permission.
		/// </summary>
		/// <param name="accountName">Name of the account to query. Use fully qualified domain_name\user_name format.</param>
		/// <param name="permissionName">Name of the permission to query.</param>
		/// <returns>True if the user account is granted the requested permission.</returns>
		public bool IsAccountGrantedPermission(string accountName, string permissionName)
		{
			if (string.IsNullOrEmpty(accountName))
				throw new ArgumentNullException(nameof(accountName));

			return IsAccountGrantedPermission(GetSecurityIdentifierForAccount(accountName, MachineName), permissionName);
		}

		/// <summary>
		/// Checks if a specific user account on the machine currently being managed is granted a permission.
		/// </summary>
		/// <param name="accountSecurityIdentifier">SecurityIdentifier of the account to query. Use fully qualified domain_name\user_name format.</param>
		/// <param name="permissionName">Name of the permission to query.</param>
		/// <returns>True if the user account is granted the requested permission.</returns>
		public bool IsAccountGrantedPermission(byte[] accountSecurityIdentifier, string permissionName)
			=> QueryGrantedAccountPermissions(accountSecurityIdentifier).Any(r => string.Equals(permissionName, r, StringComparison.Ordinal));

		/// <summary>
		/// Grants a permission to a specific user account on the machine currently being managed.
		/// </summary>
		/// <param name="accountName">Name of the account to query. Use fully qualified domain_name\user_name format.</param>
		/// <param name="permissionName">Name of the permission to query.</param>
		public void GrantAccountPermission(string accountName, string permissionName)
		{
			if (string.IsNullOrEmpty(accountName))
				throw new ArgumentNullException(nameof(accountName));

			GrantAccountPermission(GetSecurityIdentifierForAccount(accountName, MachineName), permissionName);
		}

		/// <summary>
		/// Grants a permission to a specific user account on the machine currently being managed.
		/// </summary>
		/// <param name="accountSecurityIdentifier">SecurityIdentifier of the account to query. Use fully qualified domain_name\user_name format.</param>
		/// <param name="permissionName">Name of the permission to query.</param>
		public void GrantAccountPermission(byte[] accountSecurityIdentifier, string permissionName)
		{
			if (accountSecurityIdentifier == null)
				throw new ArgumentNullException(nameof(accountSecurityIdentifier));
			if (string.IsNullOrEmpty(permissionName))
				throw new ArgumentNullException(nameof(permissionName));

			EnsureOpenSecurityPolicy();

			PermissionsHelper.GrantAccountPermission(
				_PolicyHandle!.Value,
				accountSecurityIdentifier,
				permissionName);
		}

		/// <summary>
		/// Revokes a permission from a specific user account on the machine currently being managed.
		/// </summary>
		/// <param name="accountName">Name of the account to query. Use fully qualified domain_name\user_name format.</param>
		/// <param name="permissionName">Name of the permission to query.</param>
		public void RevokeAccountPermission(string accountName, string permissionName)
		{
			if (string.IsNullOrEmpty(accountName))
				throw new ArgumentNullException(nameof(accountName));

			RevokeAccountPermission(GetSecurityIdentifierForAccount(accountName, MachineName), permissionName);
		}

		/// <summary>
		/// Revokes a permission from a specific user account on the machine currently being managed.
		/// </summary>
		/// <param name="accountSecurityIdentifier">SecurityIdentifier of the account to query. Use fully qualified domain_name\user_name format.</param>
		/// <param name="permissionName">Name of the permission to query.</param>
		public void RevokeAccountPermission(byte[] accountSecurityIdentifier, string permissionName)
		{
			if (accountSecurityIdentifier == null)
				throw new ArgumentNullException(nameof(accountSecurityIdentifier));
			if (string.IsNullOrEmpty(permissionName))
				throw new ArgumentNullException(nameof(permissionName));

			EnsureOpenSecurityPolicy();

			PermissionsHelper.RemoveAccountPermission(
				_PolicyHandle!.Value,
				accountSecurityIdentifier,
				permissionName);
		}

		private void EnsureOpenSecurityPolicy()
		{
			if (!_PolicyHandle.HasValue)
			{
				lock (_SyncObject)
				{
					if (!_PolicyHandle.HasValue)
						_PolicyHandle = PermissionsHelper.OpenSecurityPolicy(MachineName);
				}
			}
		}

		/// <summary>
		/// Releases the unmanaged resources used by this class and optionally releases the managed resources.
		/// </summary>
		/// <param name="isDisposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected virtual void Dispose(bool isDisposing)
		{
			if (_PolicyHandle.HasValue)
			{
				PermissionsHelper.CloseSecurityPolicy(_PolicyHandle.Value);
				_PolicyHandle = null;
			}
		}
	}
}
