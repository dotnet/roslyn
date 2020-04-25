// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers.WrapStatements
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpWrapStatementsDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(
            nameof(RoslynDiagnosticsAnalyzersResources.WrapStatementsMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.WrapStatementsRuleId,
            s_localizableMessage,
            s_localizableMessage,
            DiagnosticCategory.RoslynDiagnosticsMaintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableMessage,
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.Telemetry);

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

            // Report on the topmost statement that has an issue.  No need to recurse further at that point. Note: the
            // fixer will fix up all statements, but we don't want to clutter things with lots of diagnostics on the
            // same line.
            if (node is StatementSyntax statement &&
                CheckStatementSyntax(context, statement))
            {
                return;
            }

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    Recurse(context, child.AsNode(), cancellationToken);
            }
        }

        private static bool CheckStatementSyntax(
            SyntaxTreeAnalysisContext context,
            StatementSyntax statement)
        {
            if (!StatementNeedsWrapping(statement))
                return false;

            // Attribute '{0}' comes from a different version of MEF than the export attribute on '{1}'
            var additionalLocations = ImmutableArray.Create(statement.GetLocation());
            var diagnostic = Diagnostic.Create(
                Rule,
                statement.GetFirstToken().GetLocation(),
                additionalLocations: additionalLocations);
            context.ReportDiagnostic(diagnostic);
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

            if (parent.IsKind(SyntaxKind.Block))
            {
                // Blocks can be on a single line if parented by a member/accessor/lambda.
                var blockParent = parent.Parent;
                if (blockParent is MemberDeclarationSyntax ||
                    blockParent is AccessorDeclarationSyntax ||
                    blockParent is AnonymousFunctionExpressionSyntax)
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