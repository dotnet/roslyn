// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// In C#, tuples can be represented using tuple syntax and be given
    /// names. However, the underlying representation for tuples unifies
    /// to a single underlying tuple type, System.ValueTuple. Since the
    /// names aren't part of the underlying tuple type they have to be
    /// recorded somewhere else.
    /// 
    /// Roslyn records tuple names in an attribute: the
    /// TupleElementNamesAttribute. The attribute contains a single string
    /// array which records the names of the tuple elements in a pre-order
    /// depth-first traversal. If the type contains nested parameters,
    /// they are also recorded in a pre-order depth-first traversal.
    /// <see cref="DecodeTupleTypesIfApplicable(TypeSymbol, EntityHandle, PEModuleSymbol)"/>
    /// can be used to extract tuple names and types from metadata and create
    /// a <see cref="NamedTypeSymbol"/> with attached names.
    /// 
    /// <example>
    /// For instance, a method returning a tuple
    /// 
    /// <code>
    ///     (int x, int y) M() { ... }
    /// </code>
    ///
    /// will be encoded using an attribute on the return type as follows
    /// 
    /// <code>
    ///     [return: TupleElementNamesAttribute(new[] { "x", "y" })]
    ///     System.ValueTuple&lt;int, int&gt; M() { ... }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// For nested type parameters, we expand the tuple names in a pre-order
    /// traversal:
    /// 
    /// <code>
    ///     class C : BaseType&lt;((int e1, int e2) e3, int e4)&lt; { ... }
    /// </code>
    ///
    /// becomes
    /// 
    /// <code>
    ///     [TupleElementNamesAttribute(new[] { "e3", "e4", "e1", "e2" });
    ///     class C : BaseType&lt;System.ValueTuple&lt;
    ///         System.ValueTuple&lt;int,int&gt;, int&gt;
    ///     { ... }
    /// </code>
    /// </example>
    /// </summary>
    internal struct TupleTypeDecoder
    {
        private readonly ImmutableArray<string?> _elementNames;
        // Keep track of how many names we've "used" during decoding. Starts at
        // the back of the array and moves forward.
        private int _namesIndex;
        private bool _foundUsableErrorType;
        private bool _decodingFailed;

        private TupleTypeDecoder(ImmutableArray<string?> elementNames)
        {
            _elementNames = elementNames;
            _namesIndex = elementNames.IsDefault ? 0 : elementNames.Length;
            _decodingFailed = false;
            _foundUsableErrorType = false;
        }

        public static TypeSymbol DecodeTupleTypesIfApplicable(
            TypeSymbol metadataType,
            EntityHandle targetHandle,
            PEModuleSymbol containingModule)
        {
            ImmutableArray<string?> elementNames;
            var hasTupleElementNamesAttribute = containingModule
                .Module
                .HasTupleElementNamesAttribute(targetHandle, out elementNames);

            // If we have the TupleElementNamesAttribute, but no names, that's
            // bad metadata
            if (hasTupleElementNamesAttribute && elementNames.IsDefaultOrEmpty)
            {
                return new UnsupportedMetadataTypeSymbol();
            }

            return DecodeTupleTypesInternal(metadataType, elementNames, hasTupleElementNamesAttribute);
        }

        public static TypeWithAnnotations DecodeTupleTypesIfApplicable(
            TypeWithAnnotations metadataType,
            EntityHandle targetHandle,
            PEModuleSymbol containingModule)
        {
            ImmutableArray<string?> elementNames;
            var hasTupleElementNamesAttribute = containingModule
                .Module
                .HasTupleElementNamesAttribute(targetHandle, out elementNames);

            // If we have the TupleElementNamesAttribute, but no names, that's
            // bad metadata
            if (hasTupleElementNamesAttribute && elementNames.IsDefaultOrEmpty)
            {
                return TypeWithAnnotations.Create(new UnsupportedMetadataTypeSymbol());
            }

            TypeSymbol type = metadataType.Type;
            TypeSymbol decoded = DecodeTupleTypesInternal(type, elementNames, hasTupleElementNamesAttribute);
            return (object)decoded == (object)type ?
                metadataType :
                TypeWithAnnotations.Create(decoded, metadataType.NullableAnnotation, metadataType.CustomModifiers);
        }

        public static TypeSymbol DecodeTupleTypesIfApplicable(
            TypeSymbol metadataType,
            ImmutableArray<string?> elementNames)
        {
            return DecodeTupleTypesInternal(metadataType, elementNames, hasTupleElementNamesAttribute: !elementNames.IsDefaultOrEmpty);
        }

        private static TypeSymbol DecodeTupleTypesInternal(TypeSymbol metadataType, ImmutableArray<string?> elementNames, bool hasTupleElementNamesAttribute)
        {
            RoslynDebug.AssertNotNull(metadataType);

            var decoder = new TupleTypeDecoder(elementNames);
            var decoded = decoder.DecodeType(metadataType);
            if (!decoder._decodingFailed)
            {
                if (!hasTupleElementNamesAttribute || decoder._namesIndex == 0)
                {
                    return decoded;
                }
            }

            // If not all of the names have been used, the metadata is bad
            if (decoder._foundUsableErrorType)
            {
                return metadataType;
            }

            // Bad metadata
            return new UnsupportedMetadataTypeSymbol();
        }

        private TypeSymbol DecodeType(TypeSymbol type)
        {
            switch (type.Kind)
            {
                case SymbolKind.ErrorType:
                    _foundUsableErrorType = true;
                    return type;

                case SymbolKind.DynamicType:
                case SymbolKind.TypeParameter:
                    return type;

                case SymbolKind.FunctionPointerType:
                    return DecodeFunctionPointerType((FunctionPointerTypeSymbol)type);

                case SymbolKind.PointerType:
                    return DecodePointerType((PointerTypeSymbol)type);

                case SymbolKind.NamedType:
                    // We may have a tuple type from a substituted type symbol,
                    // but it will be missing names from metadata, so we'll
                    // need to re-create the type.
                    //
                    // Consider the declaration
                    //
                    //      class C : BaseType<(int x, int y)>
                    //
                    // The process for decoding tuples in C looks at the BaseType, calls
                    // DecodeOrThrow, then passes the decoded type to the TupleTypeDecoder.
                    // However, DecodeOrThrow uses the AbstractTypeMap to construct a
                    // SubstitutedTypeSymbol, which eagerly converts tuple-compatible
                    // types to TupleTypeSymbols. Thus, by the time we get to the Decoder
                    // all metadata instances of System.ValueTuple will have been
                    //  replaced with TupleTypeSymbols without names.
                    // 
                    // Rather than fixing up after-the-fact it's possible that we could
                    // flow up a SubstituteWith/Without tuple unification to the top level
                    // of the type map and change DecodeOrThrow to call into the substitution
                    // without unification instead.
                    return DecodeNamedType((NamedTypeSymbol)type);

                case SymbolKind.ArrayType:
                    return DecodeArrayType((ArrayTypeSymbol)type);

                default:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
            }
        }

        private PointerTypeSymbol DecodePointerType(PointerTypeSymbol type)
        {
            return type.WithPointedAtType(DecodeTypeInternal(type.PointedAtTypeWithAnnotations));
        }

        private FunctionPointerTypeSymbol DecodeFunctionPointerType(FunctionPointerTypeSymbol type)
        {
            var parameterTypes = ImmutableArray<TypeWithAnnotations>.Empty;
            var paramsModified = false;

            if (type.Signature.ParameterCount > 0)
            {
                var paramsBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(type.Signature.ParameterCount);

                for (int i = type.Signature.ParameterCount - 1; i >= 0; i--)
                {
                    var param = type.Signature.Parameters[i];
                    var decodedParam = DecodeTypeInternal(param.TypeWithAnnotations);
                    paramsModified = paramsModified || !decodedParam.IsSameAs(param.TypeWithAnnotations);
                    paramsBuilder.Add(decodedParam);
                }

                if (paramsModified)
                {
                    paramsBuilder.ReverseContents();
                    parameterTypes = paramsBuilder.ToImmutableAndFree();
                }
                else
                {
                    parameterTypes = type.Signature.ParameterTypesWithAnnotations;
                    paramsBuilder.Free();
                }
            }

            var decodedReturnType = DecodeTypeInternal(type.Signature.ReturnTypeWithAnnotations);

            if (paramsModified || !decodedReturnType.IsSameAs(type.Signature.ReturnTypeWithAnnotations))
            {
                return type.SubstituteTypeSymbol(decodedReturnType, parameterTypes, refCustomModifiers: default, paramRefCustomModifiers: default);
            }
            else
            {
                return type;
            }
        }

        private NamedTypeSymbol DecodeNamedType(NamedTypeSymbol type)
        {
            // First decode the type arguments
            var typeArgs = type.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
            var decodedArgs = DecodeTypeArguments(typeArgs);

            NamedTypeSymbol decodedType = type;

            // Now check the container
            NamedTypeSymbol containingType = type.ContainingType;
            NamedTypeSymbol? decodedContainingType;
            if (containingType is object && containingType.IsGenericType)
            {
                decodedContainingType = DecodeNamedType(containingType);
                Debug.Assert(decodedContainingType.IsGenericType);
            }
            else
            {
                decodedContainingType = containingType;
            }

            // Replace the type if necessary
            var containerChanged = !ReferenceEquals(decodedContainingType, containingType);
            var typeArgsChanged = typeArgs != decodedArgs;
            if (typeArgsChanged || containerChanged)
            {
                if (containerChanged)
                {
                    decodedType = decodedType.OriginalDefinition.AsMember(decodedContainingType);
                    // If the type is nested, e.g. Outer<T>.Inner<V>, then Inner is definitely
                    // not a tuple, since we know all tuple-compatible types (System.ValueTuple)
                    // are not nested types. Thus, it is safe to return without checking if
                    // Inner is a tuple.
                    return decodedType.ConstructIfGeneric(decodedArgs);
                }

                decodedType = type.ConstructedFrom.Construct(decodedArgs, unbound: false);
            }

            // Now decode into a tuple, if it is one
            if (decodedType.IsTupleType)
            {
                int tupleCardinality = decodedType.TupleElementTypesWithAnnotations.Length;
                if (tupleCardinality > 0)
                {
                    var elementNames = EatElementNamesIfAvailable(tupleCardinality);

                    Debug.Assert(elementNames.IsDefault || elementNames.Length == tupleCardinality);

                    decodedType = NamedTypeSymbol.CreateTuple(decodedType, elementNames);
                }
            }

            return decodedType;
        }

        private ImmutableArray<TypeWithAnnotations> DecodeTypeArguments(ImmutableArray<TypeWithAnnotations> typeArgs)
        {
            if (typeArgs.IsEmpty)
            {
                return typeArgs;
            }

            var decodedArgs = ArrayBuilder<TypeWithAnnotations>.GetInstance(typeArgs.Length);
            var anyDecoded = false;
            // Visit the type arguments in reverse
            for (int i = typeArgs.Length - 1; i >= 0; i--)
            {
                TypeWithAnnotations typeArg = typeArgs[i];
                TypeWithAnnotations decoded = DecodeTypeInternal(typeArg);
                anyDecoded |= !decoded.IsSameAs(typeArg);
                decodedArgs.Add(decoded);
            }

            if (!anyDecoded)
            {
                decodedArgs.Free();
                return typeArgs;
            }

            decodedArgs.ReverseContents();
            return decodedArgs.ToImmutableAndFree();
        }

        private ArrayTypeSymbol DecodeArrayType(ArrayTypeSymbol type)
        {
            TypeWithAnnotations decodedElementType = DecodeTypeInternal(type.ElementTypeWithAnnotations);
            return type.WithElementType(decodedElementType);
        }

        private TypeWithAnnotations DecodeTypeInternal(TypeWithAnnotations typeWithAnnotations)
        {
            TypeSymbol type = typeWithAnnotations.Type;
            TypeSymbol decoded = DecodeType(type);
            return ReferenceEquals(decoded, type) ?
                typeWithAnnotations :
                TypeWithAnnotations.Create(decoded, typeWithAnnotations.NullableAnnotation, typeWithAnnotations.CustomModifiers);
        }

        private ImmutableArray<string?> EatElementNamesIfAvailable(int numberOfElements)
        {
            Debug.Assert(numberOfElements > 0);

            // If we don't have any element names there's nothing to eat
            if (_elementNames.IsDefault)
            {
                return _elementNames;
            }

            // We've gone past the end of the names -- bad metadata
            if (numberOfElements > _namesIndex)
            {
                // We'll want to continue decoding without consuming more names to see if there are any error types
                _namesIndex = 0;
                _decodingFailed = true;
                return default;
            }

            // Check to see if all the elements are null
            var start = _namesIndex - numberOfElements;
            _namesIndex = start;
            bool allNull = true;

            for (int i = 0; i < numberOfElements; i++)
            {
                if (_elementNames[start + i] != null)
                {
                    allNull = false;
                    break;
                }
            }

            if (allNull)
            {
                return default;
            }

            return ImmutableArray.Create(_elementNames, start, numberOfElements);
        }
    }
}
