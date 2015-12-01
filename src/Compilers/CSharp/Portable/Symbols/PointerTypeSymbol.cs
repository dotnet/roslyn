// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a pointer type such as "int *". Pointer types
    /// are used only in unsafe code.
    /// </summary>
    internal sealed partial class PointerTypeSymbol : TypeSymbol, IPointerTypeSymbol
    {
        private readonly TypeSymbolWithAnnotations _pointedAtType;

        /// <summary>
        /// Create a new PointerTypeSymbol.
        /// </summary>
        /// <param name="pointedAtType">The type being pointed at.</param>
        internal PointerTypeSymbol(TypeSymbolWithAnnotations pointedAtType)
        {
            Debug.Assert((object)pointedAtType != null);

            _pointedAtType = pointedAtType;
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.NotApplicable; }
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

        /// <summary>
        /// Gets the type of the storage location that an instance of the pointer type points to.
        /// </summary>
        public TypeSymbolWithAnnotations PointedAtType
        {
            get
            {
                return _pointedAtType;
            }
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                // Pointers do not support boxing, so they really have no base type.
                return null;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved)
        {
            // Pointers do not support boxing, so they really have no interfaces
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override bool IsReferenceType
        {
            get
            {
                return false;
            }
        }

        public override bool IsValueType
        {
            get
            {
                return true;
            }
        }

        internal sealed override bool IsManagedType
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
                return SymbolKind.PointerType;
            }
        }

        public override TypeKind TypeKind
        {
            get
            {
                return TypeKind.Pointer;
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
            return visitor.VisitPointerType(this, argument);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitPointerType(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitPointerType(this);
        }

        public override int GetHashCode()
        {
            // We don't want to blow the stack if we have a type like T***************...***,
            // so we do not recurse until we have a non-array. 

            int indirections = 0;
            TypeSymbol current = this;
            while (current.TypeKind == TypeKind.Pointer)
            {
                indirections += 1;
                current = ((PointerTypeSymbol)current).PointedAtType.TypeSymbol;
            }

            return Hash.Combine(current, indirections);
        }

        internal override bool Equals(TypeSymbol t2, TypeSymbolEqualityOptions options)
        {
            return this.Equals(t2 as PointerTypeSymbol, options);
        }

        internal bool Equals(PointerTypeSymbol other)
        {
            return this.Equals(other, TypeSymbolEqualityOptions.None);
        }

        private bool Equals(PointerTypeSymbol other, TypeSymbolEqualityOptions options)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if ((object)other == null || !other._pointedAtType.Equals(_pointedAtType, options))
            {
                return false;
            }

            return true;
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            DiagnosticInfo result = null;

            // Check type, custom modifiers
            DeriveUseSiteDiagnosticFromType(ref result, this.PointedAtType);
            return result;
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return this.PointedAtType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes);
        }

        #region IPointerTypeSymbol Members

        ITypeSymbol IPointerTypeSymbol.PointedAtType
        {
            get { return this.PointedAtType.TypeSymbol; }
        }

        ImmutableArray<CustomModifier> IPointerTypeSymbol.CustomModifiers
        {
            get { return this.PointedAtType.CustomModifiers; }
        }

        #endregion

        #region ISymbol Members

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitPointerType(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitPointerType(this);
        }

        #endregion
    }
}
