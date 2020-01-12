namespace System
{
	/// <summary>
	/// Stores the offset of a region of data to be masked when written to log files and traces.
	/// </summary>
	public class MaskedRegion
	{
		/// <summary>
		/// Gets the starting offset (inclusive) of the <see cref="MaskedRegion"/>.
		/// </summary>
		public int StartOffsetInclusive { get; }

		/// <summary>
		/// Gets the ending offset (exclusive) of the <see cref="MaskedRegion"/>.
		/// </summary>
		public int EndOffsetExclusive { get; }

		/// <summary>
		/// Gets the length of the <see cref="MaskedRegion"/>.
		/// </summary>
		public int Length => EndOffsetExclusive - StartOffsetInclusive;

		/// <summary>
		/// Initializes a new instance of the <see cref="MaskedRegion"/> class.
		/// </summary>
		/// <param name="offset">The starting zero-based index of the masked region.</param>
		/// <param name="count">The number of characters to mask from the starting offset.</param>
		public MaskedRegion(int offset, int count)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			StartOffsetInclusive = offset;
			EndOffsetExclusive = offset + count;
		}
	}
}