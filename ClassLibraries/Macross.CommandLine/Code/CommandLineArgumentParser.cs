using System;
using System.Diagnostics;
using System.Linq;

namespace Macross.CommandLine
{
	/// <summary>
	/// Parses command-line arguments into <see cref="CommandLineArguments"/> instances.
	/// </summary>
	public static class CommandLineArgumentParser
	{
		/// <summary>
		/// Parse a set of string arguments specified on the command-line into a <see cref="CommandLineArguments"/> instance.
		/// </summary>
		/// <param name="arguments">Command-line arguments.</param>
		/// <returns><see cref="CommandLineArguments"/> instance.</returns>
		public static CommandLineArguments Parse(string[] arguments)
		{
			CommandLineArguments Response = new CommandLineArguments();

			if (arguments?.Any() != true)
				return Response;

			for (int i = 0; i < arguments.Length; i++)
			{
				string Argument = arguments[i];

				if (!Argument.StartsWith("-", StringComparison.OrdinalIgnoreCase))
				{
					if (string.IsNullOrEmpty(Response.Command))
						Response.Command = Argument;
					else
						Response.Parameters.Add(arguments[i]);
					continue;
				}

#if NETSTANDARD2_0
				Argument = Argument.Substring(1);
				if (Argument.StartsWith("-", StringComparison.OrdinalIgnoreCase)) // Double-dash case (--ArgName)
					Argument = Argument.Substring(1);
#else
				Argument = Argument[1..];
				if (Argument.StartsWith("-", StringComparison.OrdinalIgnoreCase)) // Double-dash case (--ArgName)
					Argument = Argument[1..];
#endif

				string? ArgumentValue;
#if NETSTANDARD2_0
				if (Argument.IndexOf("=", StringComparison.OrdinalIgnoreCase) >= 0) // ArgName=ArgValue case
#else
				if (Argument.Contains("=", StringComparison.OrdinalIgnoreCase)) // ArgName=ArgValue case
#endif
				{
					string[] ArgumentComponents = Argument.Split('=');
					Argument = ArgumentComponents[0].Trim();
					ArgumentValue = ArgumentComponents[1].Trim();

					// Macross TrimBookendings extension mirrored here to avoid an entire project dependency for a few lines of code.
#if NETSTANDARD2_0
					if (ArgumentValue.Length >= 2 && ArgumentValue[0] == '\"' && ArgumentValue[ArgumentValue.Length - 1] == '\"')
						ArgumentValue = ArgumentValue.Substring(1, ArgumentValue.Length - 2);
#else
					if (ArgumentValue.Length >= 2 && ArgumentValue[0] == '\"' && ArgumentValue[^1] == '\"')
						ArgumentValue = ArgumentValue[1..^1];
#endif
				}
				else if (i + 1 >= arguments.Length)
				{
					ArgumentValue = null;
				}
				else
				{
					ArgumentValue = arguments[i + 1];
					if (ArgumentValue.StartsWith("-", StringComparison.OrdinalIgnoreCase))
						ArgumentValue = null;
					else
						i++;
				}

				if (Argument.Equals("debug", StringComparison.OrdinalIgnoreCase) && !Debugger.IsAttached)
				{
					Console.WriteLine("Launching debugger...");
					Debugger.Launch();
					continue;
				}

				if (ArgumentValue == null)
					Response.Switches.Add(Argument);
				else
					Response.Options.Add(Argument, ArgumentValue);
			}

			return Response;
		}
	}
}
