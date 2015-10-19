// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal abstract class SymbolFactory<ModuleSymbol, TypeSymbol>
        where TypeSymbol : class
    {
        internal abstract TypeSymbol GetUnsupportedMetadataTypeSymbol(ModuleSymbol moduleSymbol, BadImageFormatException exception);

        /// <summary>
        /// Produce unbound generic type symbol if the type is a generic type.
        /// </summary>
        internal abstract TypeSymbol MakeUnboundIfGeneric(ModuleSymbol moduleSymbol, TypeSymbol type);

        internal abstract TypeSymbol GetSZArrayTypeSymbol(ModuleSymbol moduleSymbol, TypeSymbol elementType, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers);
        internal abstract TypeSymbol GetMDArrayTypeSymbol(ModuleSymbol moduleSymbol, int rank, TypeSymbol elementType, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers,
                                                          ImmutableArray<int> sizes, ImmutableArray<int> lowerBounds);

        /// <summary>
        /// Produce constructed type symbol.
        /// </summary>
        /// <param name="moduleSymbol"></param>
        /// <param name="generic">
        /// Symbol for generic type.
        /// </param>
        /// <param name="arguments">
        /// Generic type arguments, including those for containing types.
        /// </param>
        /// <param name="refersToNoPiaLocalType">
        /// Flags for arguments. Each item indicates whether corresponding argument refers to NoPia local types.
        /// </param>
        internal abstract TypeSymbol SubstituteTypeParameters(ModuleSymbol moduleSymbol, TypeSymbol generic, ImmutableArray<KeyValuePair<TypeSymbol, ImmutableArray<ModifierInfo<TypeSymbol>>>> arguments, ImmutableArray<bool> refersToNoPiaLocalType);

        internal abstract TypeSymbol GetByRefReturnTypeSymbol(ModuleSymbol moduleSymbol, TypeSymbol referencedType, ushort countOfCustomModifiersPrecedingByRef);

        internal abstract TypeSymbol MakePointerTypeSymbol(ModuleSymbol moduleSymbol, TypeSymbol type, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers);
        internal abstract TypeSymbol GetSpecialType(ModuleSymbol moduleSymbol, SpecialType specialType);
        internal abstract TypeSymbol GetSystemTypeSymbol(ModuleSymbol moduleSymbol);
        internal abstract TypeSymbol GetEnumUnderlyingType(ModuleSymbol moduleSymbol, TypeSymbol type);

        internal abstract bool IsVolatileModifierType(ModuleSymbol moduleSymbol, TypeSymbol type);
        internal abstract Cci.PrimitiveTypeCode GetPrimitiveTypeCode(ModuleSymbol moduleSymbol, TypeSymbol type);
    }
}
