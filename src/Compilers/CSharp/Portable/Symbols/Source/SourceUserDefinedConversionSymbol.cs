// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceUserDefinedConversionSymbol : SourceUserDefinedOperatorSymbolBase
    {
        public static SourceUserDefinedConversionSymbol CreateUserDefinedConversionSymbol(
            SourceMemberContainerTypeSymbol containingType,
            ConversionOperatorDeclarationSyntax syntax,
            DiagnosticBag diagnostics)
        {
            // Dev11 includes the explicit/implicit keyword, but we don't have a good way to include
            // Narrowing/Widening in VB and we want the languages to be consistent.
            var location = syntax.Type.Location;
            string name = syntax.ImplicitOrExplicitKeyword.IsKind(SyntaxKind.ImplicitKeyword)
                ? WellKnownMemberNames.ImplicitConversionName
                : WellKnownMemberNames.ExplicitConversionName;

            return new SourceUserDefinedConversionSymbol(
                containingType, name, location, syntax, diagnostics);
        }

        // NOTE: no need to call WithUnsafeRegionIfNecessary, since the signature
        // is bound lazily using binders from a BinderFactory (which will already include an
        // UnsafeBinder, if necessary).
        private SourceUserDefinedConversionSymbol(
            SourceMemberContainerTypeSymbol containingType,
            string name,
            Location location,
            ConversionOperatorDeclarationSyntax syntax,
            DiagnosticBag diagnostics) :
            base(
                MethodKind.Conversion,
                name,
                containingType,
                location,
                syntax,
                MakeDeclarationModifiers(syntax, location, diagnostics),
                hasBody: syntax.HasAnyBody(),
                isExpressionBodied: syntax.Body == null && syntax.ExpressionBody != null,
                isIterator: SyntaxFacts.HasYieldOperations(syntax.Body),
                diagnostics)
        {
            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);

            if (syntax.ParameterList.Parameters.Count != 1)
            {
                diagnostics.Add(ErrorCode.ERR_OvlUnaryOperatorExpected, syntax.ParameterList.GetLocation());
            }
        }

        internal ConversionOperatorDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (ConversionOperatorDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
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

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindReturnType(DiagnosticBag diagnostics)
        {
            ConversionOperatorDeclarationSyntax declarationSyntax = GetSyntax();
            return MakeParametersAndBindReturnType(declarationSyntax, declarationSyntax.Type, diagnostics);
        }
    }
}
