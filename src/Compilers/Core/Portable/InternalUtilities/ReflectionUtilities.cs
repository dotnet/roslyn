// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Roslyn.Utilities
{
    internal static class ReflectionUtilities
    {
        private static readonly Type Missing = typeof(void);

        public static Type? TryGetType(string assemblyQualifiedName)
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

        public static Type? TryGetType([NotNull] ref Type? lazyType, string assemblyQualifiedName)
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
        public static Type? GetTypeFromEither(string contractName, string desktopName)
        {
            var type = TryGetType(contractName);

            if (type == null)
            {
                type = TryGetType(desktopName);
            }

            return type;
        }

        public static Type? GetTypeFromEither([NotNull] ref Type? lazyType, string contractName, string desktopName)
        {
            if (lazyType == null)
            {
                lazyType = GetTypeFromEither(contractName, desktopName) ?? Missing;
            }

            return (lazyType == Missing) ? null : lazyType;
        }

        public static T? FindItem<T>(IEnumerable<T> collection, params Type[] paramTypes)
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

        internal static MethodInfo? GetDeclaredMethod(this TypeInfo typeInfo, string name, params Type[] paramTypes)
        {
            return FindItem(typeInfo.GetDeclaredMethods(name), paramTypes);
        }

        internal static ConstructorInfo? GetDeclaredConstructor(this TypeInfo typeInfo, params Type[] paramTypes)
        {
            return FindItem(typeInfo.DeclaredConstructors, paramTypes);
        }

        public static T? CreateDelegate<T>(this MethodInfo? methodInfo)
            where T : Delegate
        {
            if (methodInfo == null)
            {
                return null;
            }

            return (T)methodInfo.CreateDelegate(typeof(T));
        }

        public static T? InvokeConstructor<T>(this ConstructorInfo? constructorInfo, params object?[] args)
        {
            if (constructorInfo == null)
            {
                return default;
            }

            try
            {
                return (T?)constructorInfo.Invoke(args);
            }
            catch (TargetInvocationException e)
            {
                Debug.Assert(e.InnerException is object);
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                Debug.Assert(false, "Unreachable");
                return default;
            }
        }

        public static object? InvokeConstructor(this ConstructorInfo constructorInfo, params object?[] args)
        {
            return constructorInfo.InvokeConstructor<object?>(args);
        }

        public static T? Invoke<T>(this MethodInfo methodInfo, object obj, params object?[] args)
        {
            return (T?)methodInfo.Invoke(obj, args);
        }
    }
}
