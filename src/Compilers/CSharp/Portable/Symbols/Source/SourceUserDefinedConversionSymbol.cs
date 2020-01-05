// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
                diagnostics)
        {
            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);

            if (syntax.ParameterList.Parameters.Count != 1)
            {
                diagnostics.Add(ErrorCode.ERR_OvlUnaryOperatorExpected, syntax.ParameterList.GetLocation());
            }
        }

        internal new ConversionOperatorDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (ConversionOperatorDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        protected override ParameterListSyntax ParameterListSyntax
        {
            get
            {
                return GetSyntax().ParameterList;
            }
        }

        protected override TypeSyntax ReturnTypeSyntax
        {
            get
            {
                return GetSyntax().Type;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }
    }
}
