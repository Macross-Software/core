namespace Macross.Windows.ServiceManagement
{
	/// <summary>Specifies a service's security context, which defines its logon type.</summary>
	public enum ServiceAccount
	{
		/// <summary>An account that acts as a non-privileged user on the local computer, and presents anonymous credentials to any remote server.</summary>
		LocalService,

		/// <summary>An account that provides extensive local privileges, and presents the computer's credentials to any remote server.</summary>
		NetworkService,

		/// <summary>An account, used by the service control manager, that has extensive privileges on the local computer and acts as the computer on the network.</summary>
		LocalSystem,

		/// <summary>An account defined by a specific user on the network.</summary>
		User,
	}
}
