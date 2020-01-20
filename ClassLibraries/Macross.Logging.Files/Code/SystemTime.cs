namespace System
{
	internal class SystemTime : ISystemTime
	{
		public DateTime Now => DateTime.Now;

		public DateTime UtcNow => DateTime.UtcNow;
	}
}
