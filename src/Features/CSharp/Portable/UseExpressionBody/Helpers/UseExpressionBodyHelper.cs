// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal abstract class UseExpressionBodyHelper
    {
        public abstract Option<CodeStyleOption<ExpressionBodyPreference>> Option { get; }
        public abstract LocalizableString UseExpressionBodyTitle { get; }
        public abstract LocalizableString UseBlockBodyTitle { get; }
        public abstract string DiagnosticId { get; }
        public abstract ImmutableArray<SyntaxKind> SyntaxKinds { get; }

        public abstract BlockSyntax GetBody(SyntaxNode declaration);
        public abstract ArrowExpressionClauseSyntax GetExpressionBody(SyntaxNode declaration);

        public abstract bool CanOfferUseExpressionBody(OptionSet optionSet, SyntaxNode declaration, bool forAnalyzer);
        public abstract (bool canOffer, bool fixesError) CanOfferUseBlockBody(OptionSet optionSet, SyntaxNode declaration, bool forAnalyzer);
        public abstract SyntaxNode Update(SemanticModel semanticModel, SyntaxNode declaration, OptionSet options, ParseOptions parseOptions, bool useExpressionBody);

        public abstract Location GetDiagnosticLocation(SyntaxNode declaration);

        public static readonly ImmutableArray<UseExpressionBodyHelper> Helpers =
            ImmutableArray.Create<UseExpressionBodyHelper>(
                UseExpressionBodyForConstructorsHelper.Instance,
                UseExpressionBodyForConversionOperatorsHelper.Instance,
                UseExpressionBodyForIndexersHelper.Instance,
                UseExpressionBodyForMethodsHelper.Instance,
                UseExpressionBodyForOperatorsHelper.Instance,
                UseExpressionBodyForPropertiesHelper.Instance,
                UseExpressionBodyForAccessorsHelper.Instance,
                UseExpressionBodyForLocalFunctionHelper.Instance);
    }
}
