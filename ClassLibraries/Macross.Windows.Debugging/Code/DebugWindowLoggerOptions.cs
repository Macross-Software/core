using System.Text.Json;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Collections.Generic;

using Microsoft.Extensions.Hosting;

using Macross.Logging;

namespace Macross.Windows.Debugging
{
	/// <summary>
	/// Stores options for the <see cref="DebugWindow"/>.
	/// </summary>
	public class DebugWindowLoggerOptions
	{
		/// <summary>
		/// Gets the default <see cref="LoggerGroupOptions"/> filters used to group log messages by category.
		/// </summary>
		/// <remarks>
		/// Default settings are constructed as:
		/// <code><![CDATA[
		///   new LoggerGroupOptions[]
		///   {
		///   	new LoggerGroupOptions
		///   	{
		///   		GroupName = "System",
		///   		CategoryNameFilters = new string[] { "System*" }
		///   	},
		///   	new LoggerGroupOptions
		///   	{
		///   		GroupName = "Microsoft",
		///   		CategoryNameFilters = new string[] { "Microsoft*" }
		///   	},
		///   };
		/// ]]></code>
		/// </remarks>
		public static IEnumerable<LoggerGroupOptions> DefaultGroupOptions { get; } = new LoggerGroupOptions[]
		{
			new LoggerGroupOptions
			{
				GroupName = "System",
				CategoryNameFilters = new string[] { "System*" }
			},
			new LoggerGroupOptions
			{
				GroupName = "Microsoft",
				CategoryNameFilters = new string[] { "Microsoft*" }
			},
		};

		/// <summary>
		/// Gets the default <see cref="JsonSerializerOptions"/> options to use when displaying messages.</summary>
		/// <remarks>
		/// Default settings are constructed as:
		/// <code><![CDATA[
		///   new JsonSerializerOptions
		///   {
		///       IgnoreNullValues = true,
		///       WriteIndented = true,
		///       Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		///   };
		/// ]]></code>
		/// </remarks>
		public static JsonSerializerOptions DefaultJsonOptions { get; } = new JsonSerializerOptions
		{
			IgnoreNullValues = true,
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};

		/// <summary>
		/// Gets or sets a value indicating whether or not the debugger should be launched on startup if it isn't already attached. Default value: False.
		/// </summary>
		/// <remarks>
		/// This is primarily for starting applications in debug via the command-line.
		/// </remarks>
		public bool LaunchDebuggerIfNotAttached { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the <see cref="DebugWindow"/> should be shown. Default value: Show if the debugger is attached.
		/// </summary>
		/// <remarks>
		/// This is primarily for starting applications with the debug interface via the command-line.
		/// </remarks>
		public bool ShowDebugWindow { get; set; } = Debugger.IsAttached;

		/// <summary>
		/// Gets or sets a value indicating whether or not any parent console window will be hidden when showing the <see cref="DebugWindow"/>. Default value: True.
		/// </summary>
		public bool HideConsoleIfAttachedWhenShowingWindow { get; set; } = true;

		/// <summary>
		/// Gets or sets the title string that should be displayed in the <see cref="DebugWindow"/>. If not supplied the <see cref="IHostEnvironment.ApplicationName"/> value will be used.
		/// </summary>
		public string? WindowTitle { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="DebugWindow"/> width in pixels. Default value: 1024.
		/// </summary>
		public int WindowWidth { get; set; } = 1024;

		/// <summary>
		/// Gets or sets the <see cref="DebugWindow"/> height in pixels. Default value: 768.
		/// </summary>
		public int WindowHeight { get; set; } = 768;

		/// <summary>
		/// Gets or sets a value indicating whether or not the <see cref="DebugWindow"/> should start minimized. Default value: False.
		/// </summary>
		public bool StartMinimized { get; set; } = false;

		/// <summary>
		/// Gets or sets a value indicating whether or not the <see cref="DebugWindow"/> should minimize to the system tray. Default value: False.
		/// </summary>
		public bool MinimizeToSystemTray { get; set; } = false;

		/// <summary>
		/// Gets or sets the filters to use to group log messages by category.
		/// </summary>
		/// <remarks>
		/// See <see cref="DefaultGroupOptions"/> for default values.
		/// </remarks>
		public IEnumerable<LoggerGroupOptions>? GroupOptions { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="JsonSerializerOptions"/> to use when displaying messages.
		/// </summary>
		/// <remarks>
		/// See <see cref="DefaultJsonOptions"/> for default values.
		/// </remarks>
		public JsonSerializerOptions? JsonOptions { get; set; }
	}
}
