using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Macross.Logging.Files.Tests
{
	[TestClass]
	public class LogFileManagementScheduleTests
	{
		[TestMethod]
		public void NextArchiveCalculationTimeLocalModeTest()
		{
			TimeSpan TimeUntilNextArchive = LogFileManagementSchedule.Build(
				new TestSystemTime(2020, 1, 25, 1, 0, 0, DateTimeKind.Local),
				new FileLoggerOptions()).TimeUntilNextArchiveUtc;

			Assert.AreEqual(TimeSpan.Zero, TimeUntilNextArchive);

			TimeUntilNextArchive = LogFileManagementSchedule.Build(
				new TestSystemTime(2020, 1, 25, 2, 0, 0, DateTimeKind.Local),
				new FileLoggerOptions()).TimeUntilNextArchiveUtc;

			Assert.AreEqual(TimeSpan.FromHours(23), TimeUntilNextArchive);

			TimeUntilNextArchive = LogFileManagementSchedule.Build(
				new TestSystemTime(2020, 1, 25, 23, 0, 0, DateTimeKind.Local),
				new FileLoggerOptions()).TimeUntilNextArchiveUtc;

			Assert.AreEqual(TimeSpan.FromHours(2), TimeUntilNextArchive);

			TimeUntilNextArchive = LogFileManagementSchedule.Build(
				new TestSystemTime(2020, 1, 25, 1, 0, 0, DateTimeKind.Local),
				new FileLoggerOptions
				{
					LogFileArchiveTime = new TimeSpan(4, 0, 0)
				}).TimeUntilNextArchiveUtc;

			Assert.AreEqual(TimeSpan.FromHours(3), TimeUntilNextArchive);
		}

		[TestMethod]
		public void NextArchiveCalculationTimeLocalModeTimeChangeTest()
		{
			if (TimeZoneInfo.Local.StandardName != "Pacific Standard Time")
				return;

			/*
				Pacific time zone 2019 time change events:
				Sun, Mar 10, 2019 at 2:00 am PST -> PDT (at 2am clock set to 3am, 2am - 3am ghosted)
				Sun, Nov 3, 2019 at 2:00 am PDT -> PST (at 2am clock to set to 1am, 1am - 2am repeated)
			*/

			TestSystemTime SystemTime = new TestSystemTime(2019, 3, 10, 1, 0, 0, DateTimeKind.Local);

			TimeSpan TimeUntilNextArchive = LogFileManagementSchedule.Build(
				SystemTime,
				new FileLoggerOptions
				{
					LogFileArchiveTime = new TimeSpan(3, 30, 0)
				}).TimeUntilNextArchiveUtc;

			Assert.AreEqual(TimeSpan.FromHours(1.5), TimeUntilNextArchive); // Every other day of the year this will calculate as 2.5 hours!

			SystemTime = new TestSystemTime(2019, 11, 3, 0, 0, 0, DateTimeKind.Local);

			TimeUntilNextArchive = LogFileManagementSchedule.Build(
				SystemTime,
				new FileLoggerOptions
				{
					LogFileArchiveTime = new TimeSpan(3, 0, 0)
				}).TimeUntilNextArchiveUtc;

			Assert.AreEqual(TimeSpan.FromHours(4), TimeUntilNextArchive); // Every other day of the year this will calculate as 3 hours!
		}

		[TestMethod]
		public void NextArchiveCalculationTimeUtcModeTest()
		{
			FileLoggerOptions Options = new FileLoggerOptions
			{
				CutoverAndArchiveTimeZoneMode = DateTimeKind.Utc
			};

			TimeSpan TimeUntilNextArchive = LogFileManagementSchedule.Build(
				new TestSystemTime(2020, 1, 25, 1, 0, 0, DateTimeKind.Utc),
				Options).TimeUntilNextArchiveUtc;

			Assert.AreEqual(TimeSpan.Zero, TimeUntilNextArchive);

			TimeUntilNextArchive = LogFileManagementSchedule.Build(
				new TestSystemTime(2020, 1, 25, 2, 0, 0, DateTimeKind.Utc),
				Options).TimeUntilNextArchiveUtc;

			Assert.AreEqual(TimeSpan.FromHours(23), TimeUntilNextArchive);

			TimeUntilNextArchive = LogFileManagementSchedule.Build(
				new TestSystemTime(2020, 1, 25, 23, 0, 0, DateTimeKind.Utc),
				Options).TimeUntilNextArchiveUtc;

			Assert.AreEqual(TimeSpan.FromHours(2), TimeUntilNextArchive);

			TimeUntilNextArchive = LogFileManagementSchedule.Build(
				new TestSystemTime(2020, 1, 25, 1, 0, 0, DateTimeKind.Utc),
				new FileLoggerOptions
				{
					CutoverAndArchiveTimeZoneMode = DateTimeKind.Utc,
					LogFileArchiveTime = new TimeSpan(4, 0, 0)
				}).TimeUntilNextArchiveUtc;

			Assert.AreEqual(TimeSpan.FromHours(3), TimeUntilNextArchive);
		}
	}
}
