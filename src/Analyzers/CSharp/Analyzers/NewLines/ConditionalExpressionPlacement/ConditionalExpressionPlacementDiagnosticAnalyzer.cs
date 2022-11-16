// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.NewLines.ConditionalExpressionPlacement
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class ConditionalExpressionPlacementDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public ConditionalExpressionPlacementDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.ConditionalExpressionPlacementDiagnosticId,
                   EnforceOnBuildValues.ConsecutiveBracePlacement,
                   CSharpCodeStyleOptions.AllowBlankLineAfterConditionalExpressionToken,
                   new LocalizableResourceString(
                       nameof(CSharpAnalyzersResources.Blank_line_not_allowed_after_conditional_expression_token), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeTree);

        private void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var option = context.GetCSharpAnalyzerOptions().AllowBlankLineAfterConditionalExpressionToken;
            if (option.Value)
                return;

            Recurse(context, option.Notification.Severity, context.Tree.GetRoot(context.CancellationToken));
        }

        private void Recurse(SyntaxTreeAnalysisContext context, ReportDiagnostic severity, SyntaxNode node)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            // Don't bother analyzing nodes that have syntax errors in them.
            if (node.ContainsDiagnostics)
                return;

            if (node is ConditionalExpressionSyntax conditionalExpression)
                ProcessConstructorInitializer(context, severity, conditionalExpression);

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    Recurse(context, severity, child.AsNode()!);
            }
        }

        private void ProcessConstructorInitializer(
            SyntaxTreeAnalysisContext context, ReportDiagnostic severity, ConditionalExpressionSyntax conditionalExpression)
        {
            if (conditionalExpression.QuestionToken.IsMissing ||
                conditionalExpression.ColonToken.IsMissing)
            {
                return;
            }

            if (!IsAtEndOfLine(conditionalExpression.QuestionToken) ||
                !IsAtEndOfLine(conditionalExpression.ColonToken))
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticHelper.Create(
                this.Descriptor,
                conditionalExpression.QuestionToken.GetLocation(),
                severity,
                additionalLocations: null,
                properties: null));
        }

        private static bool IsAtEndOfLine(SyntaxToken token)
            => token.TrailingTrivia is [.., SyntaxTrivia(SyntaxKind.EndOfLineTrivia)];
    }
}
