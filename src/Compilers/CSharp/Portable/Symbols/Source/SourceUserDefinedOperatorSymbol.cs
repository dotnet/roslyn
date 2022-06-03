// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceUserDefinedOperatorSymbol : SourceUserDefinedOperatorSymbolBase
    {
        public static SourceUserDefinedOperatorSymbol CreateUserDefinedOperatorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            Binder bodyBinder,
            OperatorDeclarationSyntax syntax,
            bool isNullableAnalysisEnabled,
            BindingDiagnosticBag diagnostics)
        {
            var location = syntax.OperatorToken.GetLocation();

            string name = OperatorFacts.OperatorNameFromDeclaration(syntax);

            if (SyntaxFacts.IsCheckedOperator(name))
            {
                MessageID.IDS_FeatureCheckedUserDefinedOperators.CheckFeatureAvailability(diagnostics, syntax, syntax.CheckedKeyword.GetLocation());
            }
            else if (!syntax.OperatorToken.IsMissing && syntax.CheckedKeyword.IsKind(SyntaxKind.CheckedKeyword))
            {
                diagnostics.Add(ErrorCode.ERR_OperatorCantBeChecked, syntax.CheckedKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxFacts.GetOperatorKind(name)));
            }

            if (name == WellKnownMemberNames.UnsignedRightShiftOperatorName)
            {
                MessageID.IDS_FeatureUnsignedRightShift.CheckFeatureAvailability(diagnostics, syntax, syntax.OperatorToken.GetLocation());
            }

            var interfaceSpecifier = syntax.ExplicitInterfaceSpecifier;

            TypeSymbol explicitInterfaceType;
            name = ExplicitInterfaceHelpers.GetMemberNameAndInterfaceSymbol(bodyBinder, interfaceSpecifier, name, diagnostics, out explicitInterfaceType, aliasQualifierOpt: out _);

            var methodKind = interfaceSpecifier == null
                ? MethodKind.UserDefinedOperator
                : MethodKind.ExplicitInterfaceImplementation;

            return new SourceUserDefinedOperatorSymbol(
                methodKind, containingType, explicitInterfaceType, name, location, syntax, isNullableAnalysisEnabled, diagnostics);
        }

        // NOTE: no need to call WithUnsafeRegionIfNecessary, since the signature
        // is bound lazily using binders from a BinderFactory (which will already include an
        // UnsafeBinder, if necessary).
        private SourceUserDefinedOperatorSymbol(
            MethodKind methodKind,
            SourceMemberContainerTypeSymbol containingType,
            TypeSymbol explicitInterfaceType,
            string name,
            Location location,
            OperatorDeclarationSyntax syntax,
            bool isNullableAnalysisEnabled,
            BindingDiagnosticBag diagnostics) :
            base(
                methodKind,
                explicitInterfaceType,
                name,
                containingType,
                location,
                syntax,
                MakeDeclarationModifiers(methodKind, containingType.IsInterface, syntax, location, diagnostics),
                hasBody: syntax.HasAnyBody(),
                isExpressionBodied: syntax.Body == null && syntax.ExpressionBody != null,
                isIterator: SyntaxFacts.HasYieldOperations(syntax.Body),
                isNullableAnalysisEnabled: isNullableAnalysisEnabled,
                diagnostics)
        {
            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);

            if (IsAbstract || (name != WellKnownMemberNames.EqualityOperatorName && name != WellKnownMemberNames.InequalityOperatorName))
            {
                CheckFeatureAvailabilityAndRuntimeSupport(syntax, location, hasBody: syntax.Body != null || syntax.ExpressionBody != null, diagnostics: diagnostics);
            }
        }

        internal OperatorDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (OperatorDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        protected override int GetParameterCountFromSyntax()
        {
            return GetSyntax().ParameterList.ParameterCount;
        }

        protected override Location ReturnTypeLocation
        {
            get
            {
                return GetSyntax().ReturnType.Location;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }

        internal sealed override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            return OneOrMany.Create(this.GetSyntax().AttributeLists);
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            OperatorDeclarationSyntax declarationSyntax = GetSyntax();
            return MakeParametersAndBindReturnType(declarationSyntax, declarationSyntax.ReturnType, diagnostics);
        }
    }
}
