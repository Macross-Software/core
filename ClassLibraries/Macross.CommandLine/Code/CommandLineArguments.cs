using System;
using System.Collections.Generic;
#if !NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
#endif

namespace Macross.CommandLine
{
	/// <summary>
	/// Class for storing arguments passed on a command-line interface.
	/// </summary>
	public class CommandLineArguments
	{
		/// <summary>
		/// Gets the static Empty reference.
		/// </summary>
		public static CommandLineArguments Empty { get; } = new CommandLineArguments();

		/// <summary>
		/// Gets or sets the command that was specified or null if no command was specified.
		/// </summary>
		public string? Command { get; set; }

		/// <summary>
		/// Gets the parameters that were specified.
		/// </summary>
		public ICollection<string> Parameters { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Gets the options that were specified.
		/// </summary>
		public IDictionary<string, string> Options { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Gets the switches that were specified.
		/// </summary>
		public ICollection<string> Switches { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Tests whether a switch was specified on the command-line.
		/// </summary>
		/// <param name="switchName">Switch name.</param>
		/// <returns>Whether or not the switch was specified.</returns>
		public bool HasSwitch(string switchName)
		{
			if (string.IsNullOrEmpty(switchName))
				throw new ArgumentNullException(nameof(switchName));

			return Switches.Contains(switchName);
		}

		/// <summary>
		/// Attempts to retrieve the value specified as an option on the command-line.
		/// </summary>
		/// <param name="optionName">Option name.</param>
		/// <param name="optionValue">The value that was specified, if found.</param>
		/// <returns>Whether or not the value was found.</returns>
#if NETSTANDARD2_0
		public bool TryGetOption(string optionName, out string optionValue)
#else
		public bool TryGetOption(string optionName, [MaybeNullWhen(returnValue: false)] out string optionValue)
#endif
		{
			if (string.IsNullOrEmpty(optionName))
				throw new ArgumentNullException(nameof(optionName));

			return Options.TryGetValue(optionName, out optionValue);
		}

		/// <summary>
		/// Gets the specified value for a given option or returns a default value if it wasn't specified.
		/// </summary>
		/// <param name="optionName">Option name.</param>
		/// <param name="defaultOptionValue">Default value to be returned if the option was not specified.</param>
		/// <returns>The value that was specified for the given option or the default value if option was not specified.</returns>
		public string? GetOptionOrDefaultValue(string optionName, string? defaultOptionValue)
			=> TryGetOption(optionName, out string? OptionValue) ? OptionValue : defaultOptionValue;

		/// <summary>
		/// Gets the specified value for a given option.
		/// </summary>
		/// <param name="optionName">Option name.</param>
		/// <exception cref="KeyNotFoundException">The specified option was not found.</exception>
		/// <returns>The value that was specified for the given option.</returns>
		public string GetOption(string optionName)
		{
			if (!TryGetOption(optionName, out string? OptionValue))
				throw new KeyNotFoundException($"{optionName} was not specified. See help for more details.");
			return OptionValue;
		}
	}
}
