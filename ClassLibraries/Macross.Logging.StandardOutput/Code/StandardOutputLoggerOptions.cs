using System.Text.Json;
using System.Text.Encodings.Web;
using System.Collections.Generic;

namespace Macross.Logging.StandardOutput
{
	/// <summary>
	/// Stores options for the <see cref="StandardOutputLogger"/>.
	/// </summary>
	public class StandardOutputLoggerOptions
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
		/// Gets or sets the filters to use to group log messages by category.
		/// </summary>
		/// <remarks>
		/// See <see cref="DefaultGroupOptions"/> for default values.
		/// </remarks>
		public IEnumerable<LoggerGroupOptions>? GroupOptions { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="JsonSerializerOptions"/> to use when serializing messages.
		/// </summary>
		/// <remarks>
		/// See <see cref="DefaultJsonOptions"/> for default values.
		/// </remarks>
		public JsonSerializerOptions? JsonOptions { get; set; }
	}
}
