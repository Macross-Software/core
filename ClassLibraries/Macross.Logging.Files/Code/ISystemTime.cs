namespace System
{
	/// <summary>
	/// ISystemTime interface is a slim wrapper over the System interface so that unit tests don't need to access the actual actual system clock.
	/// </summary>
	internal interface ISystemTime
	{
		DateTime Now { get; }

		DateTime UtcNow { get; }
	}
}
