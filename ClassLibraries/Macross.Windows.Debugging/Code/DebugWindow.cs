using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading.Channels;
using System.Text.Json;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Macross.Logging;

namespace Macross.Windows.Debugging
{
	/// <summary>
	/// A <see cref="Form"/>-based control for presenting log messages as they are written.
	/// </summary>
	public partial class DebugWindow : Form
	{
		private readonly IHostEnvironment _HostEnvironment;
		private readonly DebugWindowMessageManager _MessageManager;
		private readonly IOptionsMonitor<DebugWindowLoggerOptions> _Options;
		private readonly CancellationTokenSource _CloseCancellationToken = new CancellationTokenSource();
		private readonly IDisposable _OptionsReloadToken;
		private readonly DebugWindowConfigureAction? _ConfigureAction;
		private readonly DebugWindowConfigureTabAction? _ConfigureTabAction;

		private readonly Dictionary<string, DebugWindowTabPage> _Tabs = new Dictionary<string, DebugWindowTabPage>(StringComparer.OrdinalIgnoreCase);
		private LoggerGroupCache? _LoggerGroupCache;

		/// <summary>
		/// Gets the <see cref="DebugWindowLoggerOptions"/> associated with the control.
		/// </summary>
		public DebugWindowLoggerOptions Options => _Options.CurrentValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="DebugWindow"/> class.
		/// </summary>
		/// <param name="hostEnvironment"><see cref="IHostEnvironment"/>.</param>
		/// <param name="messageManager"><see cref="DebugWindowMessageManager"/>.</param>
		/// <param name="options"><see cref="DebugWindowLoggerOptions"/>.</param>
		/// <param name="configureAction"><see cref="DebugWindowConfigureAction"/>.</param>
		/// <param name="configureTabAction"><see cref="DebugWindowConfigureTabAction"/>.</param>
		public DebugWindow(
			IHostEnvironment hostEnvironment,
			DebugWindowMessageManager messageManager,
			IOptionsMonitor<DebugWindowLoggerOptions> options,
			DebugWindowConfigureAction? configureAction = null,
			DebugWindowConfigureTabAction? configureTabAction = null)
		{
			_HostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
			_MessageManager = messageManager ?? throw new ArgumentNullException(nameof(messageManager));
			_Options = options ?? throw new ArgumentNullException(nameof(options));

			_ConfigureAction = configureAction;
			_ConfigureTabAction = configureTabAction;

			InitializeControls();

			ApplyOptions(options.CurrentValue);
			_OptionsReloadToken = _Options.OnChange(ApplyOptions);

			Task.Factory.StartNew(MessageProcessingTask, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_CloseCancellationToken.Dispose();
				_OptionsReloadToken.Dispose();
			}

			base.Dispose(disposing);
		}

		/// <inheritdoc/>
		protected override void OnLoad(EventArgs e)
		{
			_ConfigureAction?.Invoke(this);

			base.OnLoad(e);
		}

		/// <inheritdoc/>
		protected override void OnClosing(CancelEventArgs e)
		{
			_CloseCancellationToken.Cancel();

			base.OnClosing(e);
		}

		/// <inheritdoc/>
		protected override void OnResize(EventArgs e)
		{
			if (WindowState == FormWindowState.Minimized && NotifyIcon != null)
			{
				Hide();
				NotifyIcon.Visible = true;
			}

			base.OnResize(e);
		}

		private void OnHideAllTabs()
		{
			for (int i = 3; i < WindowMenu.DropDownItems.Count; i++)
			{
				((ToolStripMenuItem)WindowMenu.DropDownItems[i]).Checked = false;
			}
		}

		private void OnShowAllTabs()
		{
			for (int i = 3; i < WindowMenu.DropDownItems.Count; i++)
			{
				((ToolStripMenuItem)WindowMenu.DropDownItems[i]).Checked = true;
			}
		}

		private void OnTabWindowMenuItemCheckedChanged(object? sender, EventArgs e)
		{
			if (sender is ToolStripMenuItem TabWindowMenuItem)
			{
				DebugWindowTabPage Tab = (DebugWindowTabPage)TabWindowMenuItem.Tag;

				if (TabWindowMenuItem.Checked)
					TabContainer.TabPages.Add(Tab);
				else
					TabContainer.TabPages.Remove(Tab);
			}
		}

		private void OnNotifyIconMouseDoubleClick(object sender, MouseEventArgs? e)
		{
			Show();
			WindowState = FormWindowState.Normal;
			NotifyIcon.Visible = false;
		}

		private void ApplyOptions(DebugWindowLoggerOptions options)
		{
			if (InvokeRequired)
			{
				BeginInvoke((Action)(() => ApplyOptions(options)));
				return;
			}

			Text = !string.IsNullOrEmpty(options.WindowTitle)
				? options.WindowTitle
				: _HostEnvironment.ApplicationName;

			if (NotifyIcon != null)
				NotifyIcon.Text = Text;

			Size = new Size(options.WindowWidth, options.WindowHeight);

			if (options.StartMinimized)
				WindowState = FormWindowState.Minimized;

			LoggerGroupCache GroupCache = new LoggerGroupCache(options.GroupOptions);

			_LoggerGroupCache = GroupCache;
		}

		private async Task MessageProcessingTask()
		{
			try
			{
				ChannelReader<LoggerJsonMessage> MessageReader = _MessageManager.Messages.Reader;

				while (await MessageReader.WaitToReadAsync(_CloseCancellationToken.Token).ConfigureAwait(false))
				{
					while (MessageReader.TryRead(out LoggerJsonMessage Message))
					{
						if (_CloseCancellationToken.IsCancellationRequested)
							return;

						string TabTitle = Message.GroupName ?? _LoggerGroupCache.ResolveGroupNameForCategoryName(Message.CategoryName);

						if (!_Tabs.TryGetValue(TabTitle, out DebugWindowTabPage Tab))
						{
							lock (_Tabs)
							{
								if (!_Tabs.TryGetValue(TabTitle, out Tab))
								{
									Tab = new DebugWindowTabPage(this, TabTitle);
									_Tabs.Add(TabTitle, Tab);

									Invoke(new MethodInvoker(() =>
									{
										ToolStripMenuItem TabWindowMenuItem = new ToolStripMenuItem(Tab.Text)
										{
											Checked = true,
											CheckOnClick = true,
											Tag = Tab
										};
										TabWindowMenuItem.CheckedChanged += OnTabWindowMenuItemCheckedChanged;
										WindowMenu.DropDownItems.Add(TabWindowMenuItem);

										TabContainer.TabPages.Add(Tab);
										_ConfigureTabAction?.Invoke(Tab);
									}));
								}
							}
						}

						Tab.AddMessage(SerializeMessageToJson(Message));
					}
				}
			}
			catch (OperationCanceledException)
			{
			}
		}

		private string SerializeMessageToJson(LoggerJsonMessage message)
		{
			try
			{
				return JsonSerializer.Serialize(message, Options.JsonOptions);
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception JsonException)
#pragma warning restore CA1031 // Do not catch general exception types
			{
				return $"Message from Category [{message.CategoryName}] with Content [{message.Content}] could not be serialized into Json.{Environment.NewLine}{JsonException}";
			}
		}
	}
}
