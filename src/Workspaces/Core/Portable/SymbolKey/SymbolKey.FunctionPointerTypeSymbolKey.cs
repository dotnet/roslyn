// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class FunctionPointerTypeSymbolKey
        {
            public static void Create(IFunctionPointerTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteRefKind(symbol.Signature.RefKind);
                visitor.WriteSymbolKey(symbol.Signature.ReturnType);
                visitor.WriteRefKindArray(symbol.Signature.Parameters);
                visitor.WriteParameterTypesArray(symbol.Signature.Parameters);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var returnRefKind = reader.ReadRefKind();
                var returnType = reader.ReadSymbolKey();
                using var paramRefKinds = reader.ReadRefKindArray();
                using var paramTypes = reader.ReadSymbolKeyArray<ITypeSymbol>();

                if (paramTypes.IsDefault || !(returnType.GetAnySymbol() is ITypeSymbol returnTypeSymbol))
                {
                    return default;
                }

                return new SymbolKeyResolution(reader.Compilation.CreateFunctionPointerTypeSymbol(
                    returnTypeSymbol, returnRefKind, paramTypes.ToImmutable(), paramRefKinds.ToImmutable()));
            }
        }
    }
}
