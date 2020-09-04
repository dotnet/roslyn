// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

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
            DiagnosticBag diagnostics)
            : base(
                containingType,
                binder: null,
                syntax: syntax,
                getSyntax: syntax,
                setSyntax: syntax,
                arrowExpression: null,
                interfaceSpecifier: null,
                modifiers: DeclarationModifiers.Public | (isOverride ? DeclarationModifiers.Override : DeclarationModifiers.None),
                isIndexer: false,
                hasInitializer: true, // Synthesized record properties always have a synthesized initializer
                isAutoProperty: true,
                hasAccessorList: false,
                isInitOnly: true,
                RefKind.None,
                backingParameter.Name,
                backingParameter.Locations[0],
                typeOpt: backingParameter.TypeWithAnnotations,
                hasParameters: false,
                diagnostics)
        {
            BackingParameter = (SourceParameterSymbol)backingParameter;
        }


        public override IAttributeTargetSymbol AttributesOwner => BackingParameter as IAttributeTargetSymbol ?? this;

        protected override Location TypeLocation
            => ((ParameterSyntax)CSharpSyntaxNode).Type!.Location;

        protected override SyntaxTokenList GetModifierTokens(SyntaxNode syntax)
            => new SyntaxTokenList();

        public override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
            => BackingParameter.AttributeDeclarationList;

        protected override void CheckForBlockAndExpressionBody(CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            // Nothing to do here
        }

        protected override SourcePropertyAccessorSymbol CreateAccessorSymbol(
            bool isGet,
            CSharpSyntaxNode? syntax,
            PropertySymbol? explicitlyImplementedPropertyOpt,
            string? aliasQualifierOpt,
            bool isAutoPropertyAccessor,
            bool isExplicitInterfaceImplementation,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(syntax is object);
            Debug.Assert(isAutoPropertyAccessor);

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

        protected override SourcePropertyAccessorSymbol CreateExpressionBodiedAccessor(
            ArrowExpressionClauseSyntax syntax,
            PropertySymbol? explicitlyImplementedPropertyOpt,
            string? aliasQualifierOpt,
            bool isExplicitInterfaceImplementation,
            DiagnosticBag diagnostics)
        {
            // There should be no expression-bodied synthesized record properties
            throw ExceptionUtilities.Unreachable;
        }

        protected override ImmutableArray<ParameterSymbol> ComputeParameters(Binder? binder, CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            return ImmutableArray<ParameterSymbol>.Empty;
        }

        protected override TypeWithAnnotations ComputeType(Binder? binder, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            return BackingParameter.TypeWithAnnotations;
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
