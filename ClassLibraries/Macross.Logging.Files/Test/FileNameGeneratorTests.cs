using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Logging.Files.Tests
{
	[TestClass]
	public class FileNameGeneratorTests
	{
		private static readonly TestSystemTime s_Jan19Utc = new TestSystemTime(2020, 1, 19);
		private static readonly TestSystemTime s_Nov7Local = new TestSystemTime(2020, 11, 7, kind: DateTimeKind.Local);

		[TestMethod]
		public void DefaultLogFileNameTest()
		{
			string Expected = $"{Environment.MachineName}.20201107.log";
			string Actual = FileNameGenerator.GenerateFileName("AppName", s_Nov7Local, "Group", FileLoggerOptions.DefaultLogFileNamePattern);

			Assert.AreEqual(Expected, Actual);
		}

		[TestMethod]
		public void DefaultGroupLogFileNameTest()
		{
			string Expected = $"{Environment.MachineName}.Group.20201107.log";
			string Actual = FileNameGenerator.GenerateFileName("AppName", s_Nov7Local, "Group", FileLoggerOptions.DefaultGroupLogFileNamePattern);

			Assert.AreEqual(Expected, Actual);
		}

		[TestMethod]
		public void ApplicationNameTokenLogFileNameTest()
		{
			string Expected = $"AppName.log";
			string Actual = FileNameGenerator.GenerateFileName("AppName", s_Jan19Utc, "Group", "{ApplicationName}.log");

			Assert.AreEqual(Expected, Actual);
		}

		[TestMethod]
		public void LocalTimeLogFileNameTest()
		{
			string Expected = $"{Environment.MachineName}.20201107.log";
			string Actual = FileNameGenerator.GenerateFileName("AppName", s_Nov7Local, "Group", "{MachineName}.{DateTime:yyyyMMdd}.log");

			Assert.AreEqual(Expected, Actual);
		}

		[TestMethod]
		public void UtcTimeLogFileNameTest()
		{
			string Expected = $"{Environment.MachineName}.20200119.log";
			string Actual = FileNameGenerator.GenerateFileName("AppName", s_Jan19Utc, "Group", "{MachineName}.{DateTimeUtc:yyyyMMdd}.log");

			Assert.AreEqual(Expected, Actual);
		}

		[TestMethod]
		public void DateTimeNoFormatLogFileNameTest()
		{
			string Expected = $"{Environment.MachineName}.20201107.log";
			string Actual = FileNameGenerator.GenerateFileName("AppName", s_Nov7Local, "Group", "{MachineName}.{DateTime}.log");

			Assert.AreEqual(Expected, Actual);
		}

		[TestMethod]
		public void DateTimeUtcNoFormatLogFileNameTest()
		{
			string Expected = $"{Environment.MachineName}.20200119.log";
			string Actual = FileNameGenerator.GenerateFileName("AppName", s_Jan19Utc, "Group", "{MachineName}.{DateTimeUtc}.log");

			Assert.AreEqual(Expected, Actual);
		}

		[TestMethod]
		public void DateTimeCustomFormatLogFileNameTest()
		{
			string Expected = $"{Environment.MachineName}.20.11.7.log";
			string Actual = FileNameGenerator.GenerateFileName("AppName", s_Nov7Local, "Group", "{MachineName}.{DateTime:yy.M.d}.log");

			Assert.AreEqual(Expected, Actual);
		}

		[TestMethod]
		public void UnknownTokenLogFileNameTest()
		{
			string Expected = $"{Environment.MachineName}.{{UnknownToken}}.log";
			string Actual = FileNameGenerator.GenerateFileName("AppName", s_Jan19Utc, "Group", "{MachineName}.{UnknownToken}.log");

			Assert.AreEqual(Expected, Actual);
		}

		[TestMethod]
		public void CaseInsensitiveTokenMatchLogFileNameTest()
		{
			string Expected = $"{Environment.MachineName}.log";
			string Actual = FileNameGenerator.GenerateFileName("AppName", s_Jan19Utc, "Group", "{machinename}.log");

			Assert.AreEqual(Expected, Actual);
		}

		[TestMethod]
		public void WildcardTokenMatchLogFileNameTest()
		{
			string Expected = $"{Environment.MachineName}.AppName.*.*.{{NonMatch}}.*.*log";
			string Actual = FileNameGenerator.GenerateWildcardFileName("AppName", "{MachineName}.{ApplicationName}.{GroupName}.{DateTimeUtc:yyyyMMdd}.{NonMatch}.{DateTime}.log");

			Assert.AreEqual(Expected, Actual);
		}
	}
}
