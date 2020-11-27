using System;
using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Logging.Files.Tests
{
	[TestClass]
	public class FileLoggerOptionsTests
	{
		[TestMethod]
		public void TimeSpanDeserializationTest()
		{
			string json = @"{
				""CutoverAndArchiveTimeZoneMode"": ""Local"",
				""LogFileCutoverTime"": ""01:02:03"",
				""LogFileArchiveTime"": null
			}";

			FileLoggerOptions? Options = JsonSerializer.Deserialize<FileLoggerOptions>(json);

			Assert.IsNotNull(Options);
			Assert.AreEqual(DateTimeKind.Local, Options.CutoverAndArchiveTimeZoneMode);
			Assert.AreEqual(new TimeSpan(1, 2, 3), Options.LogFileCutoverTime);
			Assert.IsFalse(Options.LogFileArchiveTime.HasValue);
		}
	}
}
