using System.Buffers;
using System.Threading;
using System.Runtime.CompilerServices;
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
		private static readonly ArrayPool<T> s_Pool = ArrayPool<T>.Create(8192, 64);
		private static int s_LastAllocatedSize = 64;

		private readonly T[] _Buffer;

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
				// Following trick can reduce the range check by one
				if ((uint)index >= (uint)Count)
					throw new ArgumentOutOfRangeException(nameof(index));

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
			{
				initialSize = s_LastAllocatedSize;
			}
			else if (initialSize < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(initialSize));
			}

			_Buffer = s_Pool.Rent(initialSize.Value);
			Count = 0;
		}

		private StructPoolBackedCollection(T[] buffer, int count)
		{
			_Buffer = buffer;
			Count = count;
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
		/// Returns a new <see cref="StructPoolBackedCollection{T}" /> instance with the specified item added.
		/// </summary>
		/// <param name="item">The item to add.</param>
		/// <returns>Created <see cref="StructPoolBackedCollection{T}" /> instance.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public StructPoolBackedCollection<T> Add(T item)
		{
			T[] buffer = _Buffer;
			int count = Count;

			if ((uint)count < (uint)buffer.Length)
			{
				buffer[count] = item;
				return new StructPoolBackedCollection<T>(buffer, count + 1);
			}
			else
			{
				return AddWithResize(item);
			}
		}

		// Non-inline from Add to improve its code quality as uncommon path
		[MethodImpl(MethodImplOptions.NoInlining)]
		private StructPoolBackedCollection<T> AddWithResize(T item)
		{
			int count = Count;
			T[] buffer = _Buffer;

			T[] newBuffer = s_Pool.Rent(count * 2);

			if (newBuffer.Length > s_LastAllocatedSize)
				Interlocked.CompareExchange(ref s_LastAllocatedSize, newBuffer.Length, s_LastAllocatedSize);

#if !NETSTANDARD2_1
			Array.Copy(buffer, newBuffer, count);
#else
			Span<T> span = buffer.AsSpan(0, count);
			span.CopyTo(newBuffer);
#endif
			s_Pool.Return(buffer);

			newBuffer[count] = item;

			return new StructPoolBackedCollection<T>(newBuffer, count + 1);
		}

		/// <summary>
		/// Returns a new <see cref="StructPoolBackedCollection{T}" /> instance containing no items.
		/// </summary>
		/// <returns>Created <see cref="StructPoolBackedCollection{T}" /> instance.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public StructPoolBackedCollection<T> Clear()
		{
			if (!IsEmpty)
			{
				ClearInternal();
				return new StructPoolBackedCollection<T>(_Buffer, 0);
			}

			return this;
		}

		/// <summary>
		/// Returns the buffer backing the <see cref="StructPoolBackedCollection{T}" /> to the shared pool so it can be reused.
		/// The supplied <see cref="StructPoolBackedCollection{T}" /> should not be used after its buffer has been returned.
		/// </summary>
		/// <param name="clearBuffer">Setting to <see langword="true"/> causes the buffer to be cleared prior to being returned.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Return(bool clearBuffer = false)
		{
			if (clearBuffer)
				ClearInternal();
			s_Pool.Return(_Buffer);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ClearInternal()
#if !NETSTANDARD2_1
			=> Array.Clear(_Buffer, 0, Count);
#else
		{
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
			{
				Span<T> span = _Buffer.AsSpan(0, Count);
				span.Clear();
			}
		}
#endif

		/// <summary>
		/// Determines whether an element is in the <see cref="StructPoolBackedCollection{T}" />.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="StructPoolBackedCollection{T}" />. The value can be <see langword="null"/> for reference types.</param>
		/// <returns><see langword="true"/> if <paramref name="item"/> is found in the <see cref="StructPoolBackedCollection{T}" />; otherwise, <see langword="false"/>.</returns>
		public bool Contains(T item) => !IsEmpty && IndexOf(item) != -1;

		/// <summary>
		/// Searches for the specified object and returns the zero-based index of the first occurrence within the entire <see cref="StructPoolBackedCollection{T}"/>.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="StructPoolBackedCollection{T}"/>. The value can be <see langword="null"/> for reference types.</param>
		/// <returns>The zero-based index of the first occurrence of item within the entire <see cref="StructPoolBackedCollection{T}"/>, if found; otherwise, -1.</returns>
		public int IndexOf(T item)
			=> Array.IndexOf(_Buffer, item, 0, Count);

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="StructPoolBackedCollection{T}"/>.
		/// </summary>
		/// <param name="item">The object to remove from the <see cref="StructPoolBackedCollection{T}"/>. The value can be <see langword="null"/> for reference types.</param>
		/// <returns>A new <see cref="StructPoolBackedCollection{T}"/> instance if the item was successfully removed; otherwise the original instance is returned.</returns>
		public StructPoolBackedCollection<T> Remove(T item)
		{
			int index = IndexOf(item);

			return index >= 0
				? RemoveAt(index)
				: this;
		}

		/// <summary>
		/// Removes the element at the specified index of the <see cref="StructPoolBackedCollection{T}"/>.
		/// </summary>
		/// <param name="index">The zero-based index of the element to remove.</param>
		/// <returns>A new <see cref="StructPoolBackedCollection{T}"/> instance with the item removed.</returns>
		public StructPoolBackedCollection<T> RemoveAt(int index)
		{
			int count = Count;

			if ((uint)index >= (uint)count)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (index < --count)
			{
				Array.Copy(_Buffer, index + 1, _Buffer, index, count - index);
			}

#if NETSTANDARD2_0
			_Buffer[count] = default;
#else
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
			{
				_Buffer[count] = default!;
			}
#endif
			return new StructPoolBackedCollection<T>(_Buffer, count);
		}

		/// <summary>
		/// Sets the element at the specified index of the <see cref="StructPoolBackedCollection{T}"/>.
		/// </summary>
		/// <param name="index">The zero-based index of the element to set.</param>
		/// <param name="item">The object to set in the <see cref="StructPoolBackedCollection{T}"/>. The value can be <see langword="null"/> for reference types.</param>
		public void SetAt(int index, T item)
		{
			if ((uint)index >= (uint)Count)
				throw new ArgumentOutOfRangeException(nameof(index));

			_Buffer[index] = item;
		}

		/// <summary>
		/// Copies the entire <see cref="StructPoolBackedCollection{T}"/> to a compatible one-dimensional array, starting at the beginning of the target array.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="PoolBackedCollection{T}"/>. The <see cref="Array"/> must have zero-based indexing.</param>
		public void CopyTo(T[] array)
			=> CopyTo(array, 0);

		/// <summary>
		/// Copies a range of elements from the <see cref="StructPoolBackedCollection{T}"/> to a compatible one-dimensional array, starting at the specified index of the target array.
		/// </summary>
		/// <param name="index">The zero-based index in the source <see cref="StructPoolBackedCollection{T}"/> at which copying begins.</param>
		/// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="PoolBackedCollection{T}"/>. The <see cref="Array"/> must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		/// <param name="count">The number of elements to copy.</param>
		public void CopyTo(int index, T[] array, int arrayIndex, int count)
		{
			if (Count - index < count)
				throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");

			Array.Copy(_Buffer, index, array, arrayIndex, count);
		}

		/// <summary>
		/// Copies the entire <see cref="StructPoolBackedCollection{T}"/> to a compatible one-dimensional array, starting at the specified index of the target array.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="StructPoolBackedCollection{T}"/>. <see cref="Array"/> must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		public void CopyTo(T[] array, int arrayIndex)
			=> Array.Copy(_Buffer, 0, array, arrayIndex, Count);

		/// <inheritdoc/>
		public override bool Equals(object obj)
			=> obj is StructPoolBackedCollection<T> other && Equals(other);

		/// <inheritdoc/>
		public bool Equals(StructPoolBackedCollection<T> other)
		{
			if (Count != other.Count)
				return false;

			for (int i = 0; i < Count; i++)
			{
				T left = _Buffer[i];
				T right = other._Buffer[i];

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
			result = (31 * result) + _Buffer.GetHashCode();
			return result;
		}
#else
			=> HashCode.Combine(Count, _Buffer.GetHashCode());
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
				_Buffer = list._Buffer;
				_Count = list.Count;
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
