// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            out TypeWithAnnotations returnType,
            out ImmutableArray<CustomModifier> customModifiers,
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
            MethodSymbol constructedSourceMethod = sourceMethod.ConstructIfGeneric(destinationMethod.TypeArgumentsWithAnnotations);

            customModifiers =
                destinationMethod.RefKind != RefKind.None ? constructedSourceMethod.RefCustomModifiers : ImmutableArray<CustomModifier>.Empty;

            parameters = CopyParameterCustomModifiers(constructedSourceMethod.Parameters, destinationMethod.Parameters, alsoCopyParamsModifier);

            returnType = destinationMethod.ReturnTypeWithAnnotations; // Default value - in case we don't copy the custom modifiers.
            TypeSymbol returnTypeSymbol = returnType.Type;

            var sourceMethodReturnType = constructedSourceMethod.ReturnTypeWithAnnotations;

            // We do an extra check before copying the return type to handle the case where the overriding
            // method (incorrectly) has a different return type than the overridden method.  In such cases,
            // we want to retain the original (incorrect) return type to avoid hiding the return type
            // given in source.
            TypeSymbol returnTypeWithCustomModifiers = sourceMethodReturnType.Type;
            if (returnTypeSymbol.Equals(returnTypeWithCustomModifiers, TypeCompareKind.AllIgnoreOptions))
            {
                returnType = returnType.WithTypeAndModifiers(CopyTypeCustomModifiers(returnTypeWithCustomModifiers, returnTypeSymbol, destinationMethod.ContainingAssembly),
                                               sourceMethodReturnType.CustomModifiers);
            }
        }

        /// <param name="sourceType">Type that already has custom modifiers.</param>
        /// <param name="destinationType">Same as <paramref name="sourceType"/>, but without custom modifiers.
        /// May differ in object/dynamic, tuple element names, or other differences ignored by the runtime.</param>
        /// <param name="containingAssembly">The assembly containing the signature referring to the destination type.</param>
        /// <returns><paramref name="destinationType"/> with custom modifiers copied from <paramref name="sourceType"/>.</returns>
        internal static TypeSymbol CopyTypeCustomModifiers(TypeSymbol sourceType, TypeSymbol destinationType, AssemblySymbol containingAssembly)
        {
            Debug.Assert(sourceType.Equals(destinationType, TypeCompareKind.AllIgnoreOptions));

            const RefKind refKind = RefKind.None;

            // NOTE: overrides can differ by object/dynamic, tuple element names, etc.
            // If they do, we'll need to tweak destinationType before we can use it in place of sourceType.
            // NOTE: refKind is irrelevant here since we are just encoding/decoding the type.
            ImmutableArray<bool> flags = CSharpCompilation.DynamicTransformsEncoder.EncodeWithoutCustomModifierFlags(destinationType, refKind);
            TypeSymbol resultType = DynamicTypeDecoder.TransformTypeWithoutCustomModifierFlags(sourceType, containingAssembly, refKind, flags);

            if (!containingAssembly.RuntimeSupportsNumericIntPtr)
            {
                var builder = ArrayBuilder<bool>.GetInstance();
                CSharpCompilation.NativeIntegerTransformsEncoder.Encode(builder, destinationType);
                resultType = NativeIntegerTypeDecoder.TransformType(resultType, builder.ToImmutableAndFree());
            }

            if (destinationType.ContainsTuple() && !sourceType.Equals(destinationType, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreDynamic))
            {
                // We also preserve tuple names, if present and different
                ImmutableArray<string> names = CSharpCompilation.TupleNamesEncoder.Encode(destinationType);
                resultType = TupleTypeDecoder.DecodeTupleTypesIfApplicable(resultType, names);
            }

            // Preserve nullable modifiers as well.
            // https://github.com/dotnet/roslyn/issues/30077: Is it reasonable to copy annotations from the source?
            // If the destination had some of those annotations but not all, then clearly the destination
            // was incorrect. Or if the destination is C#7, then the destination will advertise annotations
            // that the author did not write and did not validate.
            var flagsBuilder = ArrayBuilder<byte>.GetInstance();
            destinationType.AddNullableTransforms(flagsBuilder);
            int position = 0;
            int length = flagsBuilder.Count;
            bool transformResult = resultType.ApplyNullableTransforms(defaultTransformFlag: 0, flagsBuilder.ToImmutableAndFree(), ref position, out resultType);
            Debug.Assert(transformResult && position == length);

            Debug.Assert(resultType.Equals(sourceType, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes | TypeCompareKind.IgnoreNativeIntegers)); // Same custom modifiers as source type.

            // Same object/dynamic, nullability, native integers, and tuple names as destination type.
            Debug.Assert(resultType.Equals(destinationType, TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds));

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

                if (sourceParameter.TypeWithAnnotations.CustomModifiers.Any() || sourceParameter.RefCustomModifiers.Any() ||
                    sourceParameter.Type.HasCustomModifiers(flagNonDefaultArraySizesOrLowerBounds: true) ||
                    destinationParameter.TypeWithAnnotations.CustomModifiers.Any() || destinationParameter.RefCustomModifiers.Any() ||
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
                                                                                  sourceParameter.TypeWithAnnotations.CustomModifiers,
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
            return modifiers.Any(static modifier => !modifier.IsOptional && ((CSharpCustomModifier)modifier).ModifierSymbol.IsWellKnownTypeInAttribute());
        }

        internal static bool HasRequiresLocationAttributeModifier(this ImmutableArray<CustomModifier> modifiers)
        {
            return modifiers.Any(static modifier => modifier.IsOptional && ((CSharpCustomModifier)modifier).ModifierSymbol.IsWellKnownTypeRequiresLocationAttribute());
        }

        internal static bool HasIsExternalInitModifier(this ImmutableArray<CustomModifier> modifiers)
        {
            return modifiers.Any(static modifier => !modifier.IsOptional && ((CSharpCustomModifier)modifier).ModifierSymbol.IsWellKnownTypeIsExternalInit());
        }

        internal static bool HasOutAttributeModifier(this ImmutableArray<CustomModifier> modifiers)
        {
            return modifiers.Any(static modifier => !modifier.IsOptional && ((CSharpCustomModifier)modifier).ModifierSymbol.IsWellKnownTypeOutAttribute());
        }
    }
}
