// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordPropertySymbol : SourcePropertySymbolBase
    {
        public SourceParameterSymbol BackingParameter { get; }

        public SynthesizedRecordPropertySymbol(
            SourceMemberContainerTypeSymbol containingType,
            CSharpSyntaxNode syntax,
            ParameterSymbol backingParameter,
            bool isOverride,
            BindingDiagnosticBag diagnostics)
            : base(
                containingType,
                syntax: syntax,
                hasGetAccessor: true,
                hasSetAccessor: true,
                isExplicitInterfaceImplementation: false,
                explicitInterfaceType: null,
                aliasQualifierOpt: null,
                modifiers: DeclarationModifiers.Public | (isOverride ? DeclarationModifiers.Override : DeclarationModifiers.None),
                hasInitializer: true, // Synthesized record properties always have a synthesized initializer
                hasExplicitAccessMod: false,
                hasAutoPropertyGet: true,
                hasAutoPropertySet: true,
                isExpressionBodied: false,
                accessorsHaveImplementation: true,
                getterUsesFieldKeyword: false,
                setterUsesFieldKeyword: false,
                RefKind.None,
                backingParameter.Name,
                indexerNameAttributeLists: new SyntaxList<AttributeListSyntax>(),
                backingParameter.GetFirstLocation(),
                diagnostics)
        {
            BackingParameter = (SourceParameterSymbol)backingParameter;
        }

        protected override SourcePropertySymbolBase? BoundAttributesSource => null;

        public override IAttributeTargetSymbol AttributesOwner => BackingParameter as IAttributeTargetSymbol ?? this;

        protected override Location TypeLocation
            => ((ParameterSyntax)CSharpSyntaxNode).Type!.Location;

        public override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
            => OneOrMany.Create(BackingParameter.AttributeDeclarationList);

        protected override SourcePropertyAccessorSymbol CreateGetAccessorSymbol(bool isAutoPropertyAccessor, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(isAutoPropertyAccessor);
            return CreateAccessorSymbol(isGet: true, CSharpSyntaxNode, diagnostics);
        }

        protected override SourcePropertyAccessorSymbol CreateSetAccessorSymbol(bool isAutoPropertyAccessor, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(isAutoPropertyAccessor);
            return CreateAccessorSymbol(isGet: false, CSharpSyntaxNode, diagnostics);
        }

        private static bool ShouldUseInit(TypeSymbol container)
        {
            // the setter is always init-only in record class and in readonly record struct
            return !container.IsStructType() || container.IsReadOnly;
        }

        private SourcePropertyAccessorSymbol CreateAccessorSymbol(
            bool isGet,
            CSharpSyntaxNode syntax,
            BindingDiagnosticBag diagnostics)
        {
            var usesInit = !isGet && ShouldUseInit(ContainingType);
            return SourcePropertyAccessorSymbol.CreateAccessorSymbol(
                isGet,
                usesInit,
                ContainingType,
                this,
                _modifiers,
                ((ParameterSyntax)syntax).Identifier.GetLocation(),
                syntax,
                diagnostics);
        }

        protected override (TypeWithAnnotations Type, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindType(BindingDiagnosticBag diagnostics)
        {
            return (BackingParameter.TypeWithAnnotations,
                    ImmutableArray<ParameterSymbol>.Empty);
        }

        public static bool HaveCorrespondingSynthesizedRecordPropertySymbol(SourceParameterSymbol parameter)
        {
            return parameter.ContainingSymbol is SynthesizedPrimaryConstructor &&
                   parameter.ContainingType.GetMembersUnordered().Any((s, parameter) => (s as SynthesizedRecordPropertySymbol)?.BackingParameter == (object)parameter, parameter);
        }
    }
}
