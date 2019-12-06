// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class DynamicTypeSymbol : TypeSymbol
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

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => null;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
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

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt = null)
        {
            if ((object)t2 == null)
            {
                return false;
            }

            if (ReferenceEquals(this, t2) || t2.TypeKind == TypeKind.Dynamic)
            {
                return true;
            }

            if ((comparison & TypeCompareKind.IgnoreDynamic) != 0)
            {
                var other = t2 as NamedTypeSymbol;
                return (object)other != null && other.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object;
            }

            return false;
        }

        internal override void AddNullableTransforms(ArrayBuilder<byte> transforms)
        {
        }

        internal override bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbol result)
        {
            result = this;
            return true;
        }

        internal override TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform)
        {
            return this;
        }

        internal override TypeSymbol MergeEquivalentTypes(TypeSymbol other, VarianceKind variance)
        {
            Debug.Assert(this.Equals(other, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
            return this;
        }

        protected override ISymbol CreateISymbol()
        {
            return new PublicModel.DynamicTypeSymbol(this, DefaultNullableAnnotation);
        }

        protected sealed override ITypeSymbol CreateITypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            Debug.Assert(nullableAnnotation != DefaultNullableAnnotation);
            return new PublicModel.DynamicTypeSymbol(this, nullableAnnotation);
        }
    }
}
