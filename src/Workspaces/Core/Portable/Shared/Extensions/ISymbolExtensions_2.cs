// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ISymbolExtensions
    {
        public static bool IsImplicitValueParameter([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            if (symbol is IParameterSymbol { IsImplicitlyDeclared: true })
            {
                if (symbol.ContainingSymbol is IMethodSymbol method)
                {
                    if (method.MethodKind == MethodKind.EventAdd ||
                        method.MethodKind == MethodKind.EventRemove ||
                        method.MethodKind == MethodKind.PropertySet)
                    {
                        // the name is value in C#, and Value in VB
                        return symbol.Name == "value" || symbol.Name == "Value";
                    }
                }
            }

            return false;
        }
    }
}
