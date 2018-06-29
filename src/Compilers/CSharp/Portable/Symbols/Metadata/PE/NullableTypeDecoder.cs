﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    internal static class NullableTypeDecoder
    {
        /// <summary>
        /// If the type reference has an associated NullableAttribute, this method
        /// returns the type transformed to have IsNullable set to true or false
        /// (but not null) for each reference type in the type.
        /// </summary>
        internal static TypeSymbolWithAnnotations TransformType(
            TypeSymbolWithAnnotations metadataType,
            EntityHandle targetSymbolToken,
            PEModuleSymbol containingModule)
        {
            Debug.Assert((object)metadataType != null);

            ImmutableArray<bool> nullableTransformFlags;
            containingModule.Module.HasNullableAttribute(targetSymbolToken, out nullableTransformFlags);

            return TransformType(metadataType, nullableTransformFlags);
        }

        internal static TypeSymbolWithAnnotations TransformType(TypeSymbolWithAnnotations metadataType, ImmutableArray<bool> nullableTransformFlags)
        {
            int position = 0;
            TypeSymbolWithAnnotations result;
            // PROTOTYPE(NullableReferenceTypes): handle NonNullTypes from metadata
            if (metadataType.ApplyNullableTransforms(nullableTransformFlags, useNonNullTypes: true, ref position, out result) &&
                (nullableTransformFlags.IsDefault || position == nullableTransformFlags.Length))
            {
                return result;
            }

            // No NullableAttribute applied to the target symbol, or flags do not line-up, return unchanged metadataType.
            return metadataType;
        }

        internal static TypeSymbolWithAnnotations TransformOrEraseNullability(
            TypeSymbolWithAnnotations metadataType,
            EntityHandle targetSymbolToken,
            PEModuleSymbol containingModule)
        {
            if (containingModule.UtilizesNullableReferenceTypes)
            {
                return NullableTypeDecoder.TransformType(metadataType, targetSymbolToken, containingModule);
            }
            return metadataType.SetUnknownNullabilityForReferenceTypes();
        }

        // PROTOTYPE(NullableReferenceTypes): external annotations should be removed or fully designed/productized
        internal static TypeSymbolWithAnnotations TransformOrEraseNullability(
            TypeSymbolWithAnnotations metadataType,
            EntityHandle targetSymbolToken,
            PEModuleSymbol containingModule,
            ImmutableArray<bool> extraAnnotations)
        {
            if (extraAnnotations.IsDefault)
            {
                return NullableTypeDecoder.TransformOrEraseNullability(metadataType, targetSymbolToken, containingModule);
            }
            else
            {
                // PROTOTYPE(NullableReferenceTypes): Extra annotations always win (even if we're loading a modern assembly)
                return NullableTypeDecoder.TransformType(metadataType, extraAnnotations);
            }
        }
    }
}
