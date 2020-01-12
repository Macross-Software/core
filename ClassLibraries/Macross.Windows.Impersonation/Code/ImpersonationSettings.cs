using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Security.Principal;

namespace Macross.Impersonation
{
	/// <summary>
	/// Class for managing impersonation on Windows systems.
	/// </summary>
	public class ImpersonationSettings
	{
		private class ImpersonationContext : IDisposable
		{
			private readonly WindowsIdentity _Identity;
			private readonly IntPtr _PreviousIdentityToken;
			private readonly bool _ApplyToAllThreads;

			public ImpersonationContext(WindowsIdentity identity, IntPtr previousIdentityToken, bool applyToAllThreads)
			{
				_Identity = identity ?? throw new ArgumentNullException(nameof(identity));
				_PreviousIdentityToken = previousIdentityToken;
				_ApplyToAllThreads = applyToAllThreads;

				ApplyTokenToThread(_Identity.Token, NativeMethods.GetCurrentThreadId(), applyToAllThreads);
			}

			public void Dispose()
			{
				GC.SuppressFinalize(this);

				try
				{
					if (!NativeMethods.RevertToSelf())
						throw new Win32Exception(Marshal.GetLastWin32Error());

					if (_PreviousIdentityToken != IntPtr.Zero)
					{
						uint CurrentThreadId = NativeMethods.GetCurrentThreadId();

						ApplyTokenToThread(_PreviousIdentityToken, CurrentThreadId, _ApplyToAllThreads);
					}
				}
				finally
				{
					_Identity.Dispose();
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating the name of the domain to authenticate against.
		/// </summary>
		public string? Domain { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the name of the user to use for authentication.
		/// </summary>
		public string? Username { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the password of the user to use for authentication.
		/// </summary>
		public string? Password { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether impersonation should be performed locally or only on the network.
		/// </summary>
		public bool NetOnly { get; set; }

		/// <summary>
		/// Impersonates the configured user on the current thread.
		/// </summary>
		/// <returns>Impersonation context to be disposed when impersonation is complete.</returns>
		public IDisposable Impersonate() => ImpersonateInternal(false);

		/// <summary>
		/// Impersonates the configured user on all threads in the current process.
		/// </summary>
		/// <returns>Impersonation context to be disposed when impersonation is complete.</returns>
		public IDisposable ImpersonateOnAllThreads() => ImpersonateInternal(true);

		private IDisposable ImpersonateInternal(bool applyToAllThreads)
		{
			if (string.IsNullOrEmpty(Domain) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
				throw new ArgumentException("Domain, Username, and Password properties must be set before invoking impersonation.");

			try
			{
				IntPtr PreviousIdentityToken = ReadThreadToken(NativeMethods.GetCurrentThread());
				try
				{
					if (!NativeMethods.RevertToSelf())
						throw new Win32Exception(Marshal.GetLastWin32Error());

					ImpersonationContext Context;

					WindowsIdentity? Identity = null;
					IntPtr IdentityToken = LogonUser(Domain, Username, Password, NetOnly);
					try
					{
						Identity = new WindowsIdentity(IdentityToken);
						Context = new ImpersonationContext(Identity, PreviousIdentityToken, applyToAllThreads);
						Identity = null;
					}
					finally
					{
						Identity?.Dispose();
						NativeMethods.CloseHandle(IdentityToken);
					}

					return Context;
				}
				catch
				{
					if (PreviousIdentityToken != IntPtr.Zero)
						NativeMethods.CloseHandle(PreviousIdentityToken);
					throw;
				}
			}
			catch (Exception ImpersonateException)
			{
				throw new InvalidOperationException($"Impersonation of user [{Domain}\\{Username}] failed.", ImpersonateException);
			}
		}

		internal static IntPtr LogonUser(string domain, string username, string password, bool netOnly)
		{
			IntPtr LogonToken = IntPtr.Zero;
			try
			{
				if (!NativeMethods.LogonUser(
						username,
						domain,
						password,
						netOnly ? NativeMethods.LogonType.NEW_CREDENTIALS : NativeMethods.LogonType.INTERACTIVE,
						NativeMethods.LogonProviderType.WINNT50,
						out LogonToken))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error());
				}

				if (!NativeMethods.DuplicateToken(LogonToken, netOnly ? NativeMethods.ImpersonationLevel.SECURITY_DELEGATION : NativeMethods.ImpersonationLevel.SECURITY_IMPERSONATION, out IntPtr IdentityToken))
					throw new Win32Exception(Marshal.GetLastWin32Error());

				return IdentityToken;
			}
			finally
			{
				if (LogonToken != IntPtr.Zero)
					NativeMethods.CloseHandle(LogonToken);
			}
		}

		internal static IntPtr LogonUser(string domain, string username, SecureString password, bool netOnly)
		{
			IntPtr LogonToken = IntPtr.Zero;
			IntPtr PasswordPointer = IntPtr.Zero;
			try
			{
				PasswordPointer = Marshal.SecureStringToGlobalAllocUnicode(password);

				if (!NativeMethods.LogonUser(
					username,
					domain,
					PasswordPointer,
					netOnly ? NativeMethods.LogonType.NEW_CREDENTIALS : NativeMethods.LogonType.INTERACTIVE,
					NativeMethods.LogonProviderType.WINNT50,
					out LogonToken))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error());
				}

				if (!NativeMethods.DuplicateToken(LogonToken, netOnly ? NativeMethods.ImpersonationLevel.SECURITY_DELEGATION : NativeMethods.ImpersonationLevel.SECURITY_IMPERSONATION, out IntPtr IdentityToken))
					throw new Win32Exception(Marshal.GetLastWin32Error());

				return IdentityToken;
			}
			finally
			{
				Marshal.ZeroFreeGlobalAllocUnicode(PasswordPointer);

				if (LogonToken != IntPtr.Zero)
					NativeMethods.CloseHandle(LogonToken);
			}
		}

		/* I left this in here because I thought it might be useful in the future. Could be used to enumerate all threads and check Access Tokens.
		private static IntPtr ReadThreadHandle(uint threadId)
		{
			IntPtr ThreadHandle = NativeMethods.OpenThread(NativeMethods.ThreadAccess.QUERY_INFORMATION, true, threadId);
			if (ThreadHandle == IntPtr.Zero)
			{
				int LastErrorCode = Marshal.GetLastWin32Error();
				if (LastErrorCode == NativeMethods.ERROR_INVALID_PARAMETER)
					return ThreadHandle;
				throw new Win32Exception(LastErrorCode);
			}
			return ThreadHandle;
		}*/

		private static IntPtr ReadThreadToken(IntPtr threadHandle)
		{
			if (!NativeMethods.OpenThreadToken(threadHandle, NativeMethods.TokenAccess.READ | NativeMethods.TokenAccess.IMPERSONATE, true, out IntPtr ThreadToken))
			{
				int LastErrorCode = Marshal.GetLastWin32Error();
				if (LastErrorCode == NativeMethods.ERROR_NO_TOKEN)
					return IntPtr.Zero;
				throw new Win32Exception(LastErrorCode);
			}

			return ThreadToken;
		}

		private static void ApplyTokenToThread(IntPtr token, uint currentThreadId, bool applyToAllThreads)
		{
			if (applyToAllThreads)
			{
				foreach (ProcessThread Thread in Process.GetCurrentProcess().Threads)
				{
					uint ChildThreadId = (uint)Thread.Id;
					if (currentThreadId == ChildThreadId)
						continue;

					IntPtr ThreadHandle = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SET_THREAD_TOKEN, false, ChildThreadId);
					if (ThreadHandle == IntPtr.Zero)
					{
						int LastErrorCode = Marshal.GetLastWin32Error();
						if (LastErrorCode == NativeMethods.ERROR_INVALID_PARAMETER)
							continue;
						throw new Win32Exception(LastErrorCode);
					}

					try
					{
						if (!NativeMethods.SetThreadToken(ref ThreadHandle, token))
							throw new Win32Exception(Marshal.GetLastWin32Error());
					}
					finally
					{
						NativeMethods.CloseHandle(ThreadHandle);
					}
				}
			}

			if (!NativeMethods.SetThreadToken(IntPtr.Zero, token))
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}
	}
}
