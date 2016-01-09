// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// An ArrayTypeSymbol represents an array type, such as int[] or object[,].
    /// </summary>
    internal abstract partial class ArrayTypeSymbol : TypeSymbol, IArrayTypeSymbol
    {
        private readonly TypeSymbolWithAnnotations _elementType;
        private readonly NamedTypeSymbol _baseType;

        private ArrayTypeSymbol(
            TypeSymbolWithAnnotations elementType,
            NamedTypeSymbol array)
        {
            Debug.Assert((object)elementType != null);
            Debug.Assert((object)array != null);

            _elementType = elementType;
            _baseType = array;
        }

        internal static ArrayTypeSymbol CreateCSharpArray(
            AssemblySymbol declaringAssembly,
            TypeSymbolWithAnnotations elementType,
            int rank = 1)
        {
            if (rank == 1)
            {
                return CreateSZArray(declaringAssembly, elementType);
            }

            return CreateMDArray(declaringAssembly, elementType, rank, default(ImmutableArray<int>), default(ImmutableArray<int>));
        }

        internal static ArrayTypeSymbol CreateMDArray(
            TypeSymbolWithAnnotations elementType,
            int rank,
            ImmutableArray<int> sizes,
            ImmutableArray<int> lowerBounds,
            NamedTypeSymbol array)
        {
            // Optimize for most common case - no sizes and all dimensions are zero lower bound.
            if (sizes.IsDefaultOrEmpty && lowerBounds.IsDefault)
            {
                return new MDArray(elementType, rank, array);
            }

            return new MDArrayWithSizesAndBounds(elementType, rank, sizes, lowerBounds, array);
        }

        internal static ArrayTypeSymbol CreateMDArray(
            AssemblySymbol declaringAssembly,
            TypeSymbolWithAnnotations elementType,
            int rank,
            ImmutableArray<int> sizes,
            ImmutableArray<int> lowerBounds)
        {
            return CreateMDArray(elementType, rank, sizes, lowerBounds, declaringAssembly.GetSpecialType(SpecialType.System_Array));
        }

        internal static ArrayTypeSymbol CreateSZArray(
            TypeSymbolWithAnnotations elementType,
            NamedTypeSymbol array)
        {
            return new SZArray(elementType, array, GetSZArrayInterfaces(elementType, array.ContainingAssembly));
        }

        internal static ArrayTypeSymbol CreateSZArray(
            TypeSymbolWithAnnotations elementType,
            NamedTypeSymbol array,
            ImmutableArray<NamedTypeSymbol> constructedInterfaces)
        {
            return new SZArray(elementType, array, constructedInterfaces);
        }

        internal static ArrayTypeSymbol CreateSZArray(
            AssemblySymbol declaringAssembly,
            TypeSymbolWithAnnotations elementType)
        {
            return CreateSZArray(elementType, declaringAssembly.GetSpecialType(SpecialType.System_Array), GetSZArrayInterfaces(elementType, declaringAssembly));
        }

        private static ImmutableArray<NamedTypeSymbol> GetSZArrayInterfaces(
            TypeSymbolWithAnnotations elementType,
            AssemblySymbol declaringAssembly)
        {
            var constructedInterfaces = ArrayBuilder<NamedTypeSymbol>.GetInstance();

            //There are cases where the platform does contain the interfaces.
            //So it is fine not to have them listed under the type
            var iListOfT = declaringAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IList_T);
            if (!iListOfT.IsErrorType())
            {
                constructedInterfaces.Add(new ConstructedNamedTypeSymbol(iListOfT, ImmutableArray.Create(elementType)));
            }

            var iReadOnlyListOfT = declaringAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyList_T);

            if (!iReadOnlyListOfT.IsErrorType())
            {
                constructedInterfaces.Add(new ConstructedNamedTypeSymbol(iReadOnlyListOfT, ImmutableArray.Create(elementType)));
            }

            return constructedInterfaces.ToImmutableAndFree();
        }

        /// <summary>
        /// Gets the number of dimensions of the array. A regular single-dimensional array
        /// has rank 1, a two-dimensional array has rank 2, etc.
        /// </summary>
        public abstract int Rank { get; }

        /// <summary>
        /// Is this zero-based one-dimensional array, i.e. SZArray in CLR terms.
        /// </summary>
        internal abstract bool IsSZArray { get; }

        internal bool HasSameShapeAs(ArrayTypeSymbol other)
        {
            return Rank == other.Rank && IsSZArray == other.IsSZArray;
        }

        /// <summary>
        /// Specified sizes for dimensions, by position. The length can be less than <see cref="Rank"/>,
        /// meaning that some trailing dimensions don't have the size specified.
        /// The most common case is none of the dimensions have the size specified - an empty array is returned.
        /// </summary>
        internal virtual ImmutableArray<int> Sizes
        {
            get
            {
                return ImmutableArray<int>.Empty;
            }
        }

        /// <summary>
        /// Specified lower bounds for dimensions, by position. The length can be less than <see cref="Rank"/>,
        /// meaning that some trailing dimensions don't have the lower bound specified.
        /// The most common case is all dimensions are zero bound - a null array is returned in this case.
        /// </summary>
        internal virtual ImmutableArray<int> LowerBounds
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
        /// Gets the type of the elements stored in the array.
        /// </summary>
        public TypeSymbolWithAnnotations ElementType
        {
            get
            {
                return _elementType;
            }
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                return _baseType;
            }
        }

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

        internal sealed override bool IsManagedType
        {
            get
            {
                return true;
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

        internal override bool Equals(TypeSymbol t2, TypeSymbolEqualityOptions options)
        {
            return this.Equals(t2 as ArrayTypeSymbol, options);
        }

        internal bool Equals(ArrayTypeSymbol other)
        {
            return Equals(other, TypeSymbolEqualityOptions.None);
        }

        private bool Equals(ArrayTypeSymbol other, TypeSymbolEqualityOptions options)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if ((object)other == null || !other.HasSameShapeAs(this) || 
                !other.ElementType.Equals(ElementType, options))
            {
                return false;
            }

            // Make sure bounds are the same.
            if ((options & TypeSymbolEqualityOptions.IgnoreArraySizesAndLowerBounds) == 0 && !this.HasSameSizesAndLowerBoundsAs(other))
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
                current = cur.ElementType.TypeSymbol;
            }

            return Hash.Combine(current, hash);
        }

        internal override bool ContainsNullableReferenceTypes()
        {
            return ElementType.ContainsNullableReferenceTypes();
        }

        internal override void AddNullableTransforms(ArrayBuilder<bool> transforms)
        {
            ElementType.AddNullableTransforms(transforms);
        }

        internal override bool ApplyNullableTransforms(ImmutableArray<bool> transforms, ref int position, out TypeSymbol result)
        {
            TypeSymbolWithAnnotations oldElementType = ElementType;
            TypeSymbolWithAnnotations newElementType;

            if (!oldElementType.ApplyNullableTransforms(transforms, ref position, out newElementType))
            {
                result = this;
                return false; 
            }

            if ((object)oldElementType == newElementType)
            {
                result = this;
            }
            else
            {
                result = IsSZArray ?
                    ArrayTypeSymbol.CreateSZArray(newElementType, _baseType) :
                    ArrayTypeSymbol.CreateMDArray(newElementType, Rank, Sizes, LowerBounds, _baseType);
            }

            return true;
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
            if (DeriveUseSiteDiagnosticFromType(ref result, this.ElementType))
            {
                return result;
            }

            return result;
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return _elementType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes) ||
                   ((object)_baseType != null && _baseType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes)) ||
                   GetUnificationUseSiteDiagnosticRecursive(ref result, this.InterfacesNoUseSiteDiagnostics(), owner, ref checkedTypes);
        }

        #endregion

        #region IArrayTypeSymbol Members

        ITypeSymbol IArrayTypeSymbol.ElementType
        {
            get { return this.ElementType.TypeSymbol; }
        }

        ImmutableArray<CustomModifier> IArrayTypeSymbol.CustomModifiers
        {
            get { return this.ElementType.CustomModifiers; }
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
                TypeSymbolWithAnnotations elementType,
                NamedTypeSymbol array,
                ImmutableArray<NamedTypeSymbol> constructedInterfaces)
                : base(elementType, array)
            {
                Debug.Assert(constructedInterfaces.Length <= 2);
                _interfaces = constructedInterfaces;
            }

            public override int Rank
            {
                get
                {
                    return 1;
                }
            }

            internal override bool IsSZArray
            {
                get
                {
                    return true;
                }
            }

            internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved = null)
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
        private class MDArray : ArrayTypeSymbol
        {
            private readonly int _rank;

            internal MDArray(
                TypeSymbolWithAnnotations elementType,
                int rank,
                NamedTypeSymbol array)
                : base(elementType, array)
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

            internal sealed override bool IsSZArray
            {
                get
                {
                    return false;
                }
            }

            internal sealed override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved = null)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
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
                TypeSymbolWithAnnotations elementType,
                int rank,
                ImmutableArray<int> sizes,
                ImmutableArray<int> lowerBounds,
                NamedTypeSymbol array)
                : base(elementType, rank, array)
            {
                Debug.Assert(!sizes.IsDefaultOrEmpty || !lowerBounds.IsDefault);
                Debug.Assert(lowerBounds.IsDefaultOrEmpty || (!lowerBounds.IsEmpty && (lowerBounds.Length != rank || !lowerBounds.All(b => b == 0))));
                _sizes = sizes.NullToEmpty();
                _lowerBounds = lowerBounds;
            }

            internal override ImmutableArray<int> Sizes
            {
                get
                {
                    return _sizes;
                }
            }

            internal override ImmutableArray<int> LowerBounds
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
