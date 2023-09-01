// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // PROTOTYPE: Can all the properties share a common class, with the only differences being the accessor method bodies?
    internal sealed class SynthesizedReadOnlyListIsReadOnlyProperty : SynthesizedReadOnlyListPropertyBase
    {
        private readonly PropertySymbol _interfaceProperty;

        internal SynthesizedReadOnlyListIsReadOnlyProperty(SynthesizedReadOnlyListTypeSymbol containingType, PropertySymbol interfaceProperty) :
            base(containingType)
        {
            _interfaceProperty = interfaceProperty;
            Name = ExplicitInterfaceHelpers.GetMemberName(interfaceProperty.Name, interfaceProperty.ContainingType, aliasQualifierOpt: null);
            GetMethod = new PropertyGetAccessor(containingType, interfaceProperty.GetMethod);
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

        private sealed class PropertyGetAccessor : SynthesizedImplementationMethod
        {
            internal PropertyGetAccessor(SynthesizedReadOnlyListTypeSymbol containingType, MethodSymbol interfaceMethod) :
                base(interfaceMethod, containingType)
            {
            }

            internal override bool SynthesizesLoweredBoundBody => true;

            internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
            {
                SyntheticBoundNodeFactory f = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
                f.CurrentFunction = this;

                try
                {
                    // return true;
                    var statement = f.Return(f.Literal(true));
                    f.CloseMethod(statement);
                }
                catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
                {
                    diagnostics.Add(ex.Diagnostic);
                    f.CloseMethod(f.ThrowNull());
                }
            }
        }
    }
}
