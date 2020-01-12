using System.Security.Principal;

using Microsoft.Win32.SafeHandles;

using Macross.Impersonation;

namespace System.Net
{
	/// <summary>
	/// Methods extending what is provided in the System.Net namespace for impersonation.
	/// </summary>
	public static class ImpersonationExtensions
	{
		/// <summary>
		/// Executes a function while impersonating a user.
		/// </summary>
		/// <typeparam name="T">The Type to be returned.</typeparam>
		/// <param name="credentials"><see cref="ICredentials"/> for the user to use for impersonation.</param>
		/// <param name="netOnly">Whether impersonation should be used for network access only.</param>
		/// <param name="func">The function to execute in the impersonation context.</param>
		/// <returns>Result of the function invocation.</returns>
		public static T RunImpersonated<T>(this ICredentials credentials, bool netOnly, Func<T> func)
		{
			if (credentials == null)
				throw new ArgumentNullException(nameof(credentials));

			NetworkCredential Credentials = credentials.GetCredential(null, null);

			using SafeAccessTokenHandle Token = new SafeAccessTokenHandle(ImpersonationSettings.LogonUser(Credentials.Domain, Credentials.UserName, Credentials.SecurePassword, netOnly));

			return WindowsIdentity.RunImpersonated(Token, func);
		}

		/// <summary>
		/// Executes an action while impersonating a user.
		/// </summary>
		/// <param name="credentials"><see cref="ICredentials"/> for the user to use for impersonation.</param>
		/// <param name="netOnly">Whether impersonation should be used for network access only.</param>
		/// <param name="action">The action to execute in the impersonation context.</param>
		public static void RunImpersonated(this ICredentials credentials, bool netOnly, Action action)
		{
			if (credentials == null)
				throw new ArgumentNullException(nameof(credentials));

			NetworkCredential Credentials = credentials.GetCredential(null, null);

			using SafeAccessTokenHandle Token = new SafeAccessTokenHandle(ImpersonationSettings.LogonUser(Credentials.Domain, Credentials.UserName, Credentials.SecurePassword, netOnly));

			WindowsIdentity.RunImpersonated(Token, action);
		}
	}
}
