// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceUserDefinedConversionSymbol : SourceUserDefinedOperatorSymbolBase
    {
        public static SourceUserDefinedConversionSymbol CreateUserDefinedConversionSymbol(
            SourceMemberContainerTypeSymbol containingType,
            Binder bodyBinder,
            ConversionOperatorDeclarationSyntax syntax,
            bool isNullableAnalysisEnabled,
            BindingDiagnosticBag diagnostics)
        {
            // Dev11 includes the explicit/implicit keyword, but we don't have a good way to include
            // Narrowing/Widening in VB and we want the languages to be consistent.
            var location = syntax.Type.Location;
            string name = OperatorFacts.OperatorNameFromDeclaration(syntax);

            if (name == WellKnownMemberNames.CheckedExplicitConversionName)
            {
                MessageID.IDS_FeatureCheckedUserDefinedOperators.CheckFeatureAvailability(diagnostics, syntax.CheckedKeyword);
            }
            else if (syntax.CheckedKeyword.IsKind(SyntaxKind.CheckedKeyword))
            {
                diagnostics.Add(ErrorCode.ERR_ImplicitConversionOperatorCantBeChecked, syntax.CheckedKeyword.GetLocation());
            }

            var interfaceSpecifier = syntax.ExplicitInterfaceSpecifier;

            TypeSymbol explicitInterfaceType;
            name = ExplicitInterfaceHelpers.GetMemberNameAndInterfaceSymbol(bodyBinder, interfaceSpecifier, name, diagnostics, out explicitInterfaceType, aliasQualifierOpt: out _);

            var methodKind = interfaceSpecifier == null
                ? MethodKind.Conversion
                : MethodKind.ExplicitInterfaceImplementation;

            return new SourceUserDefinedConversionSymbol(
                methodKind, containingType, explicitInterfaceType, name, location, syntax, isNullableAnalysisEnabled, diagnostics);
        }

        // NOTE: no need to call WithUnsafeRegionIfNecessary, since the signature
        // is bound lazily using binders from a BinderFactory (which will already include an
        // UnsafeBinder, if necessary).
        private SourceUserDefinedConversionSymbol(
            MethodKind methodKind,
            SourceMemberContainerTypeSymbol containingType,
            TypeSymbol explicitInterfaceType,
            string name,
            Location location,
            ConversionOperatorDeclarationSyntax syntax,
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
                hasAnyBody: syntax.HasAnyBody(),
                isExpressionBodied: syntax.Body == null && syntax.ExpressionBody != null,
                isIterator: SyntaxFacts.HasYieldOperations(syntax.Body),
                isNullableAnalysisEnabled: isNullableAnalysisEnabled,
                diagnostics)
        {
            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);

            if (syntax.ParameterList.Parameters.Count != 1)
            {
                diagnostics.Add(ErrorCode.ERR_OvlUnaryOperatorExpected, syntax.ParameterList.GetLocation());
            }

            if (IsStatic && (IsAbstract || IsVirtual))
            {
                CheckFeatureAvailabilityAndRuntimeSupport(syntax, location, hasBody: syntax.Body != null || syntax.ExpressionBody != null, diagnostics: diagnostics);
            }

            if (syntax.ExplicitInterfaceSpecifier != null)
                MessageID.IDS_FeatureStaticAbstractMembersInInterfaces.CheckFeatureAvailability(diagnostics, syntax.ExplicitInterfaceSpecifier);
        }

        internal ConversionOperatorDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (ConversionOperatorDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        internal override ExecutableCodeBinder TryGetBodyBinder(BinderFactory binderFactoryOpt = null, bool ignoreAccessibility = false)
        {
            return TryGetBodyBinderFromSyntax(binderFactoryOpt, ignoreAccessibility);
        }

        protected override int GetParameterCountFromSyntax()
        {
            return GetSyntax().ParameterList.ParameterCount;
        }

        protected override Location ReturnTypeLocation
        {
            get
            {
                return GetSyntax().Type.Location;
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
            ConversionOperatorDeclarationSyntax declarationSyntax = GetSyntax();
            return MakeParametersAndBindReturnType(declarationSyntax, declarationSyntax.Type, diagnostics);
        }
    }
}
