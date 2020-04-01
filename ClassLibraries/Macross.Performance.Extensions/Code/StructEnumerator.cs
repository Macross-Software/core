using System.Diagnostics;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Collections.Generic
{
	/// <summary>
	/// A delegate to be called as items are retrieved from an enumerator.
	/// </summary>
	/// <typeparam name="TItem">The item type.</typeparam>
	/// <typeparam name="TState">The state type which will be passed during enumeration.</typeparam>
	/// <param name="state">State to be passed on each call.</param>
	/// <param name="item">The item being processed.</param>
	/// <returns><see langword="true"/> to continue enumeration or <see langword="false"/> to break.</returns>
	public delegate bool StructEnumeratorForEachDelegate<TItem, TState>(ref TState state, TItem item)
		where TState : struct;

	/// <summary>
	/// A helper class for enumerating over an <see cref="IEnumerable{TItem}" /> instance without allocation if a struct GetEnumerator is available.
	/// </summary>
	/// <typeparam name="TEnumerable">The enumerable type.</typeparam>
	/// <typeparam name="TItem">The item type.</typeparam>
	/// <typeparam name="TState">The state type which will be passed during enumeration.</typeparam>
	public static class StructEnumerator<TEnumerable, TItem, TState>
		where TEnumerable : IEnumerable<TItem>
		where TState : struct
	{
		private static readonly MethodInfo s_GenericGetEnumeratorMethod = typeof(IEnumerable<TItem>).GetMethod("GetEnumerator");
		private static readonly MethodInfo s_GeneircCurrentGetMethod = typeof(IEnumerator<TItem>).GetProperty("Current").GetMethod;
		private static readonly MethodInfo s_MoveNextMethod = typeof(IEnumerator).GetMethod("MoveNext");
		private static readonly MethodInfo s_DisposeMethod = typeof(IDisposable).GetMethod("Dispose");
		private static readonly ConcurrentDictionary<Type, AllocationFreeForEachDelegate> s_AllocationFreeForEachDelegates = new ConcurrentDictionary<Type, AllocationFreeForEachDelegate>();
		private static readonly Func<Type, AllocationFreeForEachDelegate> s_BuildAllocationFreeForEachDelegateRef = BuildAllocationFreeForEachDelegate;

		private delegate void AllocationFreeForEachDelegate(TEnumerable instance, ref TState state, StructEnumeratorForEachDelegate<TItem, TState> itemCallback);

		/// <summary>
		/// Enumerates over an <see cref="IEnumerable{TItem}" /> instance without allocation if a struct GetEnumerator is available.
		/// </summary>
		/// <param name="instance">The enumerable instance.</param>
		/// <param name="state">State to be passed on each call.</param>
		/// <param name="itemCallback">Delegate to be called as items are retrieved from the enumerator.</param>
#pragma warning disable CA1000 // Do not declare static members on generic types
		public static void AllocationFreeForEach(TEnumerable instance, ref TState state, StructEnumeratorForEachDelegate<TItem, TState> itemCallback)
#pragma warning restore CA1000 // Do not declare static members on generic types
		{
			Debug.Assert(instance != null && itemCallback != null);

			Type type = instance.GetType();

			StructEnumerator<TEnumerable, TItem, TState>.AllocationFreeForEachDelegate allocationFreeForEachDelegate = s_AllocationFreeForEachDelegates.GetOrAdd(
				type,
				s_BuildAllocationFreeForEachDelegateRef);

			allocationFreeForEachDelegate(instance, ref state, itemCallback);
		}

		/* We want to do this type of logic...
			public static void AllocationFreeForEach(Dictionary<string, int> dictionary, ref TState state, ForEachDelegate itemCallback)
			{
				using (Dictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (!itemCallback(ref state, enumerator.Current))
							break;
					}
				}
			}
			...because it takes advantage of the struct Enumerator on the built-in types which give an allocation-free way to enumerate.
		*/
		private static AllocationFreeForEachDelegate BuildAllocationFreeForEachDelegate(Type enumerableType)
		{
			Type itemCallbackType = typeof(StructEnumeratorForEachDelegate<TItem, TState>);

			MethodInfo? getEnumeratorMethod = ResolveGetEnumeratorMethodForType(enumerableType);
			if (getEnumeratorMethod == null)
			{
				// Fallback to allocation mode and use IEnumerable<TItem>.GetEnumerator.
				// Primarily for Array.Empty and Enumerable.Empty case, but also for user types.
				getEnumeratorMethod = s_GenericGetEnumeratorMethod;
			}

			Type enumeratorType = getEnumeratorMethod.ReturnType;

			DynamicMethod dynamicMethod = new DynamicMethod(
				nameof(AllocationFreeForEach),
				null,
				new[] { typeof(TEnumerable), typeof(TState).MakeByRefType(), itemCallbackType },
				typeof(AllocationFreeForEachDelegate).Module,
				skipVisibility: true);

			ILGenerator generator = dynamicMethod.GetILGenerator();

			generator.DeclareLocal(enumeratorType);

			Label beginLoopLabel = generator.DefineLabel();
			Label processCurrentLabel = generator.DefineLabel();
			Label returnLabel = generator.DefineLabel();
			Label breakLoopLabel = generator.DefineLabel();

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Callvirt, getEnumeratorMethod);
			generator.Emit(OpCodes.Stloc_0);

			// try
			generator.BeginExceptionBlock();
			{
				generator.Emit(OpCodes.Br_S, beginLoopLabel);

				generator.MarkLabel(processCurrentLabel);

				generator.Emit(OpCodes.Ldarg_2);
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldloca_S, 0);
				generator.Emit(OpCodes.Constrained, enumeratorType);
				generator.Emit(OpCodes.Callvirt, s_GeneircCurrentGetMethod);

				generator.Emit(OpCodes.Callvirt, itemCallbackType.GetMethod("Invoke"));

				generator.Emit(OpCodes.Brtrue_S, beginLoopLabel);

				generator.Emit(OpCodes.Leave_S, returnLabel);

				generator.MarkLabel(beginLoopLabel);

				generator.Emit(OpCodes.Ldloca_S, 0);
				generator.Emit(OpCodes.Constrained, enumeratorType);
				generator.Emit(OpCodes.Callvirt, s_MoveNextMethod);

				generator.Emit(OpCodes.Brtrue_S, processCurrentLabel);

				generator.MarkLabel(breakLoopLabel);

				generator.Emit(OpCodes.Leave_S, returnLabel);
			}

			// finally
			generator.BeginFinallyBlock();
			{
				if (typeof(IDisposable).IsAssignableFrom(enumeratorType))
				{
					generator.Emit(OpCodes.Ldloca_S, 0);
					generator.Emit(OpCodes.Constrained, enumeratorType);
					generator.Emit(OpCodes.Callvirt, s_DisposeMethod);
				}
			}

			generator.EndExceptionBlock();

			generator.MarkLabel(returnLabel);

			generator.Emit(OpCodes.Ret);

			return (AllocationFreeForEachDelegate)dynamicMethod.CreateDelegate(typeof(AllocationFreeForEachDelegate));
		}

		private static MethodInfo? ResolveGetEnumeratorMethodForType(Type type)
		{
			MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			foreach (MethodInfo method in methods)
			{
				if (method.Name == "GetEnumerator"
					&& !method.ReturnType.IsInterface
					&& typeof(IEnumerator<TItem>).IsAssignableFrom(method.ReturnType)
					&& method.GetParameters().Length == 0)
				{
					return method;
				}
			}

			return null;
		}
	}
}
