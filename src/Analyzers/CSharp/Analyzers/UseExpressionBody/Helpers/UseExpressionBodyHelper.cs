// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal abstract class UseExpressionBodyHelper
    {
        public abstract Option2<CodeStyleOption2<ExpressionBodyPreference>> Option { get; }
        public abstract LocalizableString UseExpressionBodyTitle { get; }
        public abstract LocalizableString UseBlockBodyTitle { get; }
        public abstract string DiagnosticId { get; }
        public abstract ImmutableArray<SyntaxKind> SyntaxKinds { get; }

        public abstract BlockSyntax GetBody(SyntaxNode declaration);
        public abstract ArrowExpressionClauseSyntax GetExpressionBody(SyntaxNode declaration);

        public abstract bool CanOfferUseExpressionBody(OptionSet optionSet, SyntaxNode declaration, bool forAnalyzer);
        public abstract (bool canOffer, bool fixesError) CanOfferUseBlockBody(OptionSet optionSet, SyntaxNode declaration, bool forAnalyzer);
        public abstract SyntaxNode Update(SemanticModel semanticModel, SyntaxNode declaration, bool useExpressionBody);

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
