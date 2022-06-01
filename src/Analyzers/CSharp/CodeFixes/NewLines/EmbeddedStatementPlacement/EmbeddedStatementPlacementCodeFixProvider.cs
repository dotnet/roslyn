// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.NewLines.EmbeddedStatementPlacement
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.EmbeddedStatementPlacement), Shared]
    internal sealed class EmbeddedStatementPlacementCodeFixProvider : CodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EmbeddedStatementPlacementCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.EmbeddedStatementPlacementDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                CodeAction.Create(
                    CSharpCodeFixesResources.Place_statement_on_following_line,
                    c => UpdateDocumentAsync(document, diagnostic, c),
                    nameof(CSharpCodeFixesResources.Place_statement_on_following_line)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        private static Task<Document> UpdateDocumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
            => FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken);

        public static async Task<Document> FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace.Services);

#if CODE_STYLE
            var options = document.Project.AnalyzerOptions.GetAnalyzerOptionSet(editor.OriginalRoot.SyntaxTree, cancellationToken);
#else
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
#endif

            var endOfLineTrivia = SyntaxFactory.ElasticEndOfLine(options.GetOption(FormattingOptions2.NewLine, LanguageNames.CSharp));

            foreach (var diagnostic in diagnostics)
                FixOne(editor, diagnostic, endOfLineTrivia, cancellationToken);

            return document.WithSyntaxRoot(editor.GetChangedRoot());
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
            if (node is not StatementSyntax startStatement)
            {
                Debug.Fail("Couldn't find statement in fixer");
                return;
            }

            // fixup this statement and all nested statements that have an issue.
            var descendentStatements = startStatement.DescendantNodesAndSelf().OfType<StatementSyntax>();
            var badStatements = descendentStatements.Where(s => EmbeddedStatementPlacementDiagnosticAnalyzer.StatementNeedsWrapping(s));

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
                        if (!EmbeddedStatementPlacementDiagnosticAnalyzer.ContainsEndOfLineBetween(previousToken, openBrace))
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

        public override FixAllProvider GetFixAllProvider()
            => FixAllProvider.Create(
                async (context, document, diagnostics) => await FixAllAsync(document, diagnostics, context.CancellationToken).ConfigureAwait(false));
    }
}
