// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveRedundantElseStatement
{
    using static System.Reflection.Metadata.BlobBuilder;
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseSimpleUsingStatement), Shared]
    internal class RemoveRedundantElseStatementCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoveRedundantElseStatementCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, CSharpAnalyzersResources.Use_simple_using_statement, nameof(CSharpAnalyzersResources.Use_simple_using_statement));
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var elseClause = (ElseClauseSyntax) diagnostics.Single().AdditionalLocations[0].FindNode(cancellationToken);
            var topmostIf = FindTopmostIfIterative(elseClause);
            var block = topmostIf.Parent as BlockSyntax;

            var root = editor.OriginalRoot;
            var updatedRoot = root.ReplaceNode(block, RewriteBlock(block, topmostIf, elseClause));

            editor.ReplaceNode(root, updatedRoot);

            return Task.CompletedTask;
        }

        private IfStatementSyntax FindTopmostIfIterative(ElseClauseSyntax elseClause)
        {
            SyntaxNode node = elseClause;
            while (node.Parent is IfStatementSyntax or ElseClauseSyntax)
            {
                node = node.Parent;
            }

            return node as IfStatementSyntax;
        }

        private BlockSyntax RewriteBlock(BlockSyntax block, IfStatementSyntax topmostIf, ElseClauseSyntax elseClause)
        {
            var topmostIfIdx = block.Statements.IndexOf(topmostIf);

            var newTopMostIf = topmostIf.RemoveNode(elseClause, SyntaxRemoveOptions.KeepEndOfLine);
            var newBlock = block.ReplaceNode(topmostIf, newTopMostIf);

            return newBlock.WithStatements(
                newBlock.Statements.InsertRange(topmostIfIdx + 1, Expand(elseClause))
            );
        }

        private ImmutableArray<StatementSyntax> Expand(ElseClauseSyntax elseClause)
        {
            using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var result);

            switch (elseClause.Statement)
            {
                case BlockSyntax block:
                    result.AddRange(block.Statements);
                    break;

                case StatementSyntax anythingElse:
                    result.Add(anythingElse);
                    break;
            }

            return result
                .Select(statement => statement.WithAdditionalAnnotations(Formatter.Annotation))
                .AsImmutable();
        }

        private static SyntaxTriviaList Expand(ArrayBuilder<StatementSyntax> result, ElseClauseSyntax usingStatement)
        {
            switch (usingStatement.Statement)
            {
                case BlockSyntax blockSyntax:
                    var statements = blockSyntax.Statements;
                    if (!statements.Any())
                    {
                        return blockSyntax.CloseBraceToken.LeadingTrivia;
                    }

                    var openBraceLeadingTrivia = blockSyntax.OpenBraceToken.LeadingTrivia;
                    var openBraceTrailingTrivia = blockSyntax.OpenBraceToken.TrailingTrivia;

                    if (openBraceTrailingTrivia.Any(t => t.IsSingleOrMultiLineComment()))
                    {
                        var newFirstStatement = statements.First()
                            .WithPrependedLeadingTrivia(openBraceTrailingTrivia);
                        statements = statements.Replace(statements.First(), newFirstStatement);
                    }

                    if (openBraceLeadingTrivia.Any(t => t.IsSingleOrMultiLineComment() || t.IsDirective))
                    {
                        var newFirstStatement = statements.First()
                            .WithPrependedLeadingTrivia(openBraceLeadingTrivia);
                        statements = statements.Replace(statements.First(), newFirstStatement);
                    }

                    var closeBraceTrailingTrivia = blockSyntax.CloseBraceToken.TrailingTrivia;
                    if (closeBraceTrailingTrivia.Any(t => t.IsSingleOrMultiLineComment()))
                    {
                        var newLastStatement = statements.Last()
                            .WithAppendedTrailingTrivia(closeBraceTrailingTrivia);
                        statements = statements.Replace(statements.Last(), newLastStatement);
                    }

                    // if we hit a block, then inline all the statements in the block into
                    // the final list of statements.
                    result.AddRange(statements);
                    return blockSyntax.CloseBraceToken.LeadingTrivia;
                case StatementSyntax anythingElse:
                    // Any other statement should be untouched and just be placed next in the
                    // final list of statements.
                    result.Add(anythingElse);
                    return default;
            }

            return default;
        }
    }
}
