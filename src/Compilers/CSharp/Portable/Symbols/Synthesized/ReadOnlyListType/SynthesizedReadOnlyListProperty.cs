// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedReadOnlyListProperty : SynthesizedReadOnlyListPropertyBase
    {
        private readonly PropertySymbol _interfaceProperty;

        internal SynthesizedReadOnlyListProperty(SynthesizedReadOnlyListTypeSymbol containingType, PropertySymbol interfaceProperty, GenerateMethodBodyDelegate getAccessorBody) :
            base(containingType)
        {
            _interfaceProperty = interfaceProperty;
            Name = ExplicitInterfaceHelpers.GetMemberName(interfaceProperty.Name, interfaceProperty.ContainingType, aliasQualifierOpt: null);
            GetMethod = new SynthesizedReadOnlyListMethod(containingType, interfaceProperty.GetMethod, getAccessorBody);
        }

        public override string Name { get; }

        public override TypeWithAnnotations TypeWithAnnotations => _interfaceProperty.TypeWithAnnotations;

        public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

        public override bool IsIndexer => false;

        public override MethodSymbol? GetMethod { get; }

        public override MethodSymbol? SetMethod => null;

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations => ImmutableArray.Create(_interfaceProperty);

        internal override Cci.CallingConvention CallingConvention => _interfaceProperty.CallingConvention;

        public override Accessibility DeclaredAccessibility => Accessibility.Private;
    }
}
