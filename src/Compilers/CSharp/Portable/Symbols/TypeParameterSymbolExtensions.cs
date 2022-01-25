// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class TypeParameterSymbolExtensions
    {
        public static bool DependsOn(this TypeParameterSymbol typeParameter1, TypeParameterSymbol typeParameter2)
        {
            Debug.Assert((object)typeParameter1 != null);
            Debug.Assert((object)typeParameter2 != null);

            Stack<TypeParameterSymbol>? stack = null;
            HashSet<TypeParameterSymbol>? visited = null;

            while (true)
            {
                foreach (var constraintType in typeParameter1.ConstraintTypesNoUseSiteDiagnostics)
                {
                    if (constraintType.Type is TypeParameterSymbol typeParameter)
                    {
                        if (typeParameter.Equals(typeParameter2))
                        {
                            return true;
                        }
                        visited ??= new HashSet<TypeParameterSymbol>();
                        if (visited.Add(typeParameter))
                        {
                            stack ??= new Stack<TypeParameterSymbol>();
                            stack.Push(typeParameter);
                        }
                    }
                }
                if (stack is null || stack.Count == 0)
                {
                    break;
                }
                typeParameter1 = stack.Pop();
            }

            return false;
        }
    }
}
