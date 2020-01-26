using System;

namespace Macross.Logging.Files
{
	internal class LogFileManagementSchedule
	{
		public DateTime NextArchiveTimeUtc { get; }

		public TimeSpan TimeUntilNextArchiveUtc { get; }

		public DateTime NextCutoverTimeUtc { get; }

		public LogFileManagementSchedule(DateTime nextArchiveTimeUtc, TimeSpan timeUntilNextArchiveUtc, DateTime nextCutoverTimeUtc)
		{
			NextArchiveTimeUtc = nextArchiveTimeUtc;
			TimeUntilNextArchiveUtc = timeUntilNextArchiveUtc;
			NextCutoverTimeUtc = nextCutoverTimeUtc;
		}

		public static LogFileManagementSchedule Build(ISystemTime systemTime, FileLoggerOptions options)
		{
			DateTime NowUtc = systemTime.UtcNow;
			DateTime NowLocal = systemTime.Now;

			(DateTime NextCutoverUtc, _) = CalculateSchedule(
				NowUtc,
				NowLocal,
				options.CutoverAndArchiveTimeZoneMode,
				options.LogFileCutoverTime,
				FileLoggerOptions.DefaultLogFileCutoverTime);

			(DateTime NextArchiveUtc, TimeSpan TimeUntilNextArchiveUtc) = CalculateSchedule(
				NowUtc,
				NowLocal,
				options.CutoverAndArchiveTimeZoneMode,
				options.LogFileArchiveTime,
				FileLoggerOptions.DefaultLogFileArchiveTime);

			return new LogFileManagementSchedule(NextArchiveUtc, TimeUntilNextArchiveUtc, NextCutoverUtc);
		}

		private static (DateTime NextUtc, TimeSpan TimeUntilNextUtc) CalculateSchedule(
			DateTime nowUtc,
			DateTime nowLocal,
			DateTimeKind mode,
			TimeSpan? optionTime,
			TimeSpan defaultTime)
		{
			return mode == DateTimeKind.Utc
				? CalculateScheduleUtc(nowUtc, optionTime, defaultTime)
				: CalculateScheduleLocal(nowUtc, nowLocal, optionTime, defaultTime);
		}

		private static (DateTime NextUtc, TimeSpan TimeUntilNextUtc) CalculateScheduleUtc(
			DateTime nowUtc,
			TimeSpan? optionTime,
			TimeSpan defaultTime)
		{
			DateTime NextUtc = nowUtc.Date + (optionTime ?? defaultTime);
			if (NextUtc < nowUtc)
				NextUtc = NextUtc.AddDays(1);
			TimeSpan TimeUntilNextUtc = NextUtc - nowUtc;
			return (
				NextUtc,
				TimeUntilNextUtc <= TimeSpan.Zero
					? TimeSpan.Zero
					: TimeUntilNextUtc);
		}

		private static (DateTime NextUtc, TimeSpan TimeUntilNextUtc) CalculateScheduleLocal(
			DateTime nowUtc,
			DateTime nowLocal,
			TimeSpan? optionTime,
			TimeSpan defaultTime)
		{
			DateTime NextTimeLocal = nowLocal.Date + (optionTime ?? defaultTime);
			if (NextTimeLocal < nowLocal)
				NextTimeLocal = NextTimeLocal.AddDays(1);
			DateTime NextUtc = NextTimeLocal.ToUniversalTime();
			TimeSpan TimeUntilNextUtc = NextUtc - nowUtc;
			return (
				NextUtc,
				TimeUntilNextUtc <= TimeSpan.Zero
					? TimeSpan.Zero
					: TimeUntilNextUtc);
		}
	}
}
