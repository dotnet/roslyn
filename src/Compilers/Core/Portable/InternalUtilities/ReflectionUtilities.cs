// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Roslyn.Utilities
{
    internal static class ReflectionUtilities
    {
        private static readonly Type Missing = typeof(void);

#if USES_ANNOTATIONS
        public static Type? TryGetType(string assemblyQualifiedName)
#else
        public static Type TryGetType(string assemblyQualifiedName)
#endif
        {
            try
            {
                // Note that throwOnError=false only suppresses some exceptions, not all.
                return Type.GetType(assemblyQualifiedName, throwOnError: false);
            }
            catch
            {
                return null;
            }
        }

#if USES_ANNOTATIONS
        public static Type? TryGetType(ref Type lazyType, string assemblyQualifiedName)
#else
        public static Type TryGetType(ref Type lazyType, string assemblyQualifiedName)
#endif
        {
            if (lazyType == null)
            {
                lazyType = TryGetType(assemblyQualifiedName) ?? Missing;
            }

            return (lazyType == Missing) ? null : lazyType;
        }

        /// <summary>
        /// Find a <see cref="Type"/> instance by first probing the contract name and then the name as it
        /// would exist in mscorlib.  This helps satisfy both the CoreCLR and Desktop scenarios. 
        /// </summary>
#if USES_ANNOTATIONS
        public static Type? GetTypeFromEither(string contractName, string desktopName)
#else
        public static Type GetTypeFromEither(string contractName, string desktopName)
#endif
        {
            var type = TryGetType(contractName);

            if (type == null)
            {
                type = TryGetType(desktopName);
            }

            return type;
        }

#if USES_ANNOTATIONS
        public static Type? GetTypeFromEither(ref Type lazyType, string contractName, string desktopName)
#else
        public static Type GetTypeFromEither(ref Type lazyType, string contractName, string desktopName)
#endif
        {
            if (lazyType == null)
            {
                lazyType = GetTypeFromEither(contractName, desktopName) ?? Missing;
            }

            return (lazyType == Missing) ? null : lazyType;
        }

#if USES_ANNOTATIONS
        public static T? FindItem<T>(IEnumerable<T> collection, params Type[] paramTypes)
#else
        public static T FindItem<T>(IEnumerable<T> collection, params Type[] paramTypes)
#endif
            where T : MethodBase
        {
            foreach (var current in collection)
            {
                var p = current.GetParameters();
                if (p.Length != paramTypes.Length)
                {
                    continue;
                }

                bool allMatch = true;
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    if (p[i].ParameterType != paramTypes[i])
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                {
                    return current;
                }
            }

            return null;
        }

#if USES_ANNOTATIONS
        internal static MethodInfo? GetDeclaredMethod(this TypeInfo typeInfo, string name, params Type[] paramTypes)
#else
        internal static MethodInfo GetDeclaredMethod(this TypeInfo typeInfo, string name, params Type[] paramTypes)
#endif
        {
            return FindItem(typeInfo.GetDeclaredMethods(name), paramTypes);
        }

#if USES_ANNOTATIONS
        internal static ConstructorInfo? GetDeclaredConstructor(this TypeInfo typeInfo, params Type[] paramTypes)
#else
        internal static ConstructorInfo GetDeclaredConstructor(this TypeInfo typeInfo, params Type[] paramTypes)
#endif
        {
            return FindItem(typeInfo.DeclaredConstructors, paramTypes);
        }

#if USES_ANNOTATIONS
        public static T? CreateDelegate<T>(this MethodInfo? methodInfo)
            where T : Delegate
#else
        public static T CreateDelegate<T>(this MethodInfo methodInfo)
#endif
        {
            if (methodInfo == null)
            {
                return default;
            }

            return (T)(object)methodInfo.CreateDelegate(typeof(T));
        }

#if USES_ANNOTATIONS
        public static T? InvokeConstructor<T>(this ConstructorInfo? constructorInfo, params object[]? args)
#else
        public static T InvokeConstructor<T>(this ConstructorInfo constructorInfo, params object[] args)
#endif
            where T : class
        {
            if (constructorInfo == null)
            {
                return null;
            }

            try
            {
                // PROTOTYPE(NullableDogfood): Invoke method needs annotation
                return (T)constructorInfo.Invoke(args);
            }
            catch (TargetInvocationException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                Debug.Assert(false, "Unreachable");

                return null;
            }
        }

#if USES_ANNOTATIONS
        public static object? InvokeConstructor(this ConstructorInfo constructorInfo, params object[] args)
#else
        public static object InvokeConstructor(this ConstructorInfo constructorInfo, params object[] args)
#endif
        {
            return constructorInfo.InvokeConstructor<object>(args);
        }

        public static T Invoke<T>(this MethodInfo methodInfo, object obj, params object[] args)
        {
            return (T)methodInfo.Invoke(obj, args);
        }
    }
}
