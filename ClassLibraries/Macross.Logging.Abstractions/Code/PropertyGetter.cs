using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Macross.Logging
{
	internal class PropertyGetter
	{
		public string PropertyName { get; }

		public Func<object, object> GetPropertyFunc { get; }

		public PropertyGetter(Type type, PropertyInfo propertyInfo)
		{
			PropertyName = propertyInfo.Name;

			GetPropertyFunc = BuildGetPropertyFunc(propertyInfo, type);
		}

		private static Func<object, object> BuildGetPropertyFunc(PropertyInfo propertyInfo, Type runtimePropertyType)
		{
			MethodInfo? realMethod = propertyInfo.GetMethod;
			Debug.Assert(realMethod != null);

			Type? declaringType = propertyInfo.DeclaringType;
			Debug.Assert(declaringType != null);

			Type declaredPropertyType = propertyInfo.PropertyType;

			DynamicMethod dynamicMethod = new DynamicMethod(
				nameof(PropertyGetter),
				typeof(object),
				new[] { typeof(object) },
				typeof(PropertyGetter).Module,
				skipVisibility: true);
			ILGenerator generator = dynamicMethod.GetILGenerator();

			generator.Emit(OpCodes.Ldarg_0);

			if (declaringType.IsValueType)
			{
				generator.Emit(OpCodes.Unbox, declaringType);
				generator.Emit(OpCodes.Call, realMethod);
			}
			else
			{
				generator.Emit(OpCodes.Castclass, declaringType);
				generator.Emit(OpCodes.Callvirt, realMethod);
			}

			if (declaredPropertyType != runtimePropertyType && declaredPropertyType.IsValueType)
			{
				generator.Emit(OpCodes.Box, declaredPropertyType);
			}

			generator.Emit(OpCodes.Ret);

			return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
		}
	}
}
