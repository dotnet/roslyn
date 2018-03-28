// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ISymbolExtensions
    {
        public static bool IsImplicitValueParameter(this ISymbol symbolOpt)
        {
            if (symbolOpt is IParameterSymbol && symbolOpt.IsImplicitlyDeclared)
            {
                if (symbolOpt.ContainingSymbol is IMethodSymbol method)
                {
                    if (method.MethodKind == MethodKind.EventAdd ||
                        method.MethodKind == MethodKind.EventRemove ||
                        method.MethodKind == MethodKind.PropertySet)
                    {
                        // the name is value in C#, and Value in VB
                        return symbolOpt.Name == "value" || symbolOpt.Name == "Value";
                    }
                }
            }

            return false;
        }
    }
}
