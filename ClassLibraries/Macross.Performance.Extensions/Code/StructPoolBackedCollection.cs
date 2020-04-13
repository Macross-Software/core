using System.Buffers;
using System.Threading;
#if !NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
#endif

namespace System.Collections.Generic
{
	/// <summary>
	/// A struct for storing items backed by a reusable buffer to reduce memory allocations.
	/// </summary>
	/// <typeparam name="T">The type of item in the collection.</typeparam>
	public readonly struct StructPoolBackedCollection<T> : IEnumerable<T>, IEquatable<StructPoolBackedCollection<T>>
	{
		private static readonly ArrayPool<T> s_Pool = ArrayPool<T>.Create(4096, 64);
		private static int s_LastAllocatedSize = 64;

		private readonly T[]? _Buffer;

		/// <summary>
		/// Gets the number of items in the collection.
		/// </summary>
		public int Count { get; }

		/// <summary>
		/// Gets a value indicating whether or not the collection is empty.
		/// </summary>
		public bool IsEmpty => Count == 0;

		/// <summary>
		/// Gets the item stored at <paramref name="index"/> by reference.
		/// </summary>
		/// <param name="index">Zero-based index of the item to retrieve.</param>
		/// <returns>The item retrieved by reference.</returns>
		public ref T this[int index]
		{
			get
			{
				if (_Buffer == null)
					throw new InvalidOperationException("Items cannot be read from an empty pool instance.");
				return ref _Buffer[index];
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StructPoolBackedCollection{T}"/> struct.
		/// </summary>
		/// <param name="initialSize">Specifies the initial size for the collection. If no size is specified the last allocated size is used.</param>
		public StructPoolBackedCollection(int? initialSize = null)
		{
			if (!initialSize.HasValue)
				initialSize = s_LastAllocatedSize;

			if (initialSize < 0)
				throw new ArgumentOutOfRangeException(nameof(initialSize));

			_Buffer = s_Pool.Rent(initialSize.Value);
			Count = 0;
		}

		private StructPoolBackedCollection(T[]? buffer, int count)
		{
			_Buffer = buffer;
			Count = count;
		}

		/// <summary>
		/// Compares two <see cref="StructPoolBackedCollection{T}" /> instances for equality.
		/// </summary>
		/// <param name="left">First <see cref="StructPoolBackedCollection{T}" /> instance.</param>
		/// <param name="right">Second <see cref="StructPoolBackedCollection{T}" /> instance.</param>
		/// <returns><see langword="true" /> if instances are equal.</returns>
		public static bool operator ==(StructPoolBackedCollection<T> left, StructPoolBackedCollection<T> right)
			=> left.Equals(right);

		/// <summary>
		/// Compares two <see cref="StructPoolBackedCollection{T}" /> instances for inequality.
		/// </summary>
		/// <param name="left">First <see cref="StructPoolBackedCollection{T}" /> instance.</param>
		/// <param name="right">Second <see cref="StructPoolBackedCollection{T}" /> instance.</param>
		/// <returns><see langword="true" /> if instances are inequal.</returns>
		public static bool operator !=(StructPoolBackedCollection<T> left, StructPoolBackedCollection<T> right)
			=> !left.Equals(right);

		/// <summary>
		/// Returns a new <see cref="StructPoolBackedCollection{T}" /> instance with the item added.
		/// </summary>
		/// <param name="list">The source list.</param>
		/// <param name="item">The item to add.</param>
		public void Add(ref StructPoolBackedCollection<T> list, T item)
		{
			T[]? buffer = list._Buffer;

			if (buffer == null)
				throw new InvalidOperationException("Items cannot be added to an empty pool instance.");

			int count = list.Count;

			if (count >= buffer.Length)
			{
				Interlocked.CompareExchange(ref s_LastAllocatedSize, buffer.Length * 2, s_LastAllocatedSize);
				T[]? previousBuffer = buffer;

				buffer = s_Pool.Rent(s_LastAllocatedSize);

#if !NETSTANDARD2_1
				Array.Copy(previousBuffer, buffer, previousBuffer.Length);
#else
				Span<T> span = previousBuffer.AsSpan();
				span.CopyTo(buffer);
#endif
				s_Pool.Return(previousBuffer);
			}

			buffer[count] = item;
			list = new StructPoolBackedCollection<T>(buffer, count + 1);
		}

		/// <summary>
		/// Returns a new <see cref="StructPoolBackedCollection{T}" /> instance containing no items.
		/// </summary>
		/// <param name="list">The source list.</param>
		public void Clear(ref StructPoolBackedCollection<T> list)
		{
			T[]? buffer = list._Buffer;
			if (buffer != null)
			{
				list = new StructPoolBackedCollection<T>(buffer, 0);
			}
		}

		/// <summary>
		/// Returns the buffer backing the collection to the reusable pool and invalidates the <see cref="StructPoolBackedCollection{T}" /> instance provided.
		/// </summary>
		/// <param name="list">The source list.</param>
		public void Return(ref StructPoolBackedCollection<T> list)
		{
			T[]? buffer = _Buffer;
			if (buffer != null)
			{
				s_Pool.Return(buffer);
				list = new StructPoolBackedCollection<T>(null, 0);
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
			=> obj is StructPoolBackedCollection<T> other && Equals(other);

		/// <inheritdoc/>
		public bool Equals(StructPoolBackedCollection<T> other)
			=> other.Count == Count && other._Buffer == _Buffer;

		/// <inheritdoc/>
		public override int GetHashCode()
#if !NETSTANDARD2_1
		{
			int result = 1;
			result = (31 * result) + Count;
			result = (31 * result) + (_Buffer?.GetHashCode() ?? 0);
			return result;
		}
#else
			=> HashCode.Combine(Count, _Buffer?.GetHashCode() ?? 0);
#endif

		/// <summary>
		/// Gets an <see cref="Enumerator" /> instance to enumerate over the items in the collection.
		/// </summary>
		/// <returns><see cref="Enumerator" /> instance.</returns>
		public Enumerator GetEnumerator() => new Enumerator(in this);

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(in this);

		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(in this);

		/// <summary>
		/// A struct for enumerating over items in a <see cref="StructPoolBackedCollection{T}" /> instance without allocation.
		/// </summary>
		public struct Enumerator : IEnumerator<T>, IEnumerator
		{
			private readonly T[] _Buffer;
			private readonly int _Count;
			private int _Index;

			/// <summary>
			/// Initializes a new instance of the <see cref="Enumerator"/> struct.
			/// </summary>
			/// <param name="list">The <see cref="StructPoolBackedCollection{T}" /> instance being enumerated.</param>
			public Enumerator(in StructPoolBackedCollection<T> list)
			{
				_Buffer = list._Buffer ?? Array.Empty<T>();
				_Count = list.Count;
				_Index = 0;
				Current = default;
			}

			/// <inheritdoc/>
#if NETSTANDARD2_1
			[AllowNull]
#endif
			public T Current { get; private set; }

			object? IEnumerator.Current => Current;

			/// <inheritdoc/>
			public void Dispose()
			{
			}

			/// <inheritdoc/>
			public bool MoveNext()
			{
				if (_Index < _Count)
				{
					Current = _Buffer[_Index++];
					return true;
				}

				_Index = _Count + 1;
				Current = default;
				return false;
			}

			/// <inheritdoc/>
			public void Reset()
			{
				_Index = 0;
				Current = default;
			}
		}
	}
}
