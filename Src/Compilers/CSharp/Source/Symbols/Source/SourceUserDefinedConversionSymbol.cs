// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            string name = syntax.ImplicitOrExplicitKeyword.IsKind(SyntaxKind.ImplicitKeyword) ?
                WellKnownMemberNames.ImplicitConversionName :
                WellKnownMemberNames.ExplicitConversionName;

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
                syntax.GetReference(),
                syntax.Body.GetReferenceOrNull(),
                syntax.Modifiers,
                diagnostics)
        {
        }

        override protected ParameterListSyntax ParameterListSyntax
        {
            get
            {
                var syntax = (ConversionOperatorDeclarationSyntax)syntaxReference.GetSyntax();
                return syntax.ParameterList;
            }
        }

        override protected TypeSyntax ReturnTypeSyntax
        {
            get
            {
                var syntax = (ConversionOperatorDeclarationSyntax)syntaxReference.GetSyntax();
                return syntax.Type;
            }
        }
    }
}
