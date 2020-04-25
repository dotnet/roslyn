// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers.WrapStatements
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class CSharpWrapStatementsCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(RoslynDiagnosticIds.WrapStatementsRuleId);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                CodeAction.Create(
                    RoslynDiagnosticsAnalyzersResources.Place_statement_on_following_line,
                    c => UpdateDocumentAsync(document, diagnostic, c),
                    RoslynDiagnosticsAnalyzersResources.Place_statement_on_following_line),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        private static async Task<Document> UpdateDocumentAsync(
            Document document,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan);
            if (!(node is StatementSyntax statement))
            {
                Debug.Fail("Couldn't find statement in fixer");
                return document;
            }

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            // fixup this statement and all nested statements that have an issue.
            var allStatements = statement.DescendantNodesAndSelf().OfType<StatementSyntax>();
            var badStatements = allStatements.Where(s => CSharpWrapStatementsDiagnosticAnalyzer.StatementNeedsWrapping(s));

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var endOfLine = options.GetOption(FormattingOptions.NewLine);
            var endOfLineTrivia = SyntaxFactory.ElasticEndOfLine(endOfLine);

            // var walk up the statements so that higher up changes see the changes below.
            foreach (var badStatement in badStatements.OrderByDescending(s => s.SpanStart))
            {
                editor.ReplaceNode(
                    badStatement,
                    (currentBadStatement, g) =>
                    {
                        // Ensure a newline between the statement and the statement that preceded it.
                        var updatedStatement = AddLeadingTrivia(currentBadStatement, endOfLineTrivia);

                        // Also place an elastic marker at the end of the statement if we're parented by a block to ensure that
                        // the `}` is properly placed.
                        if (badStatement.Parent.IsKind(SyntaxKind.Block))
                            updatedStatement = AddTrailingTrivia(updatedStatement, SyntaxFactory.ElasticMarker);

                        return updatedStatement;
                    });
            }

            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }

        private static SyntaxNode AddLeadingTrivia(SyntaxNode node, SyntaxTrivia trivia)
            => node.WithLeadingTrivia(node.GetLeadingTrivia().Insert(0, trivia));

        private static SyntaxNode AddTrailingTrivia(SyntaxNode node, SyntaxTrivia trivia)
            => node.WithTrailingTrivia(node.GetTrailingTrivia().Add(trivia));
    }
}
