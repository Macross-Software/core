namespace System
{
	public class TestSystemTime : ISystemTime
	{
		public DateTime Now => UtcNow.ToLocalTime();

		public DateTime UtcNow { get; set; }

		public TestSystemTime(
			int year,
			int month,
			int day,
			int hour = 0,
			int minute = 0,
			int second = 0,
			DateTimeKind kind = DateTimeKind.Utc)
		{
			UtcNow = kind == DateTimeKind.Local
				? DateTime.SpecifyKind(new DateTime(year, month, day, hour, minute, second), DateTimeKind.Local).ToUniversalTime()
				: DateTime.SpecifyKind(new DateTime(year, month, day, hour, minute, second), DateTimeKind.Utc);
		}
	}
}