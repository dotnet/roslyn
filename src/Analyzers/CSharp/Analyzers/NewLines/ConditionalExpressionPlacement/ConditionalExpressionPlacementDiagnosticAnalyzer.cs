// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.NewLines.ConditionalExpressionPlacement;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class ConditionalExpressionPlacementDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public ConditionalExpressionPlacementDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.ConditionalExpressionPlacementDiagnosticId,
               EnforceOnBuildValues.ConditionalExpressionPlacement,
               CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression,
               new LocalizableResourceString(
                   nameof(CSharpAnalyzersResources.Blank_line_not_allowed_after_conditional_expression_token), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        // Note: we do not use semantics.  But this means we only reanalyze a method body when it is edited.
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(ProcessConditionalExpression, SyntaxKind.ConditionalExpression);

    private void ProcessConditionalExpression(SyntaxNodeAnalysisContext context)
    {
        var option = context.GetCSharpAnalyzerOptions().AllowBlankLineAfterTokenInConditionalExpression;
        if (option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var conditionalExpression = (ConditionalExpressionSyntax)context.Node;
        // Don't bother analyzing nodes whose parent have syntax errors in them.
        if (conditionalExpression.GetRequiredParent().GetDiagnostics().Any(static d => d.Severity == DiagnosticSeverity.Error))
            return;

        // Only if both tokens are not ok do we report an error.  For example, the following is legal:
        //
        //  var x =
        //      goo ? bar :
        //      baz ? quux : ztesh;
        //
        // despite one colon being at the end of the line.
        if (IsOk(conditionalExpression.QuestionToken) ||
            IsOk(conditionalExpression.ColonToken))
        {
            return;
        }

        context.ReportDiagnostic(DiagnosticHelper.Create(
            this.Descriptor,
            conditionalExpression.QuestionToken.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: null,
            properties: null));

        return;

        static bool IsOk(SyntaxToken token)
        {
            // Only care about tokens that are actually present.  Missing ones mean the code is incomplete and we
            // don't want to complain about those.
            if (token.IsMissing)
                return true;

            // question/colon has to be at the end of the line for us to actually care.
            if (token.TrailingTrivia is not [.., SyntaxTrivia(SyntaxKind.EndOfLineTrivia)])
                return true;

            // if the next token has pp-directives on it, we don't want to move the token around as we may screw
            // things up in different pp-contexts.
            var nextToken = token.GetNextToken();
            if (nextToken == default)
                return true;

            if (nextToken.LeadingTrivia.Any(static t => t.Kind() is
                    SyntaxKind.IfDirectiveTrivia or SyntaxKind.ElseDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia or SyntaxKind.EndIfDirectiveTrivia))
            {
                return true;
            }

            // Not ok.  Report an error if the other token is not ok as well.
            return false;
        }
    }
}
