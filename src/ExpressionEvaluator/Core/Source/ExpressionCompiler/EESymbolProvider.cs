// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class EESymbolProvider<TTypeSymbol, TLocalSymbol>
        where TTypeSymbol : class, ITypeSymbol
        where TLocalSymbol : class
    {
        /// <summary>
        /// Windows PDB constant signature format.
        /// </summary>
        /// <exception cref="BadImageFormatException"></exception>
        /// <exception cref="UnsupportedSignatureContent"></exception>
        public abstract TTypeSymbol DecodeLocalVariableType(ImmutableArray<byte> signature);

        /// <summary>
        /// Portable PDB constant signature format.
        /// </summary>
        /// <exception cref="BadImageFormatException"></exception>
        /// <exception cref="UnsupportedSignatureContent"></exception>
        public abstract void DecodeLocalConstant(ref BlobReader reader, out TTypeSymbol type, out ConstantValue value);

        public abstract TTypeSymbol GetTypeSymbolForSerializedType(string typeName);

        public abstract TLocalSymbol GetLocalVariable(string name, int slotIndex, LocalInfo<TTypeSymbol> info, ImmutableArray<bool> dynamicFlagsOpt);
        public abstract TLocalSymbol GetLocalConstant(string name, TTypeSymbol type, ConstantValue value, ImmutableArray<bool> dynamicFlagsOpt);

        /// <exception cref="BadImageFormatException"></exception>
        public abstract IAssemblySymbol GetReferencedAssembly(AssemblyReferenceHandle handle);

        /// <exception cref="BadImageFormatException"></exception>
        public abstract TTypeSymbol GetType(EntityHandle handle);
    }
}
