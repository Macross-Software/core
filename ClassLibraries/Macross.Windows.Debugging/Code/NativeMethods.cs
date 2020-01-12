using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Macross.Windows.Debugging
{
	internal static class NativeMethods
	{
		public const int ATTACH_PARENT_PROCESS = -1;
		public const int ERROR_ACCESS_DENIED = 5;
		public const int ERROR_INVALID_HANDLE = 6;

		public const uint WM_HSCROLL = 0x0114;
		public const uint WM_VSCROLL = 0x115;
		public const int WM_SETREDRAW = 11;
		public const uint SB_BOTTOM = 7;
		public const int SW_HIDE = 0;
		public const int SW_SHOW = 5;

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AttachConsole(int dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FreeConsole();

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetConsoleWindow();

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint wMsg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int GetScrollPos(IntPtr hWnd, Orientation nBar);

		[DllImport("user32.dll")]
		public static extern int SetScrollPos(IntPtr hWnd, Orientation nBar, int nPos, bool bRedraw);
	}
}
