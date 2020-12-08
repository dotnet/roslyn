// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable disable warnings

using System.Collections.Immutable;
using System.Threading;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Diagnostics.Analyzers.BlankLines
{
    public abstract class AbstractBlankLinesDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(
            nameof(RoslynDiagnosticsAnalyzersResources.BlankLinesTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(
            nameof(RoslynDiagnosticsAnalyzersResources.BlankLinesMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.BlankLinesRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.RoslynDiagnosticsMaintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.Telemetry);

        protected abstract bool IsEndOfLine(SyntaxTrivia trivia);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxTreeAction(AnalyzeTree);
        }

        private void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var tree = context.Tree;
            var cancellationToken = context.CancellationToken;
            var root = tree.GetRoot(cancellationToken);

            Recurse(context, root, cancellationToken);
        }

        private void Recurse(
            SyntaxTreeAnalysisContext context,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Don't bother analyzing nodes that have syntax errors in them.
            if (node.ContainsDiagnostics)
                return;

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    Recurse(context, child.AsNode(), cancellationToken);
                else if (child.IsToken)
                    CheckToken(context, child.AsToken());
            }
        }

        private void CheckToken(
            SyntaxTreeAnalysisContext context,
            SyntaxToken token)
        {
            if (token.ContainsDiagnostics)
                return;

            if (!ContainsMultipleBlankLines(token, out var badTrivia))
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                Location.Create(badTrivia.SyntaxTree, new TextSpan(badTrivia.SpanStart, 0)),
                additionalLocations: ImmutableArray.Create(token.GetLocation())));
        }

        private bool ContainsMultipleBlankLines(SyntaxToken token, out SyntaxTrivia firstBadTrivia)
        {
            var leadingTrivia = token.LeadingTrivia;
            for (int i = 0; i < leadingTrivia.Count; i++)
            {
                if (IsEndOfLine(leadingTrivia, i) &&
                    IsEndOfLine(leadingTrivia, i + 1))
                {
                    // Three cases that end up with two blank lines.
                    //
                    // 1. the token starts with two newlines.  This is definitely something to clean up.
                    // 2. we have two newlines after structured trivia (which itself ends with an newline).
                    // 3. we have three newlines (following non-structured trivia).

                    if (i == 0 ||
                        leadingTrivia[i - 1].HasStructure)
                    {
                        firstBadTrivia = leadingTrivia[i];
                        return true;
                    }

                    if (IsEndOfLine(leadingTrivia, i + 2))
                    {
                        // Report on the second newline.  This is for cases like:
                        //
                        //      // comment
                        //
                        //
                        //      public
                        //
                        // The first newline follows the comment.  But we want to report the issue on the start of the
                        // next line.
                        firstBadTrivia = leadingTrivia[i + 1];
                        return true;
                    }
                }
            }

            firstBadTrivia = default;
            return false;
        }

        private bool IsEndOfLine(SyntaxTriviaList triviaList, int index)
        {
            if (index >= triviaList.Count)
                return false;

            var trivia = triviaList[index];
            return IsEndOfLine(trivia);
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

            if (parent.IsKind(SyntaxKind.Block))
            {
                // Blocks can be on a single line if parented by a member/accessor/lambda.
                var blockParent = parent.Parent;
                if (blockParent is MemberDeclarationSyntax or
                    AccessorDeclarationSyntax or
                    AnonymousFunctionExpressionSyntax)
                {
                    return false;
                }
            }

            var statementStartToken = statement.GetFirstToken();
            var previousToken = statementStartToken.GetPreviousToken();

            // we have to have a newline between the start of this statement and the previous statement.
            if (ContainsEndOfLine(previousToken.TrailingTrivia) ||
                ContainsEndOfLine(statementStartToken.LeadingTrivia))
            {
                return false;
            }

            return true;
        }

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