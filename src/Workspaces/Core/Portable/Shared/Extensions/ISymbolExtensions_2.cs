// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ISymbolExtensions
{
    extension([NotNullWhen(true)] ISymbol? symbol)
    {
        public bool IsImplicitValueParameter()
        {
            if (symbol is IParameterSymbol && symbol.IsImplicitlyDeclared)
            {
                if (symbol.ContainingSymbol is IMethodSymbol method)
                {
                    if (method.MethodKind is MethodKind.EventAdd or
                        MethodKind.EventRemove or
                        MethodKind.PropertySet)
                    {
                        // the name is value in C#, and Value in VB
                        return symbol.Name is "value" or "Value";
                    }
                }
            }

            return false;
        }
    }
}
