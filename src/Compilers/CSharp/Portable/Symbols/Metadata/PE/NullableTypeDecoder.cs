// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        internal static TypeWithAnnotations TransformType(
            TypeWithAnnotations metadataType,
            EntityHandle targetSymbolToken,
            PEModuleSymbol containingModule,
            Symbol accessSymbol,
            Symbol nullableContext)
        {
            Debug.Assert(metadataType.HasType);
            Debug.Assert(accessSymbol.IsDefinition);
            Debug.Assert((object)accessSymbol.ContainingModule == containingModule);
#if DEBUG
            // Ensure we could check accessibility at this point if we had to in ShouldDecodeNullableAttributes().
            // That is, ensure the accessibility of the symbol (and containing symbols) is available.
            _ = AccessCheck.IsEffectivelyPublicOrInternal(accessSymbol, out _);
#endif

            byte defaultTransformFlag;
            ImmutableArray<byte> nullableTransformFlags;
            if (!containingModule.Module.HasNullableAttribute(targetSymbolToken, out defaultTransformFlag, out nullableTransformFlags))
            {
                byte? value = nullableContext.GetNullableContextValue();
                if (value == null)
                {
                    return metadataType;
                }
                defaultTransformFlag = value.GetValueOrDefault();
            }

            if (!containingModule.ShouldDecodeNullableAttributes(accessSymbol))
            {
                return metadataType;
            }

            return TransformType(metadataType, defaultTransformFlag, nullableTransformFlags);
        }

        internal static TypeWithAnnotations TransformType(TypeWithAnnotations metadataType, byte defaultTransformFlag, ImmutableArray<byte> nullableTransformFlags)
        {
            if (nullableTransformFlags.IsDefault && defaultTransformFlag == 0)
            {
                return metadataType;
            }

            int position = 0;
            TypeWithAnnotations result;
            if (metadataType.ApplyNullableTransforms(defaultTransformFlag, nullableTransformFlags, ref position, out result) &&
                (nullableTransformFlags.IsDefault || position == nullableTransformFlags.Length))
            {
                return result;
            }

            // No NullableAttribute applied to the target symbol, or flags do not line-up, return unchanged metadataType.
            return metadataType;
        }
    }
}
