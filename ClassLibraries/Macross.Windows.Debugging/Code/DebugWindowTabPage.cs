using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Linq;

namespace Macross.Windows.Debugging
{
	/// <summary>
	/// A <see cref="TabPage"/>-based control for presenting log messages as they are written, by category.
	/// </summary>
	public partial class DebugWindowTabPage : TabPage
	{
		private readonly LinkedList<string> _Messages = new LinkedList<string>();
		private readonly EventWaitHandle _NewMessageHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
		private int _MaxNumberOfDisplayedMessages = 100;

		/// <summary>
		/// Gets the <see cref="DebugWindow"/> hosting the tab.
		/// </summary>
		public new DebugWindow Parent { get; }

		/// <summary>
		/// Gets or sets the maximum number of log messages to display in the control at a time.
		/// </summary>
		public int MaxNumberOfDisplayedMessages
		{
			get => _MaxNumberOfDisplayedMessages;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));
				_MaxNumberOfDisplayedMessages = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether or not the display should constantly scroll to the last message written. Default value: True.
		/// </summary>
		public bool TailEnabled
		{
			get => TailCheckBox.Checked;
			set => TailCheckBox.Checked = value;
		}

		/// <summary>
		/// Gets or sets a value indicating whether or not the display should update automatically.
		/// </summary>
		public bool AutoUpdateEnabled
		{
			get => AutoUpdateCheckBox.Checked;
			set => AutoUpdateCheckBox.Checked = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DebugWindowTabPage"/> class.
		/// </summary>
		/// <param name="parent"><see cref="DebugWindow"/> parent.</param>
		/// <param name="tabTitle">Tab title.</param>
		public DebugWindowTabPage(DebugWindow parent, string tabTitle)
		{
			Parent = parent;

			Text = tabTitle;

			InitializeControls();
		}

		/// <summary>
		/// Adds a message to be displayed in the control.
		/// </summary>
		/// <param name="message">Log message.</param>
		public void AddMessage(string message)
		{
			lock (_Messages)
			{
				_Messages.AddLast(message);
				while (_Messages.Count > MaxNumberOfDisplayedMessages)
					_Messages.RemoveFirst();
				_NewMessageHandle.Set();
			}

			if (Visible)
				Invalidate();
		}

		/// <inheritdoc/>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (AutoUpdateEnabled && _NewMessageHandle.WaitOne(0))
			{
				TurnOffScrollHandlers();
				int ScrollPosition = LogMessageTextBox.GetScrollPosition(Orientation.Vertical);
				lock (_Messages)
				{
					LogMessageTextBox.Lines = _Messages.ToArray();
				}
				if (TailEnabled)
					LogMessageTextBox.ScrollToBottom();
				else
					LogMessageTextBox.SetScrollPosition(Orientation.Vertical, ScrollPosition);
				TurnOnScrollHandlers();
			}

			base.OnPaint(e);
		}

		/// <summary>
		/// Turns on scroll handlers which control toggling on/off tail mode when a user scrolls.
		/// </summary>
		public void TurnOnScrollHandlers()
		{
			LogMessageTextBox.VScroll += OnLogMessageTextBoxScroll;
			LogMessageTextBox.HScroll += OnLogMessageTextBoxScroll;
		}

		/// <summary>
		/// Turns off scroll handlers which control toggling on/off tail mode when a user scrolls.
		/// </summary>
		public void TurnOffScrollHandlers()
		{
			LogMessageTextBox.VScroll -= OnLogMessageTextBoxScroll;
			LogMessageTextBox.HScroll -= OnLogMessageTextBoxScroll;
		}

		private void OnLogMessageTextBoxScroll(object? sender, EventArgs e) => TailEnabled = false;

		private void OnMessagesToShowTextBoxKeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter)
				_ = int.TryParse(MessagesToShowTextBox.Text, out _MaxNumberOfDisplayedMessages);
		}

		private void OnTailCheckBoxCheckedChanged(object? sender, EventArgs e)
		{
			if (TailCheckBox.Checked)
			{
				TurnOffScrollHandlers();
				LogMessageTextBox.ScrollToBottom();
				TurnOnScrollHandlers();
			}
		}

		private void OnClearLogButtonClick(object? sender, EventArgs e)
		{
			lock (_Messages)
			{
				_Messages.Clear();
				LogMessageTextBox.Clear();
			}
		}
	}
}