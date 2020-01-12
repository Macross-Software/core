using Macross.Windows.Debugging;

namespace System.Windows.Forms
{
	/// <summary>
	/// Contains extension methods for extending what is provided by the framework.
	/// </summary>
	public static class WinFormsExtensions
	{
		/// <summary>
		/// Sends the WM_VSCROLL message to a control with the SB_BOTTOM param.
		/// </summary>
		/// <param name="control"><see cref="Control"/>.</param>
		public static void ScrollToBottom(this Control control)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));

			NativeMethods.SendMessage(control.Handle, NativeMethods.WM_VSCROLL, new IntPtr(NativeMethods.SB_BOTTOM), IntPtr.Zero);
		}

		/// <summary>
		/// Reads the scroll position from a control.
		/// </summary>
		/// <param name="control"><see cref="Control"/>.</param>
		/// <param name="orientation"><see cref="Orientation"/>.</param>
		/// <returns>Scroll position read.</returns>
		public static int GetScrollPosition(this Control control, Orientation orientation)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));

			return NativeMethods.GetScrollPos(control.Handle, orientation);
		}

		/// <summary>
		/// Sets the scroll position on a control.
		/// </summary>
		/// <param name="control"><see cref="Control"/>.</param>
		/// <param name="orientation"><see cref="Orientation"/>.</param>
		/// <param name="position">Scroll position.</param>
		public static void SetScrollPosition(this Control control, Orientation orientation, int position)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));

			_ = NativeMethods.SetScrollPos(control.Handle, orientation, position, true);

			NativeMethods.SendMessage(
				control.Handle,
				orientation == Orientation.Horizontal ? NativeMethods.WM_HSCROLL : NativeMethods.WM_VSCROLL,
				new IntPtr((position << 16) + 4),
				IntPtr.Zero);
		}

		/// <summary>
		/// Sends the WM_SETREDRAW message to a control with the false param.
		/// </summary>
		/// <param name="control"><see cref="Control"/>.</param>
		public static void SuspendDrawing(this Control control)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));

			NativeMethods.SendMessage(control.Handle, NativeMethods.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
		}

		/// <summary>
		/// Sends the WM_SETREDRAW message to a control with the true param.
		/// </summary>
		/// <param name="control"><see cref="Control"/>.</param>
		public static void ResumeDrawing(this Control control)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));

			NativeMethods.SendMessage(control.Handle, NativeMethods.WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);

			control.Refresh();
		}
	}
}
