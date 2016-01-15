// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    class NullableTypeDecoder
    {
        internal static TypeSymbolWithAnnotations TransformType(
            TypeSymbolWithAnnotations metadataType,
            EntityHandle targetSymbolToken,
            PEModuleSymbol containingModule)
        {
            Debug.Assert((object)metadataType != null);

            ImmutableArray<bool> nullableTransformFlags;
            if (containingModule.Module.HasNullableAttribute(targetSymbolToken, out nullableTransformFlags))
            {
                int position = 0;
                TypeSymbolWithAnnotations result;

                if (metadataType.ApplyNullableTransforms(nullableTransformFlags, ref position, out result) && position == nullableTransformFlags.Length)
                {
                    return result;
                }
            }

            // No NullableAttribute applied to the target symbol, or flags do not line-up, return unchanged metadataType.
            return metadataType;
        }
    }
}
