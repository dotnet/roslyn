// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class EESymbolProvider<TTypeSymbol, TLocalSymbol>
        where TTypeSymbol : class, ITypeSymbol
        where TLocalSymbol : class
    {
        // Local signature from StandaloneSig table
        // public abstract ImmutableArray<LocalInfo<TTypeSymbol>> DecodeLocalSignature(BlobReader signatureReader);

        // Windows PDB constant type
        public abstract TTypeSymbol DecodeLocalVariableType(ImmutableArray<byte> signature);

        // Portable PDB constant signature
        // public abstract void DecodeLocalConstantSignature(BlobReader signatureReader, out ImmutableArray<ModifierInfo<TTypeSymbol>> customModifiers, out TTypeSymbol type, out ConstantValue value);

        public abstract TLocalSymbol GetLocalVariable(string name, int slotIndex, LocalInfo<TTypeSymbol> info, ImmutableArray<bool> dynamicFlagsOpt);
        public abstract TLocalSymbol GetLocalConstant(string name, TTypeSymbol type, ConstantValue value, ImmutableArray<bool> dynamicFlagsOpt);
    }
}
