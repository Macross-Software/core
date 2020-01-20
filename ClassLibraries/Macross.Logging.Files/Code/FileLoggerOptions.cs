using System.Text.Json;
using System.Text.Encodings.Web;
using System.Collections.Generic;

using Microsoft.Extensions.Hosting;

namespace Macross.Logging.Files
{
	/// <summary>
	/// Stores options for the <see cref="FileLogger"/>.
	/// </summary>
	public class FileLoggerOptions
	{
		/// <summary>
		/// Gets the default <see cref="JsonSerializerOptions"/> options to use when serializing messages.</summary>
		/// <remarks>
		/// Default settings are constructed as:
		/// <code><![CDATA[
		///   new JsonSerializerOptions
		///   {
		///       IgnoreNullValues = true,
		///       Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		///   };
		/// ]]></code>
		/// </remarks>
		public static JsonSerializerOptions DefaultJsonOptions { get; } = new JsonSerializerOptions
		{
			IgnoreNullValues = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		};

		/// <summary>
		/// Gets the default log file name pattern, used when <see cref="LogFileNamePattern"/> is not specified and <see cref="IncludeGroupNameInFileName"/> is false.
		/// </summary>
		public const string DefaultLogFileNamePattern = "{MachineName}.{DateTimeUtc:yyyyMMdd}.log";

		/// <summary>
		/// Gets the default log file name pattern, used when <see cref="LogFileNamePattern"/> is not specified and <see cref="IncludeGroupNameInFileName"/> is true.
		/// </summary>
		public const string DefaultGroupLogFileNamePattern = "{MachineName}.{GroupName}.{DateTimeUtc:yyyyMMdd}.log";

		/// <summary>
		/// Gets the default log file directory, used when <see cref="LogFileDirectory"/> is not specified.
		/// </summary>
		public const string DefaultLogFileDirectory = "C:\\Logs\\{ApplicationName}\\";

		/// <summary>
		/// Gets the default log file archive directory, used when <see cref="LogFileArchiveDirectory"/> is not specified.
		/// </summary>
		public const string DefaultLogFileArchiveDirectory = "C:\\Logs\\Archive\\{ApplicationName}\\";

		/// <summary>
		/// Gets or sets the application name string that should be used as the {ApplicationName} token in file paths. If not supplied the <see cref="IHostEnvironment.ApplicationName"/> value will be used.
		/// </summary>
		public string? ApplicationName { get; set; }

		/// <summary>
		/// Gets or sets the directory used to store log files.
		/// </summary>
		public string? LogFileDirectory { get; set; }

		/// <summary>
		/// Gets or sets the directory used to store archived log files.
		/// </summary>
		public string? LogFileArchiveDirectory { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not group name should be part of the log file name.
		/// </summary>
		public bool IncludeGroupNameInFileName { get; set; } = false;

		/// <summary>
		/// Gets or sets the maximum file size in kilobytes of log files. Use 0 to indicate no maxium size. Default value: 10 Mb.
		/// </summary>
		public int LogFileMaxSizeInKilobytes { get; set; } = 1024 * 10; // 10 Mb default file size.

		/// <summary>
		/// Gets or sets the log file naming pattern to use.
		/// </summary>
		public string? LogFileNamePattern { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether a disk test should be performed on startup. Default value: true.
		/// </summary>
		public bool TestDiskOnStartup { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether old log files should be archived on startup. Default value: true.
		/// </summary>
		public bool ArchiveLogFilesOnStartup { get; set; } = true;

		/// <summary>
		/// Gets or sets the filters to use to group log messages by category.
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
		public IEnumerable<LoggerGroupOptions>? GroupOptions { get; set; } = new LoggerGroupOptions[]
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
		/// Gets or sets the <see cref="JsonSerializerOptions"/> to use when serializing messages.
		/// </summary>
		/// <remarks>
		/// See <see cref="DefaultJsonOptions"/> for default values.
		/// </remarks>
		public JsonSerializerOptions? JsonOptions { get; set; }
	}
}
