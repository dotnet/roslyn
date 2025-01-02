// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedReadOnlyListTypeParameterSymbol : TypeParameterSymbol
    {
        private readonly SynthesizedReadOnlyListTypeSymbol _containingType;

        internal SynthesizedReadOnlyListTypeParameterSymbol(SynthesizedReadOnlyListTypeSymbol containingType)
        {
            Debug.Assert(containingType.IsClassType());
            _containingType = containingType;
        }

        public override string Name => "T";

        public override int Ordinal => 0;

        public override bool HasConstructorConstraint => false;

        public override TypeParameterKind TypeParameterKind => TypeParameterKind.Type;

        public override bool HasReferenceTypeConstraint => false;

        public override bool IsReferenceTypeFromConstraintTypes => false;

        public override bool HasNotNullConstraint => false;

        public override bool HasValueTypeConstraint => false;

        public override bool AllowsRefLikeType => false; // The list is a class type and cannot store ref structs as elements.

        public override bool IsValueTypeFromConstraintTypes => false;

        public override bool HasUnmanagedTypeConstraint => false;

        public override VarianceKind Variance => VarianceKind.None;

        public override Symbol ContainingSymbol => _containingType;

        public override ImmutableArray<Location> Locations => _containingType.Locations;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _containingType.DeclaringSyntaxReferences;

        internal override bool? IsNotNullable => null;

        internal override bool? ReferenceTypeConstraintIsNullable => null;

        internal override void EnsureAllConstraintsAreResolved()
        {
        }

        internal override ImmutableArray<TypeWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress) => ImmutableArray<TypeWithAnnotations>.Empty;

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress) => ContainingAssembly.GetSpecialType(SpecialType.System_Object);

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress) => ContainingAssembly.GetSpecialType(SpecialType.System_Object);

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress) => ImmutableArray<NamedTypeSymbol>.Empty;
    }
}
