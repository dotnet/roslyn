// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseSimpleUsingStatementCodeFixProvider)), Shared]
    internal class UseSimpleUsingStatementCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public UseSimpleUsingStatementCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var topmostUsingStatements = diagnostics.Select(d => (UsingStatementSyntax)d.AdditionalLocations[0].FindNode(cancellationToken)).ToSet();
            var blocks = topmostUsingStatements.Select(u => (BlockSyntax)u.Parent);

            // Process blocks in reverse order so we rewrite from inside-to-outside with nested
            // usings.
            var root = editor.OriginalRoot;
            var updatedRoot = root.ReplaceNodes(
                blocks.OrderByDescending(b => b.SpanStart),
                (original, current) => RewriteBlock(original, current, topmostUsingStatements));

            editor.ReplaceNode(root, updatedRoot);

            return Task.CompletedTask;
        }

        private static SyntaxNode RewriteBlock(
            BlockSyntax originalBlock, BlockSyntax currentBlock,
            ISet<UsingStatementSyntax> topmostUsingStatements)
        {
            if (originalBlock.Statements.Count == currentBlock.Statements.Count)
            {
                var statementToUpdateIndex = originalBlock.Statements.IndexOf(s => topmostUsingStatements.Contains(s));
                var statementToUpdate = currentBlock.Statements[statementToUpdateIndex];

                if (statementToUpdate is UsingStatementSyntax usingStatement &&
                    usingStatement.Declaration != null)
                {
                    var updatedStatements = currentBlock.Statements.ReplaceRange(
                        statementToUpdate,
                        Expand(usingStatement));
                    return currentBlock.WithStatements(updatedStatements);
                }
            }

            return currentBlock;
        }

        private static IEnumerable<StatementSyntax> Expand(UsingStatementSyntax usingStatement)
        {
            var result = new List<StatementSyntax>();
            var remainingTrivia = Expand(result, usingStatement);

            if (remainingTrivia.Any(t => t.IsSingleOrMultiLineComment() || t.IsDirective))
            {
                var lastStatement = result[result.Count - 1];
                result[result.Count - 1] = lastStatement.WithAppendedTrailingTrivia(
                    remainingTrivia.Insert(0, CSharpSyntaxFacts.Instance.ElasticCarriageReturnLineFeed));
            }

            for (int i = 0, n = result.Count; i < n; i++)
            {
                result[i] = result[i].WithAdditionalAnnotations(Formatter.Annotation);
            }

            return result;
        }

        private static SyntaxTriviaList Expand(List<StatementSyntax> result, UsingStatementSyntax usingStatement)
        {
            // First, convert the using-statement into a using-declaration.
            result.Add(Convert(usingStatement));
            switch (usingStatement.Statement)
            {
                case BlockSyntax blockSyntax:
                    // if we hit a block, then inline all the statements in the block into
                    // the final list of statements.
                    result.AddRange(blockSyntax.Statements);
                    return blockSyntax.CloseBraceToken.LeadingTrivia;
                case UsingStatementSyntax childUsing when childUsing.Declaration != null:
                    // If we have a directly nested using-statement, then recurse into that
                    // expanding it and handle its children as well.
                    return Expand(result, childUsing);
                case StatementSyntax anythingElse:
                    // Any other statement should be untouched and just be placed next in the
                    // final list of statements.
                    result.Add(anythingElse);
                    return default;
            }
            return default;
        }

        private static LocalDeclarationStatementSyntax Convert(UsingStatementSyntax usingStatement)
        {
            return LocalDeclarationStatement(
                usingStatement.AwaitKeyword,
                usingStatement.UsingKeyword,
                modifiers: default,
                usingStatement.Declaration,
                Token(SyntaxKind.SemicolonToken)).WithTrailingTrivia(ElasticCarriageReturnLineFeed);
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Use_simple_using_statement, createChangedDocument, CSharpAnalyzersResources.Use_simple_using_statement)
            {
            }
        }
    }
}
