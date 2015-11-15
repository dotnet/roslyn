﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class CustomModifierUtils
    {
        /// <remarks>
        /// Out params are updated by assignment.  If you require thread-safety, pass temps and then
        /// CompareExchange them back into shared memory.
        /// </remarks>
        internal static void CopyMethodCustomModifiers(
            MethodSymbol sourceMethod,
            MethodSymbol destinationMethod,
            out TypeSymbol returnType,
            out ImmutableArray<CustomModifier> returnTypeCustomModifiers,
            out ImmutableArray<ParameterSymbol> parameters,
            bool alsoCopyParamsModifier) // Last since always named.
        {
            Debug.Assert((object)sourceMethod != null);

            // Assert: none of the method's type parameters have been substituted
            Debug.Assert((object)sourceMethod == sourceMethod.ConstructedFrom);

            returnTypeCustomModifiers = sourceMethod.ReturnTypeCustomModifiers;

            // For the most part, we will copy custom modifiers by copying types.
            // The only time when this fails is when the type refers to a type parameter
            // owned by the overridden method.  We need to replace all such references
            // with (equivalent) type parameters owned by this method.  We know that
            // we can perform this mapping positionally, because the method signatures
            // have already been compared.
            MethodSymbol constructedSourceMethod = sourceMethod.ConstructIfGeneric(destinationMethod.TypeArguments);

            parameters = CopyParameterCustomModifiers(constructedSourceMethod.Parameters, destinationMethod.Parameters, alsoCopyParamsModifier);

            returnType = destinationMethod.ReturnType; // Default value - in case we don't copy the custom modifiers.

            // We do an extra check before copying the return type to handle the case where the overriding
            // method (incorrectly) has a different return type than the overridden method.  In such cases,
            // we want to retain the original (incorrect) return type to avoid hiding the return type
            // given in source.
            TypeSymbol returnTypeWithCustomModifiers = constructedSourceMethod.ReturnType;
            if (returnType.Equals(returnTypeWithCustomModifiers, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true))
            {
                returnType = CopyTypeCustomModifiers(returnTypeWithCustomModifiers, returnType, RefKind.None, destinationMethod.ContainingAssembly);
            }
        }

        /// <param name="sourceType">Type that already has custom modifiers.</param>
        /// <param name="destinationType">Same as <paramref name="sourceType"/>, but without custom modifiers.  May differ in object/dynamic.</param>
        /// <param name="refKind"><see cref="RefKind"/> of the parameter of which this is the type (or <see cref="RefKind.None"/> for a return type.</param>
        /// <param name="containingAssembly">The assembly containing the signature referring to the destination type.</param>
        /// <returns><paramref name="destinationType"/> with custom modifiers copied from <paramref name="sourceType"/>.</returns>
        internal static TypeSymbol CopyTypeCustomModifiers(TypeSymbol sourceType, TypeSymbol destinationType, RefKind refKind, AssemblySymbol containingAssembly)
        {
            Debug.Assert(sourceType.Equals(destinationType, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: true));

            // NOTE: overrides can differ by object/dynamic.  If they do, we'll need to tweak newType before
            // we can use it in place of this.Type.  We do so by computing the dynamic transform flags that
            // code gen uses and then passing them to the dynamic type decoder that metadata reading uses.

            const int customModifierCount = 0;// Ignore custom modifiers, since we're not done copying them.
            ImmutableArray<bool> flags = CSharpCompilation.DynamicTransformsEncoder.Encode(destinationType, customModifierCount, refKind);
            TypeSymbol resultType = DynamicTypeDecoder.TransformTypeWithoutCustomModifierFlags(sourceType, containingAssembly, refKind, flags);

            Debug.Assert(resultType.Equals(sourceType, ignoreCustomModifiersAndArraySizesAndLowerBounds: false, ignoreDynamic: true)); // Same custom modifiers as source type.
            Debug.Assert(resultType.Equals(destinationType, ignoreCustomModifiersAndArraySizesAndLowerBounds: true, ignoreDynamic: false)); // Same object/dynamic as destination type.

            return resultType;
        }

        internal static ImmutableArray<ParameterSymbol> CopyParameterCustomModifiers(ImmutableArray<ParameterSymbol> sourceParameters, ImmutableArray<ParameterSymbol> destinationParameters, bool alsoCopyParamsModifier)
        {
            Debug.Assert(!destinationParameters.IsDefault);
            Debug.Assert(destinationParameters.All(p => p is SourceParameterSymbolBase));
            Debug.Assert(sourceParameters.Length == destinationParameters.Length);

            // Nearly all of the time, there will be no custom modifiers to copy, so don't
            // allocate the builder until we know that we need it.
            ArrayBuilder<ParameterSymbol> builder = null;

            int numParams = destinationParameters.Length;
            for (int i = 0; i < numParams; i++)
            {
                SourceParameterSymbolBase destinationParameter = (SourceParameterSymbolBase)destinationParameters[i];
                ParameterSymbol sourceParameter = sourceParameters[i];

                if (sourceParameter.CustomModifiers.Any() || sourceParameter.Type.HasCustomModifiers(flagNonDefaultArraySizesOrLowerBounds:true) ||
                    destinationParameter.CustomModifiers.Any() || destinationParameter.Type.HasCustomModifiers(flagNonDefaultArraySizesOrLowerBounds:true) || // Could happen if the associated property has custom modifiers.
                    (alsoCopyParamsModifier && (sourceParameter.IsParams != destinationParameter.IsParams)))
                {
                    if (builder == null)
                    {
                        builder = ArrayBuilder<ParameterSymbol>.GetInstance();
                        builder.AddRange(destinationParameters, i); //add up to, but not including, the current parameter
                    }

                    bool newParams = alsoCopyParamsModifier ? sourceParameter.IsParams : destinationParameter.IsParams;
                    builder.Add(destinationParameter.WithCustomModifiersAndParams(sourceParameter.Type, sourceParameter.CustomModifiers, 
                                                                                  destinationParameter.RefKind != RefKind.None ? sourceParameter.CountOfCustomModifiersPrecedingByRef : (ushort)0,
                                                                                  newParams));
                }
                else if (builder != null)
                {
                    builder.Add(destinationParameter);
                }
            }

            return builder == null ? destinationParameters : builder.ToImmutableAndFree();
        }
    }
}
