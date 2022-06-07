// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// Decodes System.Runtime.CompilerServices.DynamicAttribute applied to a specified metadata symbol and
    /// transforms the specified metadata type, using the decoded dynamic transforms attribute argument,
    /// by replacing each occurrence of <see cref="System.Object"/> type with dynamic type.
    /// </summary>
    /// <remarks>
    /// This is a port of TypeManager::ImportDynamicTransformType from the native compiler.
    /// Comments from the C# design document for Dynamic:
    /// SPEC:   To represent the dynamic type in metadata, any indexer, field or return value typed as dynamic or known to be a constructed type
    /// SPEC:   containing dynamic will have each occurrence of dynamic erased to object and will be annotated with a [DynamicAttribute].
    /// SPEC:   If the relevant type is a constructed type, the attribute's constructor is passed a bool array.
    /// SPEC:   This array represents a preorder traversal of each "node" in the constructed type's "tree of types",
    /// SPEC:   with true set for each "node" that is dynamic, and false set for all other types.
    /// SPEC:   When dynamic occurs as part of the base type of a type, the applicable [DynamicAttribute] is applied to the type itself.
    /// </remarks>
    internal struct DynamicTypeDecoder
    {
        private readonly ImmutableArray<bool> _dynamicTransformFlags;
        private readonly AssemblySymbol _containingAssembly;
        private readonly bool _haveCustomModifierFlags;
        private readonly bool _checkLength;

        /// <remarks>
        /// Should be accessed through <see cref="HasFlag"/>, <see cref="PeekFlag"/>, and <see cref="ConsumeFlag"/>.
        /// </remarks>
        private int _index;

        private DynamicTypeDecoder(ImmutableArray<bool> dynamicTransformFlags, bool haveCustomModifierFlags, bool checkLength, AssemblySymbol containingAssembly)
        {
            Debug.Assert(!dynamicTransformFlags.IsEmpty);
            Debug.Assert((object)containingAssembly != null);

            _dynamicTransformFlags = dynamicTransformFlags;
            _containingAssembly = containingAssembly;
            _haveCustomModifierFlags = haveCustomModifierFlags;
            _checkLength = checkLength;
            _index = 0;
        }

        /// <summary>
        /// Decodes the attributes applied to the given <see paramref="targetSymbol"/> from metadata and checks if System.Runtime.CompilerServices.DynamicAttribute is applied.
        /// If so, it transforms the given <see paramref="metadataType"/>, using the decoded dynamic transforms attribute argument,
        /// by replacing each occurrence of <see cref="System.Object"/> type with dynamic type.
        /// If no System.Runtime.CompilerServices.DynamicAttribute is applied or the decoded dynamic transforms attribute argument is erroneous,
        /// returns the unchanged <see paramref="metadataType"/>.
        /// </summary>
        /// <remarks>This method is a port of TypeManager::ImportDynamicTransformType from the native compiler.</remarks>
        internal static TypeSymbol TransformType(
            TypeSymbol metadataType,
            int targetSymbolCustomModifierCount,
            EntityHandle targetSymbolToken,
            PEModuleSymbol containingModule,
            RefKind targetSymbolRefKind = RefKind.None)
        {
            Debug.Assert((object)metadataType != null);

            ImmutableArray<bool> dynamicTransformFlags;
            if (containingModule.Module.HasDynamicAttribute(targetSymbolToken, out dynamicTransformFlags))
            {
                return TransformTypeInternal(metadataType, containingModule.ContainingAssembly,
                    targetSymbolCustomModifierCount, targetSymbolRefKind, dynamicTransformFlags,
                    haveCustomModifierFlags: true,
                    checkLength: true);
            }

            // No DynamicAttribute applied to the target symbol, return unchanged metadataType.
            return metadataType;
        }

        internal static TypeSymbol TransformTypeWithoutCustomModifierFlags(
            TypeSymbol type,
            AssemblySymbol containingAssembly,
            RefKind targetSymbolRefKind,
            ImmutableArray<bool> dynamicTransformFlags,
            bool checkLength = true)
        {
            return TransformTypeInternal(
                type,
                containingAssembly,
                0,
                targetSymbolRefKind,
                dynamicTransformFlags,
                haveCustomModifierFlags: false,
                checkLength: checkLength);
        }

        private static TypeSymbol TransformTypeInternal(
            TypeSymbol metadataType,
            AssemblySymbol containingAssembly,
            int targetSymbolCustomModifierCount,
            RefKind targetSymbolRefKind,
            ImmutableArray<bool> dynamicTransformFlags,
            bool haveCustomModifierFlags,
            bool checkLength)
        {
            Debug.Assert((object)metadataType != null);
            Debug.Assert((object)containingAssembly != null);
            Debug.Assert(!dynamicTransformFlags.IsDefault);

            if (dynamicTransformFlags.Length == 0)
            {
                return new UnsupportedMetadataTypeSymbol();
            }

            var decoder = new DynamicTypeDecoder(dynamicTransformFlags, haveCustomModifierFlags, checkLength, containingAssembly);

            // Native compiler encodes bools (always false) for custom modifiers and parameter ref-kinds, if ref-kind is ref or out.
            if (decoder.HandleCustomModifiers(targetSymbolCustomModifierCount) && decoder.HandleRefKind(targetSymbolRefKind))
            {
                TypeSymbol transformedType = decoder.TransformType(metadataType);

                if ((object)transformedType != null && (!checkLength || decoder._index == dynamicTransformFlags.Length))
                {
                    // Even when we're not checking the length, there shouldn't be any unconsumed "true"s.
                    Debug.Assert(checkLength || decoder._dynamicTransformFlags.LastIndexOf(true) < decoder._index);
                    return transformedType;
                }
            }

            // We ignore the dynamic transformation and return unchanged metadataType to match Dev11 behavior.
            return metadataType;
        }

        private TypeSymbol TransformType(TypeSymbol type)
        {
            Debug.Assert(_index >= 0);

            if (!HasFlag ||
                PeekFlag() && (type.SpecialType != SpecialType.System_Object && !type.IsDynamic()))
            {
                // Bail, since flags are invalid.
                return null;
            }

            switch (type.Kind)
            {
                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                    if (type.SpecialType == SpecialType.System_Object)
                    {
                        // Replace the given System.Object type with dynamic type if the corresponding dynamicTransformFlag is set to true.
                        return ConsumeFlag() ? DynamicTypeSymbol.Instance : type;
                    }

                    return TransformNamedType((NamedTypeSymbol)type);

                case SymbolKind.ArrayType:
                    return TransformArrayType((ArrayTypeSymbol)type);

                case SymbolKind.PointerType:
                    return TransformPointerType((PointerTypeSymbol)type);

                case SymbolKind.FunctionPointerType:
                    return TransformFunctionPointerType((FunctionPointerTypeSymbol)type);

                case SymbolKind.DynamicType:
                    Debug.Assert(!_haveCustomModifierFlags, "This shouldn't happen during decoding.");
                    return ConsumeFlag()
                        ? type
                        : _containingAssembly.GetSpecialType(SpecialType.System_Object);

                default:
                    ConsumeFlag();
                    return HandleCustomModifiers(type.CustomModifierCount()) ? type : null;
            }
        }

        // Native compiler encodes bools (always false) for custom modifiers and parameter ref-kinds, if ref-kind is ref or out.
        private bool HandleCustomModifiers(int customModifiersCount)
        {
            // If we're in source, then we're actually working on copying custom modifiers,
            // so we should not assume they are in their final state.  Instead, we will
            // ignore them completely.
            if (!_haveCustomModifierFlags)
            {
                return true;
            }

            Debug.Assert(customModifiersCount >= 0);

            for (int i = 0; i < customModifiersCount; i++)
            {
                if (!HasFlag || ConsumeFlag())
                {
                    return false;
                }
            }

            return true;
        }

        // Native compiler encodes bools (always false) for custom modifiers and parameter ref-kinds, if ref-kind is not none.
        private bool HandleRefKind(RefKind refKind)
        {
            Debug.Assert(_index >= 0);
            return refKind == RefKind.None || !ConsumeFlag();
        }

        private NamedTypeSymbol TransformNamedType(NamedTypeSymbol namedType, bool isContaining = false)
        {
            // Native compiler encodes a bool for the given namedType, but none for its containing types.
            if (!isContaining)
            {
                var flag = ConsumeFlag();
                Debug.Assert(!flag);
            }

            NamedTypeSymbol containingType = namedType.ContainingType;
            NamedTypeSymbol newContainingType;
            if ((object)containingType != null && containingType.IsGenericType)
            {
                newContainingType = TransformNamedType(namedType.ContainingType, isContaining: true);
                if ((object)newContainingType == null)
                {
                    return null;
                }

                Debug.Assert(newContainingType.IsGenericType);
            }
            else
            {
                newContainingType = containingType;
            }

            // Native compiler encodes bools for each type argument, starting from type arguments for the outermost containing type to those for the given namedType.
            ImmutableArray<TypeWithAnnotations> typeArguments = namedType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;

            ImmutableArray<TypeWithAnnotations> transformedTypeArguments = TransformTypeArguments(typeArguments); // Note, modifiers are not involved, this is behavior of the native compiler.

            if (transformedTypeArguments.IsDefault)
            {
                return null;
            }

            // Construct a new namedType, if required.
            bool containerIsChanged = (!TypeSymbol.Equals(newContainingType, containingType, TypeCompareKind.ConsiderEverything2));

            if (containerIsChanged || transformedTypeArguments != typeArguments)
            {
                if (containerIsChanged)
                {
                    namedType = namedType.OriginalDefinition.AsMember(newContainingType);
                    return namedType.ConstructIfGeneric(transformedTypeArguments);
                }

                return namedType.ConstructedFrom.Construct(transformedTypeArguments, unbound: false).WithTupleDataFrom(namedType);
            }
            else
            {
                return namedType;
            }
        }

        private ImmutableArray<TypeWithAnnotations> TransformTypeArguments(ImmutableArray<TypeWithAnnotations> typeArguments)
        {
            if (!typeArguments.Any())
            {
                return typeArguments;
            }

            var transformedTypeArgsBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            bool anyTransformed = false;
            foreach (var typeArg in typeArguments)
            {
                TypeSymbol transformedTypeArg = TransformType(typeArg.Type);
                if ((object)transformedTypeArg == null)
                {
                    transformedTypeArgsBuilder.Free();
                    return default(ImmutableArray<TypeWithAnnotations>);
                }

                // Note, modifiers are not involved, this is behavior of the native compiler.
                transformedTypeArgsBuilder.Add(typeArg.WithTypeAndModifiers(transformedTypeArg, typeArg.CustomModifiers));
                anyTransformed |= !TypeSymbol.Equals(transformedTypeArg, typeArg.Type, TypeCompareKind.ConsiderEverything2);
            }

            if (!anyTransformed)
            {
                transformedTypeArgsBuilder.Free();
                return typeArguments;
            }

            return transformedTypeArgsBuilder.ToImmutableAndFree();
        }

        private ArrayTypeSymbol TransformArrayType(ArrayTypeSymbol arrayType)
        {
            var flag = ConsumeFlag();
            Debug.Assert(!flag);

            if (!HandleCustomModifiers(arrayType.ElementTypeWithAnnotations.CustomModifiers.Length))
            {
                return null;
            }

            TypeSymbol transformedElementType = TransformType(arrayType.ElementType);
            if ((object)transformedElementType == null)
            {
                return null;
            }

            return TypeSymbol.Equals(transformedElementType, arrayType.ElementType, TypeCompareKind.ConsiderEverything2) ?
                arrayType :
                arrayType.IsSZArray ?
                    ArrayTypeSymbol.CreateSZArray(_containingAssembly, arrayType.ElementTypeWithAnnotations.WithTypeAndModifiers(transformedElementType, arrayType.ElementTypeWithAnnotations.CustomModifiers)) :
                    ArrayTypeSymbol.CreateMDArray(_containingAssembly, arrayType.ElementTypeWithAnnotations.WithTypeAndModifiers(transformedElementType, arrayType.ElementTypeWithAnnotations.CustomModifiers), arrayType.Rank, arrayType.Sizes, arrayType.LowerBounds);
        }

        private PointerTypeSymbol TransformPointerType(PointerTypeSymbol pointerType)
        {
            var flag = ConsumeFlag();
            Debug.Assert(!flag);

            if (!HandleCustomModifiers(pointerType.PointedAtTypeWithAnnotations.CustomModifiers.Length))
            {
                return null;
            }

            TypeSymbol transformedPointedAtType = TransformType(pointerType.PointedAtType);
            if ((object)transformedPointedAtType == null)
            {
                return null;
            }

            return TypeSymbol.Equals(transformedPointedAtType, pointerType.PointedAtType, TypeCompareKind.ConsiderEverything2) ?
                pointerType :
                new PointerTypeSymbol(pointerType.PointedAtTypeWithAnnotations.WithTypeAndModifiers(transformedPointedAtType, pointerType.PointedAtTypeWithAnnotations.CustomModifiers));
        }

#nullable enable
        private FunctionPointerTypeSymbol? TransformFunctionPointerType(FunctionPointerTypeSymbol type)
        {
            var flag = ConsumeFlag();
            Debug.Assert(!flag);

            var sig = type.Signature;

            var (transformedReturnWithAnnotations, madeChanges) = handle(ref this, sig.RefKind, sig.RefCustomModifiers, sig.ReturnTypeWithAnnotations);
            if (transformedReturnWithAnnotations.IsDefault)
            {
                return null;
            }

            var transformedParameters = ImmutableArray<TypeWithAnnotations>.Empty;
            if (sig.ParameterCount > 0)
            {
                var paramsTransformed = false;
                var paramsBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(sig.ParameterCount);
                try
                {
                    foreach (var param in sig.Parameters)
                    {
                        var (transformedParamType, paramTransformed) = handle(ref this, param.RefKind, param.RefCustomModifiers, param.TypeWithAnnotations);
                        if (transformedParamType.IsDefault)
                        {
                            return null;
                        }

                        paramsBuilder.Add(transformedParamType);
                        paramsTransformed |= paramTransformed;
                    }

                    transformedParameters = paramsTransformed ? paramsBuilder.ToImmutable() : sig.ParameterTypesWithAnnotations;
                    madeChanges |= paramsTransformed;
                }
                finally
                {
                    paramsBuilder.Free();
                }
            }

            if (madeChanges)
            {
                return type.SubstituteTypeSymbol(transformedReturnWithAnnotations, transformedParameters,
                                                 refCustomModifiers: default, paramRefCustomModifiers: default);
            }
            else
            {
                return type;
            }

            static (TypeWithAnnotations, bool madeChanges) handle(ref DynamicTypeDecoder decoder, RefKind refKind, ImmutableArray<CustomModifier> refCustomModifiers, TypeWithAnnotations typeWithAnnotations)
            {
                if (!decoder.HandleCustomModifiers(refCustomModifiers.Length)
                    || !decoder.HandleRefKind(refKind)
                    || !decoder.HandleCustomModifiers(typeWithAnnotations.CustomModifiers.Length))
                {
                    return (default, false);
                }

                var transformedType = decoder.TransformType(typeWithAnnotations.Type);
                if (transformedType is null)
                {
                    return (default, false);
                }

                if (transformedType.Equals(typeWithAnnotations.Type, TypeCompareKind.ConsiderEverything))
                {
                    return (typeWithAnnotations, false);
                }

                return (typeWithAnnotations.WithType(transformedType), true);
            }
        }
#nullable disable

        private bool HasFlag => _index < _dynamicTransformFlags.Length || !_checkLength;

        private bool PeekFlag() => _index < _dynamicTransformFlags.Length && _dynamicTransformFlags[_index];

        private bool ConsumeFlag()
        {
            var result = PeekFlag();
            _index++;
            return result;
        }
    }
}
