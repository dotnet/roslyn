// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordEqualityContractProperty : SourcePropertySymbolBase
    {
        internal const string PropertyName = "EqualityContract";

        public SynthesizedRecordEqualityContractProperty(
            SourceMemberContainerTypeSymbol containingType,
            DiagnosticBag diagnostics)
            : base(
                containingType,
                binder: null,
                syntax: (CSharpSyntaxNode)containingType.SyntaxReferences[0].GetSyntax(),
                getSyntax: (CSharpSyntaxNode)containingType.SyntaxReferences[0].GetSyntax(),
                setSyntax: null,
                arrowExpression: null,
                interfaceSpecifier: null,
                modifiers: (containingType.IsSealed, containingType.BaseTypeNoUseSiteDiagnostics.IsObjectType()) switch
                {
                    (true, true) => DeclarationModifiers.Private,
                    (false, true) => DeclarationModifiers.Protected | DeclarationModifiers.Virtual,
                    (_, false) => DeclarationModifiers.Protected | DeclarationModifiers.Override
                },
                isIndexer: false,
                hasInitializer: false,
                isAutoProperty: false,
                hasAccessorList: false,
                isInitOnly: false,
                RefKind.None,
                PropertyName,
                containingType.Locations[0],
                typeOpt: TypeWithAnnotations.Create(Binder.GetWellKnownType(containingType.DeclaringCompilation, WellKnownType.System_Type, diagnostics, containingType.Locations[0]), NullableAnnotation.NotAnnotated),
                hasParameters: false,
                diagnostics)
        {
        }

        public override bool IsImplicitlyDeclared => true;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
            => new SyntaxList<AttributeListSyntax>();

        public override IAttributeTargetSymbol AttributesOwner => this;

        protected override Location TypeLocation
            => ContainingType.Locations[0];

        protected override SyntaxTokenList GetModifierTokens(SyntaxNode syntax)
            => new SyntaxTokenList();

        protected override void CheckForBlockAndExpressionBody(CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            // Nothing to do here
        }

        protected override SourcePropertyAccessorSymbol? CreateAccessorSymbol(
            bool isGet,
            CSharpSyntaxNode? syntax,
            PropertySymbol? explicitlyImplementedPropertyOpt,
            string? aliasQualifierOpt,
            bool isAutoPropertyAccessor,
            bool isExplicitInterfaceImplementation,
            DiagnosticBag diagnostics)
        {
            if (!isGet)
            {
                return null;
            }

            Debug.Assert(syntax is object);

            return SourcePropertyAccessorSymbol.CreateAccessorSymbol(
                ContainingType,
                this,
                _modifiers,
                ContainingType.Locations[0],
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
            // No need to worry about reporting use-site diagnostics, we already did that in the constructor
            return TypeWithAnnotations.Create(DeclaringCompilation.GetWellKnownType(WellKnownType.System_Type), NullableAnnotation.NotAnnotated);
        }

        protected override bool HasPointerTypeSyntactically => false;

        protected override void ValidatePropertyType(DiagnosticBag diagnostics)
        {
            base.ValidatePropertyType(diagnostics);
            VerifyOverridesEqualityContractFromBase(this, diagnostics);
        }

        internal static void VerifyOverridesEqualityContractFromBase(PropertySymbol overriding, DiagnosticBag diagnostics)
        {
            if (overriding.ContainingType.BaseTypeNoUseSiteDiagnostics.IsObjectType())
            {
                return;
            }

            bool reportAnError = false;

            if (!overriding.IsOverride)
            {
                reportAnError = true;
            }
            else
            {
                var overridden = overriding.OverriddenProperty;

                if (overridden is object &&
                    !overridden.ContainingType.Equals(overriding.ContainingType.BaseTypeNoUseSiteDiagnostics, TypeCompareKind.AllIgnoreOptions))
                {
                    reportAnError = true;
                }
            }

            if (reportAnError)
            {
                diagnostics.Add(ErrorCode.ERR_DoesNotOverrideBaseEqualityContract, overriding.Locations[0], overriding, overriding.ContainingType.BaseTypeNoUseSiteDiagnostics);
            }
        }

        internal sealed class GetAccessorSymbol : SourcePropertyAccessorSymbol
        {
            internal GetAccessorSymbol(
                NamedTypeSymbol containingType,
                string name,
                SourcePropertySymbolBase property,
                DeclarationModifiers propertyModifiers,
                ImmutableArray<MethodSymbol> explicitInterfaceImplementations,
                Location location,
                CSharpSyntaxNode syntax,
                DiagnosticBag diagnostics)
                : base(
                       containingType,
                       name,
                       property,
                       propertyModifiers,
                       explicitInterfaceImplementations,
                       location,
                       syntax,
                       hasBody: false,
                       hasExpressionBody: false,
                       isIterator: false,
                       modifiers: new SyntaxTokenList(),
                       MethodKind.PropertyGet,
                       usesInit: false,
                       isAutoPropertyAccessor: true,
                       isExplicitInterfaceImplementation: false,
                       diagnostics)
            {
            }

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

            internal override bool SynthesizesLoweredBoundBody => true;

            internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
            {
                var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);

                try
                {
                    F.CurrentFunction = this;
                    F.CloseMethod(F.Block(F.Return(F.Typeof(ContainingType))));
                }
                catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
                {
                    diagnostics.Add(ex.Diagnostic);
                    F.CloseMethod(F.ThrowNull());
                }
            }
        }
    }
}
