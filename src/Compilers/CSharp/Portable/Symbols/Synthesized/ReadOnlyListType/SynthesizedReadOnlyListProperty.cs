// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedReadOnlyListProperty : PropertySymbol
    {
        private readonly NamedTypeSymbol _containingType;
        private readonly PropertySymbol _interfaceProperty;

        internal SynthesizedReadOnlyListProperty(
            NamedTypeSymbol containingType,
            PropertySymbol interfaceProperty,
            GenerateMethodBodyDelegate getAccessorBody,
            GenerateMethodBodyDelegate? setAccessorBody = null)
        {
            Debug.Assert(setAccessorBody is null == interfaceProperty.SetMethod is null);

            _containingType = containingType;
            _interfaceProperty = interfaceProperty;
            Name = ExplicitInterfaceHelpers.GetMemberName(interfaceProperty.Name, interfaceProperty.ContainingType, aliasQualifierOpt: null);
            Parameters = interfaceProperty.Parameters.SelectAsArray(static (p, t) => SynthesizedParameterSymbol.DeriveParameter(t, p), this);
            GetMethod = new SynthesizedReadOnlyListMethod(containingType, interfaceProperty.GetMethod, getAccessorBody);
            SetMethod = interfaceProperty.SetMethod is null ? null : new SynthesizedReadOnlyListMethod(containingType, interfaceProperty.SetMethod, setAccessorBody!);
        }

        public override string Name { get; }

        public override RefKind RefKind => RefKind.None;

        public override TypeWithAnnotations TypeWithAnnotations => _interfaceProperty.TypeWithAnnotations;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override ImmutableArray<ParameterSymbol> Parameters { get; }

        public override bool IsIndexer => Parameters.Length > 0;

        public override MethodSymbol? GetMethod { get; }

        public override MethodSymbol? SetMethod { get; }

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations => ImmutableArray.Create(_interfaceProperty);

        public override Symbol ContainingSymbol => _containingType;

        public override ImmutableArray<Location> Locations => _containingType.Locations;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _containingType.DeclaringSyntaxReferences;

        public override Accessibility DeclaredAccessibility => Accessibility.Private;

        public override bool IsStatic => false;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsExtern => false;

        internal override bool IsRequired => false;

        internal override bool HasSpecialName => false;

        internal override Cci.CallingConvention CallingConvention => _interfaceProperty.CallingConvention;

        internal override bool MustCallMethodsDirectly => false;

        internal override bool HasUnscopedRefAttribute => false;

        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;

        internal override int? TryGetOverloadResolutionPriority() => null;
    }
}
