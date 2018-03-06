﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

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
            out CustomModifiersTuple customModifiers,
            out ImmutableArray<ParameterSymbol> parameters,
            bool alsoCopyParamsModifier) // Last since always named.
        {
            Debug.Assert((object)sourceMethod != null);

            // Assert: none of the method's type parameters have been substituted
            Debug.Assert((object)sourceMethod == sourceMethod.ConstructedFrom);

            // For the most part, we will copy custom modifiers by copying types.
            // The only time when this fails is when the type refers to a type parameter
            // owned by the overridden method.  We need to replace all such references
            // with (equivalent) type parameters owned by this method.  We know that
            // we can perform this mapping positionally, because the method signatures
            // have already been compared.
            MethodSymbol constructedSourceMethod = sourceMethod.ConstructIfGeneric(destinationMethod.TypeArguments);

            customModifiers = CustomModifiersTuple.Create(
                constructedSourceMethod.ReturnTypeCustomModifiers,
                destinationMethod.RefKind != RefKind.None ? constructedSourceMethod.RefCustomModifiers : ImmutableArray<CustomModifier>.Empty);

            parameters = CopyParameterCustomModifiers(constructedSourceMethod.Parameters, destinationMethod.Parameters, alsoCopyParamsModifier);

            returnType = destinationMethod.ReturnType; // Default value - in case we don't copy the custom modifiers.

            // We do an extra check before copying the return type to handle the case where the overriding
            // method (incorrectly) has a different return type than the overridden method.  In such cases,
            // we want to retain the original (incorrect) return type to avoid hiding the return type
            // given in source.
            TypeSymbol returnTypeWithCustomModifiers = constructedSourceMethod.ReturnType;
            if (returnType.Equals(returnTypeWithCustomModifiers, TypeCompareKind.AllIgnoreOptions))
            {
                returnType = CopyTypeCustomModifiers(returnTypeWithCustomModifiers, returnType, destinationMethod.ContainingAssembly);
            }
        }

        /// <param name="sourceType">Type that already has custom modifiers.</param>
        /// <param name="destinationType">Same as <paramref name="sourceType"/>, but without custom modifiers.  May differ in object/dynamic.</param>
        /// <param name="containingAssembly">The assembly containing the signature referring to the destination type.</param>
        /// <returns><paramref name="destinationType"/> with custom modifiers copied from <paramref name="sourceType"/>.</returns>
        internal static TypeSymbol CopyTypeCustomModifiers(TypeSymbol sourceType, TypeSymbol destinationType, AssemblySymbol containingAssembly)
        {
            Debug.Assert(sourceType.Equals(destinationType, TypeCompareKind.AllIgnoreOptions));

            // NOTE: overrides can differ by object/dynamic.  If they do, we'll need to tweak newType before
            // we can use it in place of this.Type.  We do so by computing the dynamic transform flags that
            // code gen uses and then passing them to the dynamic type decoder that metadata reading uses.
            // NOTE: ref is irrelevant here since we are just encoding/decoding the type out of the signature context
            ImmutableArray<bool> flags = CSharpCompilation.DynamicTransformsEncoder.EncodeWithoutCustomModifierFlags(destinationType, RefKind.None);
            TypeSymbol typeWithDynamic = DynamicTypeDecoder.TransformTypeWithoutCustomModifierFlags(sourceType, containingAssembly, RefKind.None, flags);

            TypeSymbol resultType;
            if (destinationType.ContainsTuple() && !sourceType.Equals(destinationType, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreDynamic))
            {
                // We also preserve tuple names, if present and different
                ImmutableArray<string> names = CSharpCompilation.TupleNamesEncoder.Encode(destinationType);
                resultType = TupleTypeDecoder.DecodeTupleTypesIfApplicable(typeWithDynamic, names);
            }
            else
            {
                resultType = typeWithDynamic;
            }

            Debug.Assert(resultType.Equals(sourceType, TypeCompareKind.IgnoreDynamicAndTupleNames)); // Same custom modifiers as source type.
            Debug.Assert(resultType.Equals(destinationType, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds)); // Same object/dynamic and tuple names as destination type.
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

                if (sourceParameter.CustomModifiers.Any() || sourceParameter.RefCustomModifiers.Any() ||
                    sourceParameter.Type.HasCustomModifiers(flagNonDefaultArraySizesOrLowerBounds: true) ||
                    destinationParameter.CustomModifiers.Any() || destinationParameter.RefCustomModifiers.Any() ||
                    destinationParameter.Type.HasCustomModifiers(flagNonDefaultArraySizesOrLowerBounds: true) || // Could happen if the associated property has custom modifiers.
                    (alsoCopyParamsModifier && (sourceParameter.IsParams != destinationParameter.IsParams)))
                {
                    if (builder == null)
                    {
                        builder = ArrayBuilder<ParameterSymbol>.GetInstance();
                        builder.AddRange(destinationParameters, i); //add up to, but not including, the current parameter
                    }

                    bool newParams = alsoCopyParamsModifier ? sourceParameter.IsParams : destinationParameter.IsParams;
                    builder.Add(destinationParameter.WithCustomModifiersAndParams(sourceParameter.Type, 
                                                                                  sourceParameter.CustomModifiers,
                                                                                  destinationParameter.RefKind != RefKind.None ? sourceParameter.RefCustomModifiers : ImmutableArray<CustomModifier>.Empty,
                                                                                  newParams));
                }
                else if (builder != null)
                {
                    builder.Add(destinationParameter);
                }
            }

            return builder == null ? destinationParameters : builder.ToImmutableAndFree();
        }

        internal static bool HasInAttributeModifier(this ImmutableArray<CustomModifier> modifiers)
        {
            return modifiers.Any(modifier => !modifier.IsOptional && modifier.Modifier.IsWellKnownTypeInAttribute());
        }
    }
}
