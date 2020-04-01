using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
	/// <summary>
	/// A helper class for enumerating over an <see cref="IEnumerable{TValue}" /> instance without allocation if a struct GetEnumerator is available.
	/// </summary>
	/// <typeparam name="TValue">The value type.</typeparam>
	/// <typeparam name="TState">The state type which will be passed during enumeration.</typeparam>
	public static class ListStructEnumerator<TValue, TState>
		where TState : struct
	{
		/// <summary>
		/// Enumerates over an <see cref="IEnumerable{TValue}" /> instance without allocation if a struct GetEnumerator is available.
		/// </summary>
		/// <param name="instance">The enumerable instance.</param>
		/// <param name="state">State to be passed on each call.</param>
		/// <param name="itemCallback">Delegate to be called as items are retrieved from the enumerator.</param>
#pragma warning disable CA1000 // Do not declare static members on generic types
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void AllocationFreeForEach(IEnumerable<TValue> instance, ref TState state, StructEnumeratorForEachDelegate<TValue, TState> itemCallback)
#pragma warning restore CA1000 // Do not declare static members on generic types
			=> StructEnumerator<IEnumerable<TValue>, TValue, TState>.AllocationFreeForEach(instance, ref state, itemCallback);
	}
}
