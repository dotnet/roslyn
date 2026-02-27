// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedUnionValuePropertySymbol : SourcePropertySymbolBase
    {
        public SynthesizedUnionValuePropertySymbol(
            SourceMemberContainerTypeSymbol containingType,
            TypeDeclarationSyntax syntax,
            BindingDiagnosticBag diagnostics)
            : base(
                containingType,
                syntax: syntax,
                hasGetAccessor: true,
                hasSetAccessor: false,
                isExplicitInterfaceImplementation: false,
                explicitInterfaceType: null,
                aliasQualifierOpt: null,
                modifiers: DeclarationModifiers.Public,
                hasInitializer: false,
                hasExplicitAccessMod: false,
                hasAutoPropertyGet: true,
                hasAutoPropertySet: false,
                isExpressionBodied: false,
                accessorsHaveImplementation: true,
                getterUsesFieldKeyword: false,
                setterUsesFieldKeyword: false,
                RefKind.None,
                WellKnownMemberNames.ValuePropertyName,
                indexerNameAttributeLists: new SyntaxList<AttributeListSyntax>(),
                syntax.Location,
                diagnostics)
        {
        }

        public override bool IsImplicitlyDeclared => true;

        protected override SourcePropertySymbolBase? BoundAttributesSource => null;

        public override IAttributeTargetSymbol AttributesOwner => this;

        protected override Location TypeLocation
            => ((TypeDeclarationSyntax)CSharpSyntaxNode).Identifier.GetLocation();

        public override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
            => OneOrMany<SyntaxList<AttributeListSyntax>>.Empty;

        protected override SourcePropertyAccessorSymbol CreateGetAccessorSymbol(bool isAutoPropertyAccessor, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(isAutoPropertyAccessor);
            return SourcePropertyAccessorSymbol.CreateAccessorSymbol(
                ContainingType,
                this,
                _modifiers,
                TypeLocation,
                CSharpSyntaxNode,
                diagnostics);
        }

        protected override SourcePropertyAccessorSymbol CreateSetAccessorSymbol(bool isAutoPropertyAccessor, BindingDiagnosticBag diagnostics)
        {
            throw ExceptionUtilities.Unreachable();
        }

        protected override (TypeWithAnnotations Type, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindType(BindingDiagnosticBag diagnostics)
        {
            return (TypeWithAnnotations.Create(Binder.GetSpecialType(DeclaringCompilation, SpecialType.System_Object, TypeLocation, diagnostics), nullableAnnotation: NullableAnnotation.Annotated),
                    ImmutableArray<ParameterSymbol>.Empty);
        }

        internal override CallerUnsafeMode CallerUnsafeMode => CallerUnsafeMode.None;
    }
}
