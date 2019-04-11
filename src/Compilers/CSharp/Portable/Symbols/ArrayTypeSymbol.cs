// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// An ArrayTypeSymbol represents an array type, such as int[] or object[,].
    /// </summary>
    internal abstract partial class ArrayTypeSymbol : TypeSymbol, IArrayTypeSymbol
    {
        private readonly TypeWithAnnotations _elementTypeWithAnnotations;
        private readonly NamedTypeSymbol _baseType;

        private ArrayTypeSymbol(
            TypeWithAnnotations elementTypeWithAnnotations,
            NamedTypeSymbol array)
        {
            Debug.Assert(elementTypeWithAnnotations.HasType);
            Debug.Assert((object)array != null);

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

        internal sealed override ManagedKind ManagedKind => ManagedKind.Managed;

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

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
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

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
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

        public override Symbol ContainingSymbol
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

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison)
        {
            return this.Equals(t2 as ArrayTypeSymbol, comparison);
        }

        internal bool Equals(ArrayTypeSymbol other)
        {
            return Equals(other, TypeCompareKind.ConsiderEverything);
        }

        private bool Equals(ArrayTypeSymbol other, TypeCompareKind comparison)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // We don't want to blow the stack if we have a type like T[][][][][][][][]....[][],
            // so we do not recurse until we have a non-array. Rather, hash all the ranks together
            // and then hash that with the "T" type.
            var array = this;
            do
            {
                if (other is null || !other.HasSameShapeAs(array))
                {
                    return false;
                }

                // Make sure bounds are the same
                if ((comparison & TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) == 0 && !array.HasSameSizesAndLowerBoundsAs(other))
                {
                    return false;
                }

                var arrayElementType = array.ElementTypeWithAnnotations;
                var otherElementType = other.ElementTypeWithAnnotations;
                if (arrayElementType.Type is ArrayTypeSymbol nextArray)
                {
                    // Compare everything but the actual ArrayTypeSymbol instance. 
                    var otherTwa = TypeWithAnnotations.Create(arrayElementType.Type, otherElementType.NullableAnnotation, otherElementType.CustomModifiers);
                    if (!arrayElementType.Equals(otherTwa, comparison))
                    {
                        return false;
                    }

                    array = nextArray;
                    other = otherElementType.Type as ArrayTypeSymbol;
                }
                else
                {
                    return arrayElementType.Equals(otherElementType, comparison);
                }
            }
            while (true);
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
            var elementType = ElementTypeWithAnnotations;
            while (elementType.Type is ArrayTypeSymbol arrayType)
            {
                elementType.AddNullableTransformShallow(transforms);
                elementType = arrayType.ElementTypeWithAnnotations;
            }

            elementType.AddNullableTransforms(transforms);
        }

        internal override TypeSymbol ApplyNullableTransforms(NullableTransformStream stream) =>
            TransformNullable(ApplyTransformNullableTransform.Instance, stream);

        internal override TypeSymbol SetObliviousNullabilityForReferenceTypes() =>
            TransformNullable(SetObliviousNullableTransform.Instance, null);

        internal override TypeSymbol MergeNullability(TypeSymbol other, VarianceKind variance)
        {
            Debug.Assert(this.Equals(other, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
            return TransformNullable(MergeNullableTransform.Instance, ((ArrayTypeSymbol)other, variance));
        }

        private ArrayTypeSymbol TransformNullable<TState, TInitialState>(NullableTransform<TState, TInitialState> transform, TInitialState initialState)
        {
            // The language supports deeply nested arrays, up to 10,000+ instances. This means we can't implemented a head
            // recursive solution here. Need to take an iterative approach to building up the annotations here.
            var builder = ArrayBuilder<TState>.GetInstance();

            // Dig through the element types to get the set of ArrayTypeSymbol instances that need to be marked
            // as oblivious
            var array = this;
            var state = transform.GetInitialState(initialState, this);
            builder.Push(state);
            do
            {
                array = array.ElementType as ArrayTypeSymbol;
                if (array is object)
                {
                    state = transform.GetState(state, array);
                    builder.Push(state);
                }
            }
            while (array is object);

            // Fixup the element type on the most nested array
            var returnValue = transform.CreateInitialReturnValue(builder.Pop());

            // All but the most nested array instance is just an oblivious array with the same custom modifiers
            while (builder.Count > 0)
            {
                returnValue = transform.GetReturnValue(returnValue, builder.Pop());
            }

            builder.Free();
            return returnValue;
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

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            DiagnosticInfo result = null;

            // check element type
            // check custom modifiers
            if (DeriveUseSiteDiagnosticFromType(ref result, this.ElementTypeWithAnnotations))
            {
                return result;
            }

            return result;
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return _elementTypeWithAnnotations.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes) ||
                   ((object)_baseType != null && _baseType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes)) ||
                   GetUnificationUseSiteDiagnosticRecursive(ref result, this.InterfacesNoUseSiteDiagnostics(), owner, ref checkedTypes);
        }

        #endregion

        #region IArrayTypeSymbol Members

        ITypeSymbol IArrayTypeSymbol.ElementType
        {
            get { return this.ElementType; }
        }

        ImmutableArray<CustomModifier> IArrayTypeSymbol.CustomModifiers
        {
            get { return this.ElementTypeWithAnnotations.CustomModifiers; }
        }

        bool IArrayTypeSymbol.Equals(IArrayTypeSymbol symbol)
        {
            return this.Equals(symbol as ArrayTypeSymbol);
        }

        #endregion

        #region ISymbol Members

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitArrayType(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitArrayType(this);
        }

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

            internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved = null)
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

            internal sealed override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved = null)
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

        private abstract class NullableTransform<TState, TInitialState>
        {
            internal abstract TState GetInitialState(TInitialState initialState, ArrayTypeSymbol array);
            internal abstract TState GetState(TState outerState, ArrayTypeSymbol currentArray);
            internal abstract ArrayTypeSymbol CreateInitialReturnValue(TState mostNestedState);
            internal abstract ArrayTypeSymbol GetReturnValue(ArrayTypeSymbol oldReturn, TState state);
        }

        private sealed class MergeNullableTransform : NullableTransform<
            (ArrayTypeSymbol thisArray, ArrayTypeSymbol otherArray, VarianceKind varianceKind),
            (ArrayTypeSymbol other, VarianceKind varianceKind)>
        {
            internal static readonly MergeNullableTransform Instance = new MergeNullableTransform();

            internal override (ArrayTypeSymbol thisArray, ArrayTypeSymbol otherArray, VarianceKind varianceKind) GetInitialState(
                (ArrayTypeSymbol other, VarianceKind varianceKind) initialState,
                ArrayTypeSymbol array) => (array, initialState.other, initialState.varianceKind);

            internal override (ArrayTypeSymbol thisArray, ArrayTypeSymbol otherArray, VarianceKind varianceKind) GetState(
                (ArrayTypeSymbol thisArray, ArrayTypeSymbol otherArray, VarianceKind varianceKind) outerState,
                ArrayTypeSymbol currentArray)
            {
                var otherArray = (ArrayTypeSymbol)(outerState.otherArray.ElementType);
                return (currentArray, otherArray, outerState.varianceKind);
            }

            internal override ArrayTypeSymbol CreateInitialReturnValue((ArrayTypeSymbol thisArray, ArrayTypeSymbol otherArray, VarianceKind varianceKind) mostNestedState)
            {
                var elementType = mostNestedState.thisArray.ElementTypeWithAnnotations.MergeNullability(
                    mostNestedState.otherArray.ElementTypeWithAnnotations,
                    mostNestedState.varianceKind);
                return mostNestedState.thisArray.WithElementType(elementType);
            }

            internal override ArrayTypeSymbol GetReturnValue(ArrayTypeSymbol previousArray, (ArrayTypeSymbol thisArray, ArrayTypeSymbol otherArray, VarianceKind varianceKind) state)
            {
                var nullableAnnotation = NullableAnnotationExtensions.MergeNullableAnnotation(
                    state.thisArray.ElementTypeWithAnnotations.NullableAnnotation,
                    state.otherArray.ElementTypeWithAnnotations.NullableAnnotation,
                    state.varianceKind);
                var elementTypeWithAnnotations = TypeWithAnnotations.Create(previousArray, nullableAnnotation, state.thisArray.ElementTypeWithAnnotations.CustomModifiers);
                return state.thisArray.WithElementType(elementTypeWithAnnotations);
            }
        }

        private sealed class SetObliviousNullableTransform : NullableTransform<ArrayTypeSymbol, object>
        {
            internal static readonly SetObliviousNullableTransform Instance = new SetObliviousNullableTransform();

            internal override ArrayTypeSymbol GetInitialState(object initialState, ArrayTypeSymbol array) => array;

            internal override ArrayTypeSymbol GetState(ArrayTypeSymbol _, ArrayTypeSymbol currentArray) => currentArray;

            internal override ArrayTypeSymbol CreateInitialReturnValue(ArrayTypeSymbol mostNestedState)
            {
                var elementType = mostNestedState.ElementTypeWithAnnotations.SetObliviousNullabilityForReferenceTypes();
                return mostNestedState.WithElementType(elementType);
            }

            internal override ArrayTypeSymbol GetReturnValue(ArrayTypeSymbol previousReturnValue, ArrayTypeSymbol state)
            {
                var elementTypeWithAnnotations = TypeWithAnnotations.Create(previousReturnValue, NullableAnnotation.Oblivious, state.ElementTypeWithAnnotations.CustomModifiers);
                return state.WithElementType(elementTypeWithAnnotations);
            }
        }

        private sealed class ApplyTransformNullableTransform : NullableTransform<
            (ArrayTypeSymbol arrayType, byte? elementTransformFlag, NullableTransformStream stream),
            NullableTransformStream>
        {
            internal static readonly ApplyTransformNullableTransform Instance = new ApplyTransformNullableTransform();
            internal override (ArrayTypeSymbol arrayType, byte? elementTransformFlag, NullableTransformStream stream) GetInitialState(NullableTransformStream initialState, ArrayTypeSymbol array) =>
                (array, initialState.GetNextTransform(), initialState);

            internal override (ArrayTypeSymbol arrayType, byte? elementTransformFlag, NullableTransformStream stream) GetState(
                (ArrayTypeSymbol arrayType, byte? elementTransformFlag, NullableTransformStream stream) outerState,
                ArrayTypeSymbol currentArray) =>
                (currentArray, outerState.stream.GetNextTransform(), outerState.stream);

            internal override ArrayTypeSymbol CreateInitialReturnValue((ArrayTypeSymbol arrayType, byte? elementTransformFlag, NullableTransformStream stream) mostNestedState)
            {
                var arrayType = mostNestedState.arrayType;
                var transformFlag = mostNestedState.elementTransformFlag;
                var stream = mostNestedState.stream;
                if (!transformFlag.HasValue ||
                    !(arrayType.ElementTypeWithAnnotations.ApplyNullableTransformShallow(transformFlag.Value) is TypeWithAnnotations newElementType))
                {
                    stream.SetHasInsufficientData();
                    return null;
                }

                return arrayType.WithElementType(newElementType);
            }

            internal override ArrayTypeSymbol GetReturnValue(
                ArrayTypeSymbol oldReturn,
                (ArrayTypeSymbol arrayType, byte? elementTransformFlag, NullableTransformStream stream) state)
            {
                var elementType = state.arrayType.ElementTypeWithAnnotations;
                elementType = TypeWithAnnotations.Create(oldReturn, elementType.NullableAnnotation, elementType.CustomModifiers);

                if (!state.elementTransformFlag.HasValue)
                {
                    Debug.Assert(state.stream.HasInsufficientData);
                    return state.arrayType.WithElementType(elementType);
                }

                var result = elementType.ApplyNullableTransformShallow(state.elementTransformFlag.Value);
                if (!result.HasValue)
                {
                    Debug.Assert(state.stream.HasInsufficientData);
                    return state.arrayType.WithElementType(elementType);
                }

                return state.arrayType.WithElementType(result.Value);
            }
        }
    }
}
