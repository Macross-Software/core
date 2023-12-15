using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.CommandLine.Tests
{
	[TestClass]
	public class CommandLineTests
	{
		[TestMethod]
		public void CommandParsedTest()
		{
			CommandLineArguments Expected = new CommandLineArguments
			{
				Command = "push"
			};
			CommandLineArguments Actual = CommandLineArgumentParser.Parse(ParseCommandLine("push"));

			Assert.AreEqual(Expected.Command, Actual.Command);
		}

		[TestMethod]
		public void CommandAndParametersParsedTest()
		{
			CommandLineArguments Expected = new CommandLineArguments
			{
				Command = "push"
			};
			Expected.Parameters.Add("param1");
			Expected.Parameters.Add("param2");

			CommandLineArguments Actual = CommandLineArgumentParser.Parse(ParseCommandLine("push param1 param2"));

			Assert.AreEqual(Expected.Command, Actual.Command);
			Assert.IsTrue(Expected.Parameters.SequenceEqual(Actual.Parameters));
		}

		[TestMethod]
		public void OptionsAndSwitchesParsedTest()
		{
			CommandLineArguments Expected = new CommandLineArguments();
			Expected.Options.Add("option1", "value1");
			Expected.Options.Add("option2", "value with spaces");
			Expected.Options.Add("option3", "value3");
			Expected.Switches.Add("switch1");
			Expected.Switches.Add("switch2");

			CommandLineArguments Actual = CommandLineArgumentParser.Parse(ParseCommandLine("-switch1 -option1 value1 --switch2 --option2=\"value with spaces\" -option3=value3"));

			Assert.AreEqual(Expected.Command, Actual.Command);
			Assert.IsTrue(Expected.Options.SequenceEqual(Actual.Options));
			Assert.IsTrue(Expected.Switches.SequenceEqual(Actual.Switches));
		}

		/// <remarks>
		/// Adapted from code posted by <a href="https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990">Daniel Earwicker </a>.
		/// </remarks>
		private static string[] ParseCommandLine(string commandLine)
		{
			bool inQuotes = false;
			bool isEscaping = false;

			return commandLine
				.Split(c =>
				{
					if (c == '\\' && !isEscaping)
					{
						isEscaping = true;
						return false;
					}

					if (c == '\"' && !isEscaping)
						inQuotes = !inQuotes;

					isEscaping = false;

					return !inQuotes && char.IsWhiteSpace(c);
				})
				.Select(arg => arg.Trim().TrimBookendings('\"').Replace("\\\"", "\"", StringComparison.OrdinalIgnoreCase))
				.Where(arg => !string.IsNullOrEmpty(arg))
				.ToArray();
		}
	}
}
