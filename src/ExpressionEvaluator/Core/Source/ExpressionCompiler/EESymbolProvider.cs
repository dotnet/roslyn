﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class EESymbolProvider<TTypeSymbol, TLocalSymbol>
        where TTypeSymbol : class, ITypeSymbolInternal
        where TLocalSymbol : class, ILocalSymbolInternal
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

        public abstract TLocalSymbol GetLocalVariable(
            string? name,
            int slotIndex,
            LocalInfo<TTypeSymbol> info,
            ImmutableArray<bool> dynamicFlagsOpt,
            ImmutableArray<string?> tupleElementNamesOpt);

        public abstract TLocalSymbol GetLocalConstant(
            string name,
            TTypeSymbol type,
            ConstantValue value,
            ImmutableArray<bool> dynamicFlagsOpt,
            ImmutableArray<string?> tupleElementNamesOpt);

        /// <exception cref="BadImageFormatException"></exception>
        public abstract IAssemblySymbolInternal GetReferencedAssembly(AssemblyReferenceHandle handle);

        /// <exception cref="BadImageFormatException"></exception>
        public abstract TTypeSymbol GetType(EntityHandle handle);
    }
}
