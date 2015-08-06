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
    internal sealed partial class ArrayTypeSymbol : TypeSymbol, IArrayTypeSymbol
    {
        private readonly TypeSymbol _elementType;
        private readonly int _rank;
        private readonly NamedTypeSymbol _baseType;
        private readonly ImmutableArray<NamedTypeSymbol> _interfaces;
        private readonly ImmutableArray<CustomModifier> _customModifiers;

        /// <summary>
        /// Create a new ArrayTypeSymbol.
        /// </summary>
        /// <param name="elementType">The element type of this array type.</param>
        /// <param name="customModifiers">Custom modifiers for the element type of this array type.</param>
        /// <param name="rank">The rank of this array type.</param>
        /// <param name="declaringAssembly">The assembly "declaring"/using the array type.</param>
        internal ArrayTypeSymbol(
            AssemblySymbol declaringAssembly,
            TypeSymbol elementType,
            ImmutableArray<CustomModifier> customModifiers = default(ImmutableArray<CustomModifier>),
            int rank = 1)
            : this(elementType,
                   rank,
                   declaringAssembly.GetSpecialType(SpecialType.System_Array),
                   GetArrayInterfaces(elementType, rank, declaringAssembly),
                   customModifiers.NullToEmpty())
        {
        }

        internal ArrayTypeSymbol(
            TypeSymbol elementType,
            int rank,
            NamedTypeSymbol array,
            ImmutableArray<NamedTypeSymbol> constructedInterfaces,
            ImmutableArray<CustomModifier> customModifiers)
        {
            Debug.Assert((object)elementType != null);
            Debug.Assert((object)array != null);
            Debug.Assert(rank >= 1);
            Debug.Assert(constructedInterfaces.Length <= 2);
            Debug.Assert(constructedInterfaces.Length == 0 || rank == 1);
            Debug.Assert(rank == 1 || !customModifiers.Any());

            _elementType = elementType;
            _rank = rank;
            _baseType = array;
            _interfaces = constructedInterfaces;
            _customModifiers = customModifiers;
        }

        private static ImmutableArray<NamedTypeSymbol> GetArrayInterfaces(
            TypeSymbol elementType,
            int rank,
            AssemblySymbol declaringAssembly)
        {
            var constructedInterfaces = ArrayBuilder<NamedTypeSymbol>.GetInstance();

            if (rank == 1)
            {
                //There are cases where the platform does contain the interfaces.
                //So it is fine not to have them listed under the type
                var iListOfT = declaringAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IList_T);
                if (!iListOfT.IsErrorType())
                {
                    constructedInterfaces.Add(new ConstructedNamedTypeSymbol(iListOfT, ImmutableArray.Create(new TypeWithModifiers(elementType))));
                }

                var iReadOnlyListOfT = declaringAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyList_T);

                if (!iReadOnlyListOfT.IsErrorType())
                {
                    constructedInterfaces.Add(new ConstructedNamedTypeSymbol(iReadOnlyListOfT, ImmutableArray.Create(new TypeWithModifiers(elementType))));
                }
            }

            return constructedInterfaces.ToImmutableAndFree();
        }

        /// <summary>
        /// Gets the list of custom modifiers associated with the array.
        /// Returns an empty list if there are no custom modifiers.
        /// </summary>
        public ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return _customModifiers;
            }
        }

        /// <summary>
        /// Gets the number of dimensions of the array. A regular single-dimensional array
        /// has rank 1, a two-dimensional array has rank 2, etc.
        /// </summary>
        public int Rank
        {
            get
            {
                return _rank;
            }
        }

        /// <summary>
        /// Gets the type of the elements stored in the array.
        /// </summary>
        public TypeSymbol ElementType
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

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved = null)
        {
            return _interfaces;
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

        internal override bool Equals(TypeSymbol t2, bool ignoreCustomModifiers, bool ignoreDynamic)
        {
            return this.Equals(t2 as ArrayTypeSymbol, ignoreCustomModifiers, ignoreDynamic);
        }

        internal bool Equals(ArrayTypeSymbol other)
        {
            return Equals(other, false, false);
        }

        private bool Equals(ArrayTypeSymbol other, bool ignoreCustomModifiers, bool ignoreDynamic)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if ((object)other == null || other.Rank != Rank || !other.ElementType.Equals(ElementType, ignoreCustomModifiers, ignoreDynamic))
            {
                return false;
            }

            // Make sure custom modifiers are the same.
            if (!ignoreCustomModifiers)
            {
                var mod = this.CustomModifiers;
                var otherMod = other.CustomModifiers;
                var count = mod.Length;

                if (count != otherMod.Length)
                {
                    return false;
                }

                for (int i = 0; i < count; i++)
                {
                    if (!mod[i].Equals(otherMod[i]))
                    {
                        return false;
                    }
                }
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
            if (DeriveUseSiteDiagnosticFromType(ref result, this.ElementType))
            {
                return result;
            }

            // check custom modifiers
            if (DeriveUseSiteDiagnosticFromCustomModifiers(ref result, this.CustomModifiers))
            {
                return result;
            }

            return result;
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return _elementType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes) ||
                   ((object)_baseType != null && _baseType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes)) ||
                   GetUnificationUseSiteDiagnosticRecursive(ref result, this.InterfacesNoUseSiteDiagnostics(), owner, ref checkedTypes) ||
                   GetUnificationUseSiteDiagnosticRecursive(ref result, this.CustomModifiers, owner, ref checkedTypes);
        }

        #endregion

        #region IArrayTypeSymbol Members

        ITypeSymbol IArrayTypeSymbol.ElementType
        {
            get { return this.ElementType; }
        }

        ImmutableArray<CustomModifier> IArrayTypeSymbol.CustomModifiers
        {
            get { return this.CustomModifiers; }
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
    }
}
