// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    using TypeInfo = System.Reflection.TypeInfo;

    internal static class ReflectionHelpers
    {
        public static MethodInfo GetMethod(this TypeInfo typeInfo, string name, Type[] types)
        {
            foreach (var method in typeInfo.GetDeclaredMethods(name))
            {
                var parameters = method.GetParameters();
                if (parameters.Length == types.Length)
                {
                    for (int i = 0; i < types.Length; i++)
                    {
                        if (types[i] != parameters[i].ParameterType)
                        {
                            continue;
                        }
                    }

                    return method;
                }
            }

            return null;
        }

        public static FieldInfo GetField(this TypeInfo typeInfo, string name)
        {
            return typeInfo.GetDeclaredField(name);
        }
    }
}