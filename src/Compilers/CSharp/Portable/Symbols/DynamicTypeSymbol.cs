// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class DynamicTypeSymbol : TypeSymbol, IDynamicTypeSymbol
    {
        internal static readonly DynamicTypeSymbol Instance = new DynamicTypeSymbol();

        private DynamicTypeSymbol()
        {
        }

        public override string Name
        {
            get
            {
                return "dynamic";
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        public override bool IsReferenceType
        {
            get
            {
                return true;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.DynamicType;
            }
        }

        public override TypeKind TypeKind
        {
            get
            {
                return TypeKind.Dynamic;
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

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                return null;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override bool IsStatic
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

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitDynamicType(this, argument);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitDynamicType(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitDynamicType(this);
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return null;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.NotApplicable;
            }
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return false;
        }

        public override int GetHashCode()
        {
            // return the distinguished value for 'object' because the hash code ignores the distinction
            // between dynamic and object.  It also ignores custom modifiers.
            return (int)Microsoft.CodeAnalysis.SpecialType.System_Object;
        }

        internal override bool Equals(TypeSymbol t2, TypeSymbolEqualityOptions options)
        {
            if ((object)t2 == null)
            {
                return false;
            }

            if (ReferenceEquals(this, t2) || t2.TypeKind == TypeKind.Dynamic)
            {
                return true;
            }

            if ((options & TypeSymbolEqualityOptions.IgnoreDynamic) != 0)
            {
                var other = t2 as NamedTypeSymbol;
                return (object)other != null && other.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object;
            }

            return false;
        }

        internal override bool ContainsNullableReferenceTypes()
        {
            return false;
        }

        internal override void AddNullableTransforms(ArrayBuilder<bool> transforms)
        {
        }

        internal override bool ApplyNullableTransforms(ImmutableArray<bool> transforms, ref int position, out TypeSymbol result)
        {
            result = this;
            return true;
        }

        internal override TypeSymbol SetUnknownNullabilityForRefernceTypes()
        {
            return this;
        }

        #region ISymbol Members

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitDynamicType(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitDynamicType(this);
        }

        #endregion
    }
}
