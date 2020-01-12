using System;

namespace Macross.Windows.Debugging
{
	/// <summary>
	/// An empty scope without any logic.
	/// </summary>
	internal class NullScope : IDisposable
	{
		public static NullScope Instance { get; } = new NullScope();

		private NullScope()
		{
		}

		/// <inheritdoc />
		public void Dispose()
		{
		}
	}
}
