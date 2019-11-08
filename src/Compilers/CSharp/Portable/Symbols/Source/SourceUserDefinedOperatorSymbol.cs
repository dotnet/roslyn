// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceUserDefinedOperatorSymbol : SourceUserDefinedOperatorSymbolBase
    {
        public static SourceUserDefinedOperatorSymbol CreateUserDefinedOperatorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            OperatorDeclarationSyntax syntax,
            DiagnosticBag diagnostics)
        {
            var location = syntax.OperatorToken.GetLocation();

            string name = OperatorFacts.OperatorNameFromDeclaration(syntax);

            return new SourceUserDefinedOperatorSymbol(
                containingType, name, location, syntax, diagnostics,
                syntax is {
                    Body: null, ExpressionBody: {
                    }
                });
        }

        // NOTE: no need to call WithUnsafeRegionIfNecessary, since the signature
        // is bound lazily using binders from a BinderFactory (which will already include an
        // UnsafeBinder, if necessary).
        private SourceUserDefinedOperatorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            string name,
            Location location,
            OperatorDeclarationSyntax syntax,
            DiagnosticBag diagnostics,
            bool isExpressionBodied) :
            base(
                MethodKind.UserDefinedOperator,
                name,
                containingType,
                location,
                syntax,
                diagnostics)
        {
            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);

            if (name != WellKnownMemberNames.EqualityOperatorName && name != WellKnownMemberNames.InequalityOperatorName)
            {
                CheckFeatureAvailabilityAndRuntimeSupport(syntax, location, hasBody: syntax.Body != null || syntax.ExpressionBody != null, diagnostics: diagnostics);
            }
        }

        internal new OperatorDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (OperatorDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
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
                return GetSyntax().ReturnType;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }
    }
}
