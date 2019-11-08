// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    /// a <see cref="TupleTypeSymbol"/> with attached names.
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
        private readonly ImmutableArray<string> _elementNames;
        // Keep track of how many names we've "used" during decoding. Starts at
        // the back of the array and moves forward.
        private int _namesIndex;

        private TupleTypeDecoder(ImmutableArray<string> elementNames)
        {
            _elementNames = elementNames;
            _namesIndex = elementNames.IsDefault ? 0 : elementNames.Length;
        }

        public static TypeSymbol DecodeTupleTypesIfApplicable(
            TypeSymbol metadataType,
            EntityHandle targetHandle,
            PEModuleSymbol containingModule)
        {
            ImmutableArray<string> elementNames;
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
            ImmutableArray<string> elementNames;
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
            ImmutableArray<string> elementNames)
        {
            return DecodeTupleTypesInternal(metadataType, elementNames, hasTupleElementNamesAttribute: !elementNames.IsDefaultOrEmpty);
        }

        private static TypeSymbol DecodeTupleTypesInternal(TypeSymbol metadataType, ImmutableArray<string> elementNames, bool hasTupleElementNamesAttribute)
        {
            Debug.Assert((object)metadataType != null);

            var decoder = new TupleTypeDecoder(elementNames);
            try
            {
                var decoded = decoder.DecodeType(metadataType);
                // If not all of the names have been used, the metadata is bad
                if (!hasTupleElementNamesAttribute ||
                    decoder._namesIndex == 0)
                {
                    return decoded;
                }
            }
            catch (InvalidOperationException)
            {
                // Indicates that the tuple info in the attribute didn't match
                // the type. Bad metadata.
            }

            if (metadataType.HasUseSiteError)
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
                case SymbolKind.DynamicType:
                case SymbolKind.TypeParameter:
                case SymbolKind.PointerType:
                    return type;

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
                    return DecodeNamedType(type.IsTupleType ? type.TupleUnderlyingType : (NamedTypeSymbol)type);

                case SymbolKind.ArrayType:
                    return DecodeArrayType((ArrayTypeSymbol)type);

                default:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
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
            NamedTypeSymbol decodedContainingType;
            if (containingType is object { IsGenericType: true })
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
            int tupleCardinality;
            if (decodedType.IsTupleCompatible(out tupleCardinality))
            {
                var elementNames = EatElementNamesIfAvailable(tupleCardinality);

                Debug.Assert(elementNames.IsDefault || elementNames.Length == tupleCardinality);

                decodedType = TupleTypeSymbol.Create(decodedType, elementNames);
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

        private ImmutableArray<string> EatElementNamesIfAvailable(int numberOfElements)
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
                throw new InvalidOperationException();
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
                return default(ImmutableArray<string>);
            }

            var builder = ArrayBuilder<string>.GetInstance(numberOfElements);

            for (int i = 0; i < numberOfElements; i++)
            {
                builder.Add(_elementNames[start + i]);
            }

            return builder.ToImmutableAndFree();
        }
    }
}
