// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Differencing;
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
            => new CSharpWrapStatementsFixAllProvider();

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
            var newRoot = await FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(newRoot);
        }

        public static async Task<SyntaxNode> FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var endOfLine = options.GetOption(FormattingOptions.NewLine);
            var endOfLineTrivia = SyntaxFactory.ElasticEndOfLine(endOfLine);

            foreach (var diagnostic in diagnostics)
                FixOne(editor, diagnostic, endOfLineTrivia, cancellationToken);

            return editor.GetChangedRoot();
        }

        private static void FixOne(
            SyntaxEditor editor,
            Diagnostic diagnostic,
            SyntaxTrivia endOfLineTrivia,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var root = editor.OriginalRoot;
            var node = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan);
            if (!(node is StatementSyntax startStatement))
            {
                Debug.Fail("Couldn't find statement in fixer");
                return;
            }

            // fixup this statement and all nested statements that have an issue.
            var descendentStatements = startStatement.DescendantNodesAndSelf().OfType<StatementSyntax>();
            var badStatements = descendentStatements.Where(s => CSharpWrapStatementsDiagnosticAnalyzer.StatementNeedsWrapping(s));

            // Walk from lower statements to higher so the higher up changes see the changes below.
            foreach (var badStatement in badStatements.OrderByDescending(s => s.SpanStart))
            {
                editor.ReplaceNode(
                    badStatement,
                    (currentBadStatement, _) =>
                    {
                        // Ensure a newline between the statement and the statement that preceded it.
                        var updatedStatement = AddLeadingTrivia(currentBadStatement, endOfLineTrivia);

                        // Ensure that if we wrap an empty block that the trailing brace is on a new line as well.
                        if (updatedStatement is BlockSyntax blockSyntax &&
                            blockSyntax.Statements.Count == 0)
                        {
                            updatedStatement = blockSyntax.WithCloseBraceToken(
                                AddLeadingTrivia(blockSyntax.CloseBraceToken, SyntaxFactory.ElasticMarker));
                        }

                        return updatedStatement;
                    });
            }

            // Now walk up all our containing blocks ensuring that they wrap over multiple lines
            var ancestorBlocks = startStatement.AncestorsAndSelf().OfType<BlockSyntax>();
            foreach (var block in ancestorBlocks)
            {
                var openBrace = block.OpenBraceToken;
                var previousToken = openBrace.GetPreviousToken();

                editor.ReplaceNode(
                    block,
                    (current, _) =>
                    {
                        // If the block's open { is not already on a new line, add an elastic marker so it will be placed there.
                        var currentBlock = (BlockSyntax)current;
                        if (!CSharpWrapStatementsDiagnosticAnalyzer.ContainsEndOfLineBetween(previousToken, openBrace))
                        {
                            currentBlock = currentBlock.WithOpenBraceToken(
                                AddLeadingTrivia(currentBlock.OpenBraceToken, SyntaxFactory.ElasticMarker));
                        }

                        return currentBlock.WithCloseBraceToken(
                            AddLeadingTrivia(currentBlock.CloseBraceToken, SyntaxFactory.ElasticMarker));
                    });
            }
        }

        private static SyntaxNode AddLeadingTrivia(SyntaxNode node, SyntaxTrivia trivia)
            => node.WithLeadingTrivia(node.GetLeadingTrivia().Insert(0, trivia));

        private static SyntaxToken AddLeadingTrivia(SyntaxToken token, SyntaxTrivia trivia)
            => token.WithLeadingTrivia(token.LeadingTrivia.Insert(0, trivia));

    }
}
