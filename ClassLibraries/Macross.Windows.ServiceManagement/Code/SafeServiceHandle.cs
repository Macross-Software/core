using System;

using Microsoft.Win32.SafeHandles;

namespace Macross.Windows.ServiceManagement
{
	internal class SafeServiceHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		private SafeServiceHandle()
			: base(true)
		{
		}

		public SafeServiceHandle(IntPtr handle)
			: base(true)
		{
			SetHandle(handle);
		}

		protected override bool ReleaseHandle() => NativeMethods.CloseServiceHandle(handle);
	}
}
