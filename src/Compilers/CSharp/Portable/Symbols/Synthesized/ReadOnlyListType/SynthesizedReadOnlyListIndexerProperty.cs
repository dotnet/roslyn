// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedReadOnlyListIndexerProperty : SynthesizedReadOnlyListPropertyBase
    {
        private readonly PropertySymbol _interfaceProperty;

        internal SynthesizedReadOnlyListIndexerProperty(SynthesizedReadOnlyListTypeSymbol containingType, PropertySymbol interfaceProperty) :
            base(containingType)
        {
            _interfaceProperty = interfaceProperty;
            Name = ExplicitInterfaceHelpers.GetMemberName(interfaceProperty.Name, interfaceProperty.ContainingType, aliasQualifierOpt: null);
            GetMethod = new PropertyGetAccessor(containingType, interfaceProperty.GetMethod);
            SetMethod = interfaceProperty.SetMethod is null ? null : new SynthesizedReadOnlyListNotSupportedMethod(containingType, interfaceProperty.SetMethod);
        }

        public override string Name { get; }

        public override TypeWithAnnotations TypeWithAnnotations => _interfaceProperty.TypeWithAnnotations;

        public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty; // PROTOTYPE: This is incorrect.

        public override bool IsIndexer => false;

        public override MethodSymbol? GetMethod { get; }

        public override MethodSymbol? SetMethod { get; }

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
                    var field = ContainingType.GetFieldsToEmit().Single();
                    // return _items[index];
                    var statement = f.Return(
                        f.ArrayAccess(
                            f.Field(f.This(), field),
                            f.Parameter(Parameters[0])));
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
