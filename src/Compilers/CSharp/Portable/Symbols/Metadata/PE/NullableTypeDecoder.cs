// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            PEModuleSymbol containingModule,
            INonNullTypesContext nonNullTypesContext)
        {
            Debug.Assert(!metadataType.IsNull);

            ImmutableArray<bool> nullableTransformFlags;
            containingModule.Module.HasNullableAttribute(targetSymbolToken, out nullableTransformFlags);

            return TransformType(metadataType, nullableTransformFlags, nonNullTypesContext);
        }

        internal static TypeSymbolWithAnnotations TransformType(TypeSymbolWithAnnotations metadataType, ImmutableArray<bool> nullableTransformFlags, INonNullTypesContext nonNullTypesContext)
        {
            int position = 0;
            TypeSymbolWithAnnotations result;
            if (metadataType.ApplyNullableTransforms(nullableTransformFlags, nonNullTypesContext, ref position, out result) &&
                (nullableTransformFlags.IsDefault || position == nullableTransformFlags.Length))
            {
                return result;
            }

            // No NullableAttribute applied to the target symbol, or flags do not line-up, return unchanged metadataType.
            return metadataType;
        }

        // https://github.com/dotnet/roslyn/issues/29821 external annotations should be removed or fully designed/productized
        internal static TypeSymbolWithAnnotations TransformType(
            TypeSymbolWithAnnotations metadataType,
            EntityHandle targetSymbolToken,
            PEModuleSymbol containingModule,
            INonNullTypesContext nonNullTypesContext,
            ImmutableArray<bool> extraAnnotations)
        {
            if (extraAnnotations.IsDefault)
            {
                return NullableTypeDecoder.TransformType(metadataType, targetSymbolToken, containingModule, nonNullTypesContext);
            }
            else
            {
                return NullableTypeDecoder.TransformType(metadataType, extraAnnotations, NonNullTypesTrueContext.Instance);
            }
        }
    }
}
