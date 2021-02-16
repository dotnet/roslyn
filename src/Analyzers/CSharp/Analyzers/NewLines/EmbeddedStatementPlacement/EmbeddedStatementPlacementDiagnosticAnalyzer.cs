﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.NewLines.EmbeddedStatementPlacement
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class EmbeddedStatementPlacementDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public EmbeddedStatementPlacementDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.EmbeddedStatementPlacementDiagnosticId,
                   EnforceOnBuildValues.EmbeddedStatementPlacement,
                   CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(
                       nameof(CSharpAnalyzersResources.Embedded_statements_must_be_on_their_own_line), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeTree);

        private void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var option = context.GetOption(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine);
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

            // Report on the topmost statement that has an issue.  No need to recurse further at that point. Note: the
            // fixer will fix up all statements, but we don't want to clutter things with lots of diagnostics on the
            // same line.
            if (node is StatementSyntax statement &&
                CheckStatementSyntax(context, severity, statement))
            {
                return;
            }

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    Recurse(context, severity, child.AsNode()!);
            }
        }

        private bool CheckStatementSyntax(SyntaxTreeAnalysisContext context, ReportDiagnostic severity, StatementSyntax statement)
        {
            if (!StatementNeedsWrapping(statement))
                return false;

            var additionalLocations = ImmutableArray.Create(statement.GetLocation());
            context.ReportDiagnostic(DiagnosticHelper.Create(
                this.Descriptor,
                statement.GetFirstToken().GetLocation(),
                severity,
                additionalLocations,
                properties: null));
            return true;
        }

        public static bool StatementNeedsWrapping(StatementSyntax statement)
        {
            // Statement has to be parented by another statement (or an else-clause) to count.
            var parent = statement.Parent;
            var parentIsElseClause = parent.IsKind(SyntaxKind.ElseClause);

            if (!(parent is StatementSyntax || parentIsElseClause))
                return false;

            // `else if` is always allowed.
            if (statement.IsKind(SyntaxKind.IfStatement) && parentIsElseClause)
                return false;

            var statementStartToken = statement.GetFirstToken();

            // we have to have a newline between the start of this statement and the previous statement.
            if (ContainsEndOfLineBetween(statementStartToken.GetPreviousToken(), statementStartToken))
                return false;

            // Looks like a statement that might need wrapping.  However, we do suppress wrapping for a few well known
            // acceptable cases.

            if (parent.IsKind(SyntaxKind.Block))
            {
                // Blocks can be on a single line if parented by a member/accessor/lambda.
                // And if they only contain a single statement at most within them.
                var blockParent = parent.Parent;
                if (blockParent is MemberDeclarationSyntax or
                    AccessorDeclarationSyntax or
                    AnonymousFunctionExpressionSyntax)
                {
                    if (parent.DescendantNodes().OfType<StatementSyntax>().Count() <= 1)
                        return false;
                }
            }

            return true;
        }

        public static bool ContainsEndOfLineBetween(SyntaxToken previous, SyntaxToken next)
            => ContainsEndOfLine(previous.TrailingTrivia) || ContainsEndOfLine(next.LeadingTrivia);

        private static bool ContainsEndOfLine(SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                    return true;
            }

            return false;
        }
    }
}
