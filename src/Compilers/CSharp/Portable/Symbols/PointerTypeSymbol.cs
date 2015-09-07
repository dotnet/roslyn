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
        private readonly TypeSymbol _pointedAtType;
        private readonly ImmutableArray<CustomModifier> _customModifiers;

        /// <summary>
        /// Create a new PointerTypeSymbol.
        /// </summary>
        /// <param name="pointedAtType">The type being pointed at.</param>
        internal PointerTypeSymbol(TypeSymbol pointedAtType)
            : this(pointedAtType, ImmutableArray<CustomModifier>.Empty)
        {
        }

        /// <summary>
        /// Create a new PointerTypeSymbol.
        /// </summary>
        /// <param name="pointedAtType">The type being pointed at.</param>
        /// <param name="customModifiers">Custom modifiers for the element type of this array type.</param>
        internal PointerTypeSymbol(TypeSymbol pointedAtType, ImmutableArray<CustomModifier> customModifiers)
        {
            Debug.Assert((object)pointedAtType != null);

            _pointedAtType = pointedAtType;
            _customModifiers = customModifiers.NullToEmpty();
        }

        /// <summary>
        /// The list of custom modifiers, if any, associated with the pointer type.
        /// </summary>
        public ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return _customModifiers;
            }
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
        public TypeSymbol PointedAtType
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
                current = ((PointerTypeSymbol)current).PointedAtType;
            }

            return Hash.Combine(current, indirections);
        }

        internal override bool Equals(TypeSymbol t2, bool ignoreCustomModifiersAndArraySizesAndLowerBounds, bool ignoreDynamic)
        {
            return this.Equals(t2 as PointerTypeSymbol, ignoreCustomModifiersAndArraySizesAndLowerBounds, ignoreDynamic);
        }

        internal bool Equals(PointerTypeSymbol other)
        {
            return this.Equals(other, false, false);
        }

        private bool Equals(PointerTypeSymbol other, bool ignoreCustomModifiersAndArraySizesAndLowerBounds, bool ignoreDynamic)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if ((object)other == null || !other._pointedAtType.Equals(_pointedAtType, ignoreCustomModifiersAndArraySizesAndLowerBounds, ignoreDynamic))
            {
                return false;
            }

            if (!ignoreCustomModifiersAndArraySizesAndLowerBounds)
            {
                // Make sure custom modifiers are the same.
                var mod = this.CustomModifiers;
                var otherMod = other.CustomModifiers;

                int count = mod.Length;

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

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            DiagnosticInfo result = null;

            // Check type, custom modifiers
            if (DeriveUseSiteDiagnosticFromType(ref result, this.PointedAtType) ||
                DeriveUseSiteDiagnosticFromCustomModifiers(ref result, this.CustomModifiers))
            {
            }

            return result;
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return this.PointedAtType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes) ||
                   GetUnificationUseSiteDiagnosticRecursive(ref result, this.CustomModifiers, owner, ref checkedTypes);
        }

        #region IPointerTypeSymbol Members

        ITypeSymbol IPointerTypeSymbol.PointedAtType
        {
            get { return this.PointedAtType; }
        }

        ImmutableArray<CustomModifier> IPointerTypeSymbol.CustomModifiers
        {
            get { return this.CustomModifiers; }
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
