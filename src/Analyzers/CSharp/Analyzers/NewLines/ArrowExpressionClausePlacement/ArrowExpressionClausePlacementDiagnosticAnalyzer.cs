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

namespace Microsoft.CodeAnalysis.CSharp.NewLines.ArrowExpressionClausePlacement
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class ArrowExpressionClausePlacementDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public ArrowExpressionClausePlacementDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.ArrowExpressionClausePlacementDiagnosticId,
                   EnforceOnBuildValues.ArrowExpressionClausePlacement,
                   CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause,
                   new LocalizableResourceString(
                       nameof(CSharpAnalyzersResources.Blank_line_not_allowed_after_arrow_expression_clause_token), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeTree);

        private void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var option = context.GetCSharpAnalyzerOptions().AllowBlankLineAfterTokenInArrowExpressionClause;
            if (option.Value)
                return;

            Recurse(context, option.Notification.Severity, context.GetAnalysisRoot(findInTrivia: false));
        }

        private void Recurse(SyntaxTreeAnalysisContext context, ReportDiagnostic severity, SyntaxNode node)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (node is ArrowExpressionClauseSyntax arrowExpressionClause)
                ProcessArrowExpressionClause(context, severity, arrowExpressionClause);

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (!context.ShouldAnalyzeSpan(child.Span))
                    continue;

                if (child.IsNode)
                    Recurse(context, severity, child.AsNode()!);
            }
        }

        private void ProcessArrowExpressionClause(
            SyntaxTreeAnalysisContext context, ReportDiagnostic severity, ArrowExpressionClauseSyntax arrowExpressionClause)
        {
            // get
            //     => 1 + 2;
            //
            // Never looks good.  So we don't process in that case.
            if (arrowExpressionClause.Parent is AccessorDeclarationSyntax)
                return;

            // Don't bother analyzing nodes that have syntax errors in them.
            if (arrowExpressionClause.GetDiagnostics().Any(static d => d.Severity == DiagnosticSeverity.Error))
                return;

            if (IsOk(arrowExpressionClause.ArrowToken))
                return;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                this.Descriptor,
                arrowExpressionClause.ArrowToken.GetLocation(),
                severity,
                additionalLocations: null,
                properties: null));

            return;

            static bool IsOk(SyntaxToken token)
            {
                // Only care about tokens that are actually present.  Missing ones mean the code is incomplete and we
                // don't want to complain about those.
                if (token.IsMissing)
                    return true;

                // Arrow has to be at the end of the line for us to actually care.
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

                // Not ok.
                return false;
            }
        }
    }
}
