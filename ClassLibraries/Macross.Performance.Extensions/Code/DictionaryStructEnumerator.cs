using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
	/// <summary>
	/// A helper class for enumerating over an <see cref="IDictionary{TKey, TValue}" /> instance without allocation if a struct GetEnumerator is available.
	/// </summary>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <typeparam name="TValue">The value type.</typeparam>
	/// <typeparam name="TState">The state type which will be passed during enumeration.</typeparam>
	public static class DictionaryStructEnumerator<TKey, TValue, TState>
		where TState : struct
	{
		/// <summary>
		/// Enumerates over an <see cref="IDictionary{TKey, TValue}" /> instance without allocation if a struct GetEnumerator is available.
		/// </summary>
		/// <param name="instance">The enumerable instance.</param>
		/// <param name="state">State to be passed on each call.</param>
		/// <param name="itemCallback">Delegate to be called as items are retrieved from the enumerator.</param>
#pragma warning disable CA1000 // Do not declare static members on generic types
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void AllocationFreeForEach(IEnumerable<KeyValuePair<TKey, TValue>> instance, ref TState state, StructEnumeratorForEachDelegate<KeyValuePair<TKey, TValue>, TState> itemCallback)
#pragma warning restore CA1000 // Do not declare static members on generic types
			=> StructEnumerator<IEnumerable<KeyValuePair<TKey, TValue>>, KeyValuePair<TKey, TValue>, TState>.AllocationFreeForEach(instance, ref state, itemCallback);

		/// <summary>
		/// Returns a delegate that can be reused to enumerate over an <see cref="IDictionary{TKey, TValue}" /> instance without allocation if a struct GetEnumerator is available.
		/// </summary>
		/// <remarks>
		/// A delegate can be reused only when the run-time type of the instance is going to be the same. For example, you will always enumerate using an <see cref="IDictionary{TKey, TValue}"/> over a <see cref="Dictionary{TKey, TValue}"/>.
		/// If the run-time is unknown, or could change, use <see cref="AllocationFreeForEach(IEnumerable{KeyValuePair{TKey, TValue}}, ref TState, StructEnumeratorForEachDelegate{KeyValuePair{TKey, TValue}, TState})"/> which
		/// will do a type check each invocation.
		/// </remarks>
		/// <param name="instance">The enumerable instance.</param>
		/// <returns><see cref="StructEnumerator{TEnumerable, TItem, TState}.AllocationFreeForEachDelegate" />.</returns>
#pragma warning disable CA1000 // Do not declare static members on generic types
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static StructEnumerator<IEnumerable<KeyValuePair<TKey, TValue>>, KeyValuePair<TKey, TValue>, TState>.AllocationFreeForEachDelegate FindAllocationFreeForEachDelegate(IEnumerable<KeyValuePair<TKey, TValue>> instance)
#pragma warning restore CA1000 // Do not declare static members on generic types
			=> StructEnumerator<IEnumerable<KeyValuePair<TKey, TValue>>, KeyValuePair<TKey, TValue>, TState>.FindAllocationFreeForEachDelegate(instance);
	}
}
