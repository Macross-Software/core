using System.Buffers;
using System.Threading;
using System.Runtime.CompilerServices;
#if !NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
#endif

namespace System.Collections.Generic
{
	/// <summary>
	/// A class for storing items backed by a reusable buffer to reduce memory allocations.
	/// </summary>
	/// <typeparam name="T">The type of item in the collection.</typeparam>
	public class PoolBackedCollection<T> : ICollection<T>, IEquatable<PoolBackedCollection<T>>, IDisposable
	{
		private static readonly ArrayPool<T> s_Pool = ArrayPool<T>.Create(8192, 64);
		private static int s_LastAllocatedSize = 64;

		private T[] _Buffer;

		/// <inheritdoc/>
		public int Count { get; private set; }

		/// <summary>
		/// Gets a value indicating whether or not the collection is empty.
		/// </summary>
		public bool IsEmpty => Count == 0;

		/// <inheritdoc/>
		public bool IsReadOnly => false;

		/// <summary>
		/// Gets the item stored at <paramref name="index"/> by reference.
		/// </summary>
		/// <param name="index">Zero-based index of the item to retrieve.</param>
		/// <returns>The item retrieved by reference.</returns>
		public ref T this[int index]
		{
			get
			{
				// Following trick can reduce the range check by one
				if ((uint)index >= (uint)Count)
					throw new ArgumentOutOfRangeException(nameof(index));

				return ref _Buffer[index];
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PoolBackedCollection{T}"/> class.
		/// </summary>
		/// <param name="initialCapacity">Specifies the initial capacity for the collection. If no capacity is specified the last allocated size is used.</param>
		public PoolBackedCollection(int? initialCapacity = null)
		{
			if (!initialCapacity.HasValue)
			{
				initialCapacity = s_LastAllocatedSize;
			}
			else if (initialCapacity < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(initialCapacity));
			}

			_Buffer = s_Pool.Rent(initialCapacity.Value);
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="PoolBackedCollection{T}"/> class.
		/// </summary>
		~PoolBackedCollection()
		{
			Dispose(false);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases the unmanaged resources used by this class and optionally releases the managed resources.
		/// </summary>
		/// <param name="isDisposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected virtual void Dispose(bool isDisposing)
		{
			if (isDisposing)
			{
				// Clear?
				s_Pool.Return(_Buffer);
				_Buffer = Array.Empty<T>();
				Count = 0;
			}
		}

#if NETSTANDARD2_1
		/// <summary>
		/// Return a <see cref="Span{T}" /> instance for the items in the collection.
		/// </summary>
		/// <returns>Created <see cref="Span{T}" />.</returns>
		public Span<T> AsSpan() => new Span<T>(_Buffer, 0, Count);

		/// <summary>
		/// Return a <see cref="Memory{T}" /> instance for the items in the collection.
		/// </summary>
		/// <returns>Created <see cref="Memory{T}" />.</returns>
		public Memory<T> AsMemory() => new Memory<T>(_Buffer, 0, Count);
#endif

		/// <summary>
		/// Compares two <see cref="PoolBackedCollection{T}" /> instances for equality.
		/// </summary>
		/// <param name="left">First <see cref="PoolBackedCollection{T}" /> instance.</param>
		/// <param name="right">Second <see cref="PoolBackedCollection{T}" /> instance.</param>
		/// <returns><see langword="true" /> if instances are equal.</returns>
		public static bool operator ==(PoolBackedCollection<T> left, PoolBackedCollection<T> right)
		{
			return left is null
				? right is null
				: left.Equals(right);
		}

		/// <summary>
		/// Compares two <see cref="PoolBackedCollection{T}" /> instances for inequality.
		/// </summary>
		/// <param name="left">First <see cref="PoolBackedCollection{T}" /> instance.</param>
		/// <param name="right">Second <see cref="PoolBackedCollection{T}" /> instance.</param>
		/// <returns><see langword="true" /> if instances are inequal.</returns>
		public static bool operator !=(PoolBackedCollection<T> left, PoolBackedCollection<T> right)
			=> !(left == right);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(T item)
		{
			T[] buffer = _Buffer;
			int count = Count;

			if ((uint)count < (uint)buffer.Length)
			{
				buffer[count] = item;
				Count++;
			}
			else
			{
				AddWithResize(item);
			}
		}

		// Non-inline from Add to improve its code quality as uncommon path
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void AddWithResize(T item)
		{
			int count = Count;

			T[] newBuffer = s_Pool.Rent(count * 2);

			if (newBuffer.Length > s_LastAllocatedSize)
				Interlocked.CompareExchange(ref s_LastAllocatedSize, newBuffer.Length, s_LastAllocatedSize);

#if !NETSTANDARD2_1
			Array.Copy(_Buffer, newBuffer, count);
#else
			Span<T> span = _Buffer.AsSpan(0, count);
			span.CopyTo(newBuffer);
#endif
			s_Pool.Return(_Buffer);

			newBuffer[count] = item;
			_Buffer = newBuffer;
			Count++;
		}

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			if (!IsEmpty)
			{
#if !NETSTANDARD2_1
				Array.Clear(_Buffer, 0, Count);
#else
				if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
				{
					Span<T> span = _Buffer.AsSpan(0, Count);
					span.Clear();
				}
#endif
				Count = 0;
			}
		}

		/// <inheritdoc/>
		public bool Contains(T item) => !IsEmpty && IndexOf(item) != -1;

		/// <summary>
		/// Searches for the specified object and returns the zero-based index of the first occurrence within the entire <see cref="PoolBackedCollection{T}"/>.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="PoolBackedCollection{T}"/>. The value can be <see langword="null"/> for reference types.</param>
		/// <returns>The zero-based index of the first occurrence of item within the entire <see cref="PoolBackedCollection{T}"/>, if found; otherwise, -1.</returns>
		public int IndexOf(T item)
			=> Array.IndexOf(_Buffer, item, 0, Count);

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="PoolBackedCollection{T}"/>.
		/// </summary>
		/// <param name="item">The object to remove from the <see cref="PoolBackedCollection{T}"/>. The value can be <see langword="null"/> for reference types.</param>
		/// <returns><see langword="true"/> if <paramref name="item"/> is successfully removed; otherwise, <see langword="false"/>. This method also returns <see langword="false"/> if item was not found in the <see cref="PoolBackedCollection{T}"/>.</returns>
		public bool Remove(T item)
		{
			int index = IndexOf(item);
			if (index >= 0)
			{
				RemoveAt(index);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Removes the element at the specified index of the <see cref="PoolBackedCollection{T}"/>.
		/// </summary>
		/// <param name="index">The zero-based index of the element to remove.</param>
		public void RemoveAt(int index)
		{
			if ((uint)index >= (uint)Count)
				throw new ArgumentOutOfRangeException(nameof(index));

			Count--;
			if (index < Count)
			{
				Array.Copy(_Buffer, index + 1, _Buffer, index, Count - index);
			}

#if NETSTANDARD2_0
			_Buffer[Count] = default;
#else
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
			{
				_Buffer[Count] = default!;
			}
#endif
		}

		/// <summary>
		/// Sets the element at the specified index of the <see cref="PoolBackedCollection{T}"/>.
		/// </summary>
		/// <param name="index">The zero-based index of the element to set.</param>
		/// <param name="item">The object to set in the <see cref="PoolBackedCollection{T}"/>. The value can be <see langword="null"/> for reference types.</param>
		public void SetAt(int index, T item)
		{
			if ((uint)index >= (uint)Count)
				throw new ArgumentOutOfRangeException(nameof(index));

			_Buffer[index] = item;
		}

		/// <summary>
		/// Copies the entire <see cref="PoolBackedCollection{T}"/> to a compatible one-dimensional array, starting at the beginning of the target array.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="PoolBackedCollection{T}"/>. The <see cref="Array"/> must have zero-based indexing.</param>
		public void CopyTo(T[] array)
			=> CopyTo(array, 0);

		/// <summary>
		/// Copies a range of elements from the <see cref="PoolBackedCollection{T}"/> to a compatible one-dimensional array, starting at the specified index of the target array.
		/// </summary>
		/// <param name="index">The zero-based index in the source <see cref="PoolBackedCollection{T}"/> at which copying begins.</param>
		/// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="PoolBackedCollection{T}"/>. The <see cref="Array"/> must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		/// <param name="count">The number of elements to copy.</param>
		public void CopyTo(int index, T[] array, int arrayIndex, int count)
		{
			if (Count - index < count)
				throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");

			Array.Copy(_Buffer, index, array, arrayIndex, count);
		}

		/// <inheritdoc/>
		public void CopyTo(T[] array, int arrayIndex)
			=> Array.Copy(_Buffer, 0, array, arrayIndex, Count);

		/// <inheritdoc/>
		public override bool Equals(object obj)
			=> obj is PoolBackedCollection<T> other && Equals(other);

		/// <inheritdoc/>
		public bool Equals(PoolBackedCollection<T> other)
		{
			if (Count != other?.Count)
				return false;

			for (int i = 0; i < Count; i++)
			{
				T left = _Buffer![i];
				T right = other._Buffer![i];

				bool LeftIsNull = left is null;
				if (LeftIsNull != (right is null))
					return false;

				if (!LeftIsNull && !left!.Equals(right))
					return false;
			}

			return true;
		}

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
		public Enumerator GetEnumerator() => new Enumerator(this);

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

		/// <summary>
		/// A struct for enumerating over items in a <see cref="PoolBackedCollection{T}" /> instance without allocation.
		/// </summary>
		public struct Enumerator : IEnumerator<T>, IEnumerator
		{
			private readonly T[] _Buffer;
			private readonly int _Count;
			private int _Index;

			/// <summary>
			/// Initializes a new instance of the <see cref="Enumerator"/> struct.
			/// </summary>
			/// <param name="list">The <see cref="PoolBackedCollection{T}" /> instance being enumerated.</param>
			public Enumerator(PoolBackedCollection<T> list)
			{
				_Buffer = list?._Buffer ?? Array.Empty<T>();
				_Count = list?.Count ?? 0;
				_Index = 0;
				Current = default!;
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
