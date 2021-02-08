// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
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
            bool isOverride)
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
                isAutoProperty: true,
                isExpressionBodied: false,
                isInitOnly: true,
                RefKind.None,
                backingParameter.Name,
                indexerNameAttributeLists: new SyntaxList<AttributeListSyntax>(),
                backingParameter.Locations[0])
        {
            BackingParameter = (SourceParameterSymbol)backingParameter;
        }

        public override IAttributeTargetSymbol AttributesOwner => BackingParameter as IAttributeTargetSymbol ?? this;

        protected override Location TypeLocation
            => ((ParameterSyntax)CSharpSyntaxNode).Type!.Location;

        public override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
            => BackingParameter.AttributeDeclarationList;

        protected override SourcePropertyAccessorSymbol CreateGetAccessorSymbol(bool isAutoPropertyAccessor, DiagnosticBag diagnostics)
        {
            Debug.Assert(isAutoPropertyAccessor);
            return CreateAccessorSymbol(isGet: true, CSharpSyntaxNode, diagnostics);
        }

        protected override SourcePropertyAccessorSymbol CreateSetAccessorSymbol(bool isAutoPropertyAccessor, DiagnosticBag diagnostics)
        {
            Debug.Assert(isAutoPropertyAccessor);
            return CreateAccessorSymbol(isGet: false, CSharpSyntaxNode, diagnostics);
        }

        private SourcePropertyAccessorSymbol CreateAccessorSymbol(
            bool isGet,
            CSharpSyntaxNode syntax,
            DiagnosticBag diagnostics)
        {
            return SourcePropertyAccessorSymbol.CreateAccessorSymbol(
                isGet,
                usesInit: !isGet, // the setter is always init-only
                ContainingType,
                this,
                _modifiers,
                ((ParameterSyntax)syntax).Identifier.GetLocation(),
                syntax,
                diagnostics);
        }

        protected override (TypeWithAnnotations Type, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindType(DiagnosticBag diagnostics)
        {
            return (BackingParameter.TypeWithAnnotations,
                    ImmutableArray<ParameterSymbol>.Empty);
        }

        protected override bool HasPointerTypeSyntactically
            // Since we already bound the type, don't bother looking at syntax
            => TypeWithAnnotations.DefaultType.IsPointerOrFunctionPointer();

        public static bool HaveCorrespondingSynthesizedRecordPropertySymbol(SourceParameterSymbol parameter)
        {
            return parameter.ContainingSymbol is SynthesizedRecordConstructor &&
                   parameter.ContainingType.GetMembersUnordered().Any((s, parameter) => (s as SynthesizedRecordPropertySymbol)?.BackingParameter == (object)parameter, parameter);
        }
    }
}
