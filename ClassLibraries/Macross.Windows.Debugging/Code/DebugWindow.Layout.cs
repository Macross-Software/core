using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace Macross.Windows.Debugging
{
	public partial class DebugWindow
	{
		private static ToolStripMenuItem CreateMenuItem(string text, Keys? shortcutKeys, EventHandler clientHandler)
		{
			ToolStripMenuItem MenuItem = new ToolStripMenuItem(text);

			if (shortcutKeys.HasValue)
				MenuItem.ShortcutKeys = shortcutKeys.Value;

			MenuItem.Click += clientHandler;

			return MenuItem;
		}

		/// <summary>
		/// Gets the main <see cref="MenuStrip"/> for the window.
		/// </summary>
		public MenuStrip MainMenu { get; } = new MenuStrip();

		/// <summary>
		/// Gets the main <see cref="ToolStripMenuItem"/> for the Window sub-menu.
		/// </summary>
		public ToolStripMenuItem WindowMenu { get; } = new ToolStripMenuItem("&Window");

		/// <summary>
		/// Gets the <see cref="TabControl"/> hosting the <see cref="DebugWindowTabPage"/> controls for presenting log messages by category.
		/// </summary>
		public TabControl TabContainer { get; } = new TabControl
		{
			Dock = DockStyle.Fill
		};

		/// <summary>
		/// Gets the system tray <see cref="NotifyIcon"/> used when <see cref="DebugWindowLoggerOptions.MinimizeToSystemTray"/> is used.
		/// </summary>
		public NotifyIcon? NotifyIcon { get; private set; }

		// Once there is a proper WinForm designer for .Net Core this can be removed in favor of that.
		private void InitializeControls()
		{
			Padding = new Padding(5);

			InitializeMainMenu();

			Controls.Add(TabContainer);
			Controls.Add(MainMenu);

			if (_Options.CurrentValue.MinimizeToSystemTray)
			{
				NotifyIcon = new NotifyIcon
				{
					Icon = Icon
				};
				NotifyIcon.ContextMenuStrip = InitializeNotifyIconMenu();
				NotifyIcon.MouseDoubleClick += OnNotifyIconMouseDoubleClick;
			}
		}

		private void InitializeMainMenu()
		{
			ToolStripMenuItem FileMenu = new ToolStripMenuItem("&File");

			FileMenu.DropDownItems.AddRange(
				new ToolStripItem[]
				{
					CreateMenuItem("E&xit", Keys.Alt | Keys.F4, (sender, eventArgs) => Close())
				});

			ToolStripMenuItem ToolsMenu = new ToolStripMenuItem("&Tools");

			ToolsMenu.DropDownItems.AddRange(
				new ToolStripItem[]
				{
					CreateMenuItem("&Debug", Keys.Control | Keys.Alt | Keys.P, (sender, eventArgs) => Debugger.Launch())
				});

			WindowMenu.DropDownItems.AddRange(
				new ToolStripItem[]
				{
					CreateMenuItem("&Hide All", Keys.Alt | Keys.H, (sender, eventArgs) => OnHideAllTabs()),
					CreateMenuItem("&Show All", Keys.Alt | Keys.S, (sender, eventArgs) => OnShowAllTabs()),
					new ToolStripSeparator()
				});

			MainMenu.Items.Add(FileMenu);
			MainMenu.Items.Add(ToolsMenu);
			MainMenu.Items.Add(WindowMenu);
		}

		private ContextMenuStrip InitializeNotifyIconMenu()
		{
			ContextMenuStrip Menu = new ContextMenuStrip();

			Menu.Items.Add("Show").Click += (sender, eventArgs) => OnNotifyIconMouseDoubleClick(sender, null);
			Menu.Items.Add("Exit").Click += (sender, eventArgs) =>
			{
				if (NotifyIcon != null)
					NotifyIcon.Visible = false;
				Close();
			};

			return Menu;
		}
	}
}
