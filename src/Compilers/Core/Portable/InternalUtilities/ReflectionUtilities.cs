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
        public static Type TryGetType(string assemblyQualifiedName)
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

        /// <summary>
        /// Find a <see cref="Type"/> instance by first probing the contract name and then the name as it
        /// would exist in mscorlib.  This helps satisfy both the CoreCLR and Desktop scenarios. 
        /// </summary>
        public static Type GetTypeFromEither(string contractName, string desktopName)
        {
            var type = TryGetType(contractName);

            if (type == null)
            {
                type = TryGetType(desktopName);
            }

            return type;
        }

        public static T FindItem<T>(IEnumerable<T> collection, params Type[] paramTypes)
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

        internal static MethodInfo GetDeclaredMethod(this TypeInfo typeInfo, string name, params Type[] paramTypes)
        {
            return FindItem(typeInfo.GetDeclaredMethods(name), paramTypes);
        }

        internal static ConstructorInfo GetDeclaredConstructor(this TypeInfo typeInfo, params Type[] paramTypes)
        {
            return FindItem(typeInfo.DeclaredConstructors, paramTypes);
        }

        public static T CreateDelegate<T>(this MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                return default(T);
            }

            return (T)(object)methodInfo.CreateDelegate(typeof(T));
        }

        public static T InvokeConstructor<T>(this ConstructorInfo constructorInfo, params object[] args)
        {
            if (constructorInfo == null)
            {
                return default(T);
            }

            try
            {
                return (T)constructorInfo.Invoke(args);
            }
            catch (TargetInvocationException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                Debug.Assert(false, "Unreachable");
                return default(T);
            }
        }

        public static object InvokeConstructor(this ConstructorInfo constructorInfo, params object[] args)
        {
            return constructorInfo.InvokeConstructor<object>(args);
        }

        public static T Invoke<T>(this MethodInfo methodInfo, object obj, params object[] args)
        {
            return (T)methodInfo.Invoke(obj, args);
        }
    }
}
