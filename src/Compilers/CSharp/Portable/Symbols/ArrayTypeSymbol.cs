// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// An ArrayTypeSymbol represents an array type, such as int[] or object[,].
    /// </summary>
    internal abstract partial class ArrayTypeSymbol : TypeSymbol, IArrayTypeSymbolInternal
    {
        private readonly TypeWithAnnotations _elementTypeWithAnnotations;
        private readonly NamedTypeSymbol _baseType;

        private ArrayTypeSymbol(
            TypeWithAnnotations elementTypeWithAnnotations,
            NamedTypeSymbol array)
        {
            Debug.Assert(elementTypeWithAnnotations.HasType);
            RoslynDebug.Assert((object)array != null);

            _elementTypeWithAnnotations = elementTypeWithAnnotations;
            _baseType = array;
        }

        internal static ArrayTypeSymbol CreateCSharpArray(
            AssemblySymbol declaringAssembly,
            TypeWithAnnotations elementTypeWithAnnotations,
            int rank = 1)
        {
            if (rank == 1)
            {
                return CreateSZArray(declaringAssembly, elementTypeWithAnnotations);
            }

            return CreateMDArray(declaringAssembly, elementTypeWithAnnotations, rank, default(ImmutableArray<int>), default(ImmutableArray<int>));
        }

        internal static ArrayTypeSymbol CreateMDArray(
            TypeWithAnnotations elementTypeWithAnnotations,
            int rank,
            ImmutableArray<int> sizes,
            ImmutableArray<int> lowerBounds,
            NamedTypeSymbol array)
        {
            // Optimize for most common case - no sizes and all dimensions are zero lower bound.
            if (sizes.IsDefaultOrEmpty && lowerBounds.IsDefault)
            {
                return new MDArrayNoSizesOrBounds(elementTypeWithAnnotations, rank, array);
            }

            return new MDArrayWithSizesAndBounds(elementTypeWithAnnotations, rank, sizes, lowerBounds, array);
        }

        internal static ArrayTypeSymbol CreateMDArray(
            AssemblySymbol declaringAssembly,
            TypeWithAnnotations elementType,
            int rank,
            ImmutableArray<int> sizes,
            ImmutableArray<int> lowerBounds)
        {
            return CreateMDArray(elementType, rank, sizes, lowerBounds, declaringAssembly.GetSpecialType(SpecialType.System_Array));
        }

        internal static ArrayTypeSymbol CreateSZArray(
            TypeWithAnnotations elementTypeWithAnnotations,
            NamedTypeSymbol array)
        {
            return new SZArray(elementTypeWithAnnotations, array, GetSZArrayInterfaces(elementTypeWithAnnotations, array.ContainingAssembly));
        }

        internal static ArrayTypeSymbol CreateSZArray(
            TypeWithAnnotations elementTypeWithAnnotations,
            NamedTypeSymbol array,
            ImmutableArray<NamedTypeSymbol> constructedInterfaces)
        {
            return new SZArray(elementTypeWithAnnotations, array, constructedInterfaces);
        }

        internal static ArrayTypeSymbol CreateSZArray(
            AssemblySymbol declaringAssembly,
            TypeWithAnnotations elementType)
        {
            return CreateSZArray(elementType, declaringAssembly.GetSpecialType(SpecialType.System_Array), GetSZArrayInterfaces(elementType, declaringAssembly));
        }

        internal ArrayTypeSymbol WithElementType(TypeWithAnnotations elementTypeWithAnnotations)
        {
            return ElementTypeWithAnnotations.IsSameAs(elementTypeWithAnnotations) ? this : WithElementTypeCore(elementTypeWithAnnotations);
        }

        protected abstract ArrayTypeSymbol WithElementTypeCore(TypeWithAnnotations elementTypeWithAnnotations);

        private static ImmutableArray<NamedTypeSymbol> GetSZArrayInterfaces(
            TypeWithAnnotations elementTypeWithAnnotations,
            AssemblySymbol declaringAssembly)
        {
            var constructedInterfaces = ArrayBuilder<NamedTypeSymbol>.GetInstance();

            //There are cases where the platform does contain the interfaces.
            //So it is fine not to have them listed under the type
            var iListOfT = declaringAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IList_T);
            if (!iListOfT.IsErrorType())
            {
                constructedInterfaces.Add(new ConstructedNamedTypeSymbol(iListOfT, ImmutableArray.Create(elementTypeWithAnnotations)));
            }

            var iReadOnlyListOfT = declaringAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyList_T);

            if (!iReadOnlyListOfT.IsErrorType())
            {
                constructedInterfaces.Add(new ConstructedNamedTypeSymbol(iReadOnlyListOfT, ImmutableArray.Create(elementTypeWithAnnotations)));
            }

            return constructedInterfaces.ToImmutableAndFree();
        }

        /// <summary>
        /// Gets the number of dimensions of the array. A regular single-dimensional array
        /// has rank 1, a two-dimensional array has rank 2, etc.
        /// </summary>
        public abstract int Rank { get; }

        /// <summary>
        /// Is this a zero-based one-dimensional array, i.e. SZArray in CLR terms.
        /// </summary>
        public abstract bool IsSZArray { get; }

        internal bool HasSameShapeAs(ArrayTypeSymbol other)
        {
            return Rank == other.Rank && IsSZArray == other.IsSZArray;
        }

        /// <summary>
        /// Specified sizes for dimensions, by position. The length can be less than <see cref="Rank"/>,
        /// meaning that some trailing dimensions don't have the size specified.
        /// The most common case is none of the dimensions have the size specified - an empty array is returned.
        /// </summary>
        public virtual ImmutableArray<int> Sizes
        {
            get
            {
                return ImmutableArray<int>.Empty;
            }
        }

        /// <summary>
        /// Specified lower bounds for dimensions, by position. The length can be less than <see cref="Rank"/>,
        /// meaning that some trailing dimensions don't have the lower bound specified.
        /// The most common case is all dimensions are zero bound - a default array is returned in this case.
        /// </summary>
        public virtual ImmutableArray<int> LowerBounds
        {
            get
            {
                return default(ImmutableArray<int>);
            }
        }

        /// <summary>
        /// Note, <see cref="Rank"/> equality should be checked separately!!!
        /// </summary>
        internal bool HasSameSizesAndLowerBoundsAs(ArrayTypeSymbol other)
        {
            if (this.Sizes.SequenceEqual(other.Sizes))
            {
                var thisLowerBounds = this.LowerBounds;

                if (thisLowerBounds.IsDefault)
                {
                    return other.LowerBounds.IsDefault;
                }

                var otherLowerBounds = other.LowerBounds;

                return !otherLowerBounds.IsDefault && thisLowerBounds.SequenceEqual(otherLowerBounds);
            }

            return false;
        }

        /// <summary>
        /// Normally C# arrays have default sizes and lower bounds - sizes are not specified and all dimensions are zero bound.
        /// This property should return false for any deviations.
        /// </summary>
        internal abstract bool HasDefaultSizesAndLowerBounds { get; }

        /// <summary>
        /// Gets the type of the elements stored in the array along with its annotations.
        /// </summary>
        public TypeWithAnnotations ElementTypeWithAnnotations
        {
            get
            {
                return _elementTypeWithAnnotations;
            }
        }

        /// <summary>
        /// Gets the type of the elements stored in the array.
        /// </summary>
        public TypeSymbol ElementType
        {
            get
            {
                return _elementTypeWithAnnotations.Type;
            }
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => _baseType;

        public override bool IsReferenceType
        {
            get
            {
                return true;
            }
        }

        public override bool IsValueType
        {
            get
            {
                return false;
            }
        }

        internal sealed override ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo) => ManagedKind.Managed;

        public sealed override bool IsRefLikeType
        {
            get
            {
                return false;
            }
        }

        public sealed override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        internal sealed override ObsoleteAttributeData? ObsoleteAttributeData
        {
            get { return null; }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return ImmutableArray<Symbol>.Empty;
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return ImmutableArray<Symbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.ArrayType;
            }
        }

        public override TypeKind TypeKind
        {
            get
            {
                return TypeKind.Array;
            }
        }

        public override Symbol? ContainingSymbol
        {
            get
            {
                return null;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitArrayType(this, argument);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitArrayType(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitArrayType(this);
        }

        internal override bool Equals(TypeSymbol? t2, TypeCompareKind comparison)
        {
            return this.Equals(t2 as ArrayTypeSymbol, comparison);
        }

        private bool Equals(ArrayTypeSymbol? other, TypeCompareKind comparison)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if ((object?)other == null || !other.HasSameShapeAs(this) ||
                !other.ElementTypeWithAnnotations.Equals(ElementTypeWithAnnotations, comparison))
            {
                return false;
            }

            // Make sure bounds are the same.
            if ((comparison & TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) == 0 && !this.HasSameSizesAndLowerBoundsAs(other))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            // We don't want to blow the stack if we have a type like T[][][][][][][][]....[][],
            // so we do not recurse until we have a non-array. Rather, hash all the ranks together
            // and then hash that with the "T" type.

            int hash = 0;
            TypeSymbol current = this;
            while (current.TypeKind == TypeKind.Array)
            {
                var cur = (ArrayTypeSymbol)current;
                hash = Hash.Combine(cur.Rank, hash);
                current = cur.ElementType;
            }

            return Hash.Combine(current, hash);
        }

        internal override void AddNullableTransforms(ArrayBuilder<byte> transforms)
        {
            ElementTypeWithAnnotations.AddNullableTransforms(transforms);
        }

        internal override bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbol result)
        {
            TypeWithAnnotations oldElementType = ElementTypeWithAnnotations;
            TypeWithAnnotations newElementType;

            if (!oldElementType.ApplyNullableTransforms(defaultTransformFlag, transforms, ref position, out newElementType))
            {
                result = this;
                return false;
            }

            result = WithElementType(newElementType);
            return true;
        }

        internal override TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform)
        {
            return WithElementType(transform(ElementTypeWithAnnotations));
        }

        internal override TypeSymbol MergeEquivalentTypes(TypeSymbol other, VarianceKind variance)
        {
            Debug.Assert(this.Equals(other, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
            TypeWithAnnotations elementType = ElementTypeWithAnnotations.MergeEquivalentTypes(((ArrayTypeSymbol)other).ElementTypeWithAnnotations, variance);
            return WithElementType(elementType);
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.NotApplicable;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return false;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        #region Use-Site Diagnostics

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            UseSiteInfo<AssemblySymbol> result = default;

            // check element type
            // check custom modifiers
            DeriveUseSiteInfoFromType(ref result, this.ElementTypeWithAnnotations, AllowedRequiredModifierType.None);

            return result;
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return _elementTypeWithAnnotations.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes) ||
                   ((object)_baseType != null && _baseType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes)) ||
                   GetUnificationUseSiteDiagnosticRecursive(ref result, this.InterfacesNoUseSiteDiagnostics(), owner, ref checkedTypes);
        }

        #endregion

        protected sealed override ISymbol CreateISymbol()
        {
            return new PublicModel.ArrayTypeSymbol(this, DefaultNullableAnnotation);
        }

        protected sealed override ITypeSymbol CreateITypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            Debug.Assert(nullableAnnotation != DefaultNullableAnnotation);
            return new PublicModel.ArrayTypeSymbol(this, nullableAnnotation);
        }

        internal override bool IsRecord => false;

        internal override bool IsRecordStruct => false;

        internal sealed override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            return SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
        }

        internal sealed override bool HasInlineArrayAttribute(out int length)
        {
            length = 0;
            return false;
        }

        #region IArrayTypeSymbolInternal

        ITypeSymbolInternal IArrayTypeSymbolInternal.ElementType => ElementType;

        #endregion

        /// <summary>
        /// Represents SZARRAY - zero-based one-dimensional array 
        /// </summary>
        private sealed class SZArray : ArrayTypeSymbol
        {
            private readonly ImmutableArray<NamedTypeSymbol> _interfaces;

            internal SZArray(
                TypeWithAnnotations elementTypeWithAnnotations,
                NamedTypeSymbol array,
                ImmutableArray<NamedTypeSymbol> constructedInterfaces)
                : base(elementTypeWithAnnotations, array)
            {
                Debug.Assert(constructedInterfaces.Length <= 2);
                _interfaces = constructedInterfaces;
            }

            protected override ArrayTypeSymbol WithElementTypeCore(TypeWithAnnotations newElementType)
            {
                var newInterfaces = _interfaces.SelectAsArray((i, t) => i.OriginalDefinition.Construct(t), newElementType.Type);
                return new SZArray(newElementType, BaseTypeNoUseSiteDiagnostics, newInterfaces);
            }

            public override int Rank
            {
                get
                {
                    return 1;
                }
            }

            /// <summary>
            /// SZArray is an array type encoded in metadata with ELEMENT_TYPE_SZARRAY (always single-dim array with 0 lower bound).
            /// Non-SZArray type is encoded in metadata with ELEMENT_TYPE_ARRAY and with optional sizes and lower bounds. Even though 
            /// non-SZArray can also be a single-dim array with 0 lower bound, the encoding of these types in metadata is distinct.
            /// </summary>
            public override bool IsSZArray
            {
                get
                {
                    return true;
                }
            }

            internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null)
            {
                return _interfaces;
            }

            internal override bool HasDefaultSizesAndLowerBounds
            {
                get
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Represents MDARRAY - multi-dimensional array (possibly of rank 1)
        /// </summary>
        private abstract class MDArray : ArrayTypeSymbol
        {
            private readonly int _rank;

            internal MDArray(
                TypeWithAnnotations elementTypeWithAnnotations,
                int rank,
                NamedTypeSymbol array)
                : base(elementTypeWithAnnotations, array)
            {
                Debug.Assert(rank >= 1);
                _rank = rank;
            }

            public sealed override int Rank
            {
                get
                {
                    return _rank;
                }
            }

            public sealed override bool IsSZArray
            {
                get
                {
                    return false;
                }
            }

            internal sealed override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }
        }

        private sealed class MDArrayNoSizesOrBounds : MDArray
        {
            internal MDArrayNoSizesOrBounds(
                TypeWithAnnotations elementTypeWithAnnotations,
                int rank,
                NamedTypeSymbol array)
                : base(elementTypeWithAnnotations, rank, array)
            {
            }

            protected override ArrayTypeSymbol WithElementTypeCore(TypeWithAnnotations elementTypeWithAnnotations)
            {
                return new MDArrayNoSizesOrBounds(elementTypeWithAnnotations, Rank, BaseTypeNoUseSiteDiagnostics);
            }

            internal override bool HasDefaultSizesAndLowerBounds
            {
                get
                {
                    return true;
                }
            }
        }

        private sealed class MDArrayWithSizesAndBounds : MDArray
        {
            private readonly ImmutableArray<int> _sizes;
            private readonly ImmutableArray<int> _lowerBounds;

            internal MDArrayWithSizesAndBounds(
                TypeWithAnnotations elementTypeWithAnnotations,
                int rank,
                ImmutableArray<int> sizes,
                ImmutableArray<int> lowerBounds,
                NamedTypeSymbol array)
                : base(elementTypeWithAnnotations, rank, array)
            {
                Debug.Assert(!sizes.IsDefaultOrEmpty || !lowerBounds.IsDefault);
                Debug.Assert(lowerBounds.IsDefaultOrEmpty || (!lowerBounds.IsEmpty && (lowerBounds.Length != rank || !lowerBounds.All(b => b == 0))));
                _sizes = sizes.NullToEmpty();
                _lowerBounds = lowerBounds;
            }

            protected override ArrayTypeSymbol WithElementTypeCore(TypeWithAnnotations elementTypeWithAnnotations)
            {
                return new MDArrayWithSizesAndBounds(elementTypeWithAnnotations, Rank, _sizes, _lowerBounds, BaseTypeNoUseSiteDiagnostics);
            }

            public override ImmutableArray<int> Sizes
            {
                get
                {
                    return _sizes;
                }
            }

            public override ImmutableArray<int> LowerBounds
            {
                get
                {
                    return _lowerBounds;
                }
            }

            internal override bool HasDefaultSizesAndLowerBounds
            {
                get
                {
                    return false;
                }
            }
        }
    }
}
