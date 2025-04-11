// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private static class TypeParameterOrdinalSymbolKey
    {
        public static void Create(ITypeParameterSymbol symbol, int methodIndex, SymbolKeyWriter visitor)
        {
            Contract.ThrowIfFalse(symbol.TypeParameterKind == TypeParameterKind.Method);
            visitor.WriteInteger(methodIndex);
            visitor.WriteInteger(symbol.Ordinal);
        }

        public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
        {
            var methodIndex = reader.ReadInteger();
            var ordinal = reader.ReadInteger();
            var method = reader.ResolveMethod(methodIndex);

            var typeParameter = method?.TypeParameters[ordinal];
            if (typeParameter == null)
            {
                failureReason = $"({nameof(TypeParameterOrdinalSymbolKey)} failed)";
                return default;
            }

            failureReason = null;
            return new SymbolKeyResolution(typeParameter);
        }
    }
}
