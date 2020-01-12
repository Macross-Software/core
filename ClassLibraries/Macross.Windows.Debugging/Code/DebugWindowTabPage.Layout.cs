using System.Drawing;
using System.Windows.Forms;

namespace Macross.Windows.Debugging
{
	public partial class DebugWindowTabPage
	{
		/// <summary>
		/// Gets the <see cref="RichTextBox"/> control presenting the log messages being written.
		/// </summary>
		public RichTextBox LogMessageTextBox { get; } = new RichTextBox
		{
			Dock = DockStyle.Fill,
			ScrollBars = RichTextBoxScrollBars.Both,
			WordWrap = false,
			Multiline = true,
			Font = new Font("Lucida Console", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0)
		};

		/// <summary>
		/// Gets the <see cref="TextBox"/> control used to configure the <see cref="MaxNumberOfDisplayedMessages"/> value.
		/// </summary>
		public TextBox MessagesToShowTextBox { get; } = new TextBox
		{
			Anchor = AnchorStyles.Left,
			Text = "100",
			Margin = new Padding(0),
			Size = new Size(37, 20)
		};

		/// <summary>
		/// Gets the <see cref="CheckBox"/> control used to configure the <see cref="TailEnabled"/> value.
		/// </summary>
		public CheckBox TailCheckBox { get; } = new CheckBox
		{
			Text = "Tail",
			Checked = true,
			AutoSize = true,
			Anchor = AnchorStyles.Left,
			Margin = new Padding(5, 0, 0, 0)
		};

		/// <summary>
		/// Gets the <see cref="CheckBox"/> control used to configure the <see cref="AutoUpdateEnabled"/> value.
		/// </summary>
		public CheckBox AutoUpdateCheckBox { get; } = new CheckBox
		{
			Text = "Auto Update",
			Checked = true,
			AutoSize = true,
			Anchor = AnchorStyles.Left,
			Margin = new Padding(0)
		};

		/// <summary>
		/// Gets the <see cref="Button"/> control used to clear the <see cref="LogMessageTextBox"/> control.
		/// </summary>
		public Button ClearLogButton { get; } = new Button
		{
			Text = "Clear",
			Anchor = AnchorStyles.Right,
			Margin = new Padding(0)
		};

#pragma warning disable CA2000 // Dispose objects before losing scope
		private void InitializeControls()
		{
			Padding = new Padding(3);

			TableLayoutPanel MainTableLayoutPanel = new TableLayoutPanel
			{
				ColumnCount = 1,
				RowCount = 2,
				Dock = DockStyle.Fill
			};

			Controls.Add(MainTableLayoutPanel);

			MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			MainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			MainTableLayoutPanel.RowStyles.Add(new RowStyle());

			MainTableLayoutPanel.Controls.Add(LogMessageTextBox, 0, 0);
			MainTableLayoutPanel.Controls.Add(InitializeDetailControls(), 0, 1);

			MessagesToShowTextBox.KeyPress += OnMessagesToShowTextBoxKeyPress;

			TailCheckBox.CheckedChanged += OnTailCheckBoxCheckedChanged;

			ClearLogButton.Click += OnClearLogButtonClick;

			TurnOnScrollHandlers();
		}

		private Control InitializeDetailControls()
		{
			TableLayoutPanel DetailTableLayoutPanel = new TableLayoutPanel
			{
				AutoSize = true,
				ColumnCount = 4,
				RowCount = 1,
				Dock = DockStyle.Fill,
				Margin = new Padding(0)
			};

			DetailTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			DetailTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			DetailTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			DetailTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			DetailTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			DetailTableLayoutPanel.Controls.Add(InitializeNumberOfLogMessagesControls(), 0, 0);
			DetailTableLayoutPanel.Controls.Add(TailCheckBox, 1, 0);
			DetailTableLayoutPanel.Controls.Add(AutoUpdateCheckBox, 2, 0);
			DetailTableLayoutPanel.Controls.Add(ClearLogButton, 3, 0);

			return DetailTableLayoutPanel;
		}

		private Control InitializeNumberOfLogMessagesControls()
		{
			TableLayoutPanel NumberOfLogMessagesTableLayoutPanel = new TableLayoutPanel
			{
				AutoSize = true,
				ColumnCount = 2,
				RowCount = 1,
				Dock = DockStyle.Fill,
				Margin = new Padding(0)
			};

			NumberOfLogMessagesTableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
			NumberOfLogMessagesTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			NumberOfLogMessagesTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

			NumberOfLogMessagesTableLayoutPanel.Controls.Add(
				new Label
				{
					Text = "Messages to show:",
					AutoSize = true,
					Anchor = AnchorStyles.Left,
					Margin = new Padding(0)
				},
				0,
				0);
			NumberOfLogMessagesTableLayoutPanel.Controls.Add(MessagesToShowTextBox, 1, 0);

			return NumberOfLogMessagesTableLayoutPanel;
		}
#pragma warning restore CA2000 // Dispose objects before losing scope
	}
}
