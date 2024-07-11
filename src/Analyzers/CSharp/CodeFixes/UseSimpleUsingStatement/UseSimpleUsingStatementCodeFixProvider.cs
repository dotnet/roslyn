// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

namespace Microsoft.CodeAnalysis.CSharp.UseSimpleUsingStatement;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseSimpleUsingStatement), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class UseSimpleUsingStatementCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Use_simple_using_statement, nameof(CSharpAnalyzersResources.Use_simple_using_statement));
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

    private static BlockSyntax RewriteBlock(
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

    private static ImmutableArray<StatementSyntax> Expand(UsingStatementSyntax usingStatement)
    {
        using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var result);
        var remainingTrivia = Expand(result, usingStatement);

        if (remainingTrivia.Any(t => t.IsSingleOrMultiLineComment() || t.IsDirective))
        {
            var lastStatement = result[^1];
            result[^1] = lastStatement.WithAppendedTrailingTrivia(
                remainingTrivia.Insert(0, CSharpSyntaxFacts.Instance.ElasticCarriageReturnLineFeed));
        }

        for (int i = 0, n = result.Count; i < n; i++)
            result[i] = result[i].WithAdditionalAnnotations(Formatter.Annotation);

        return result.ToImmutableAndClear();
    }

    private static SyntaxTriviaList Expand(ArrayBuilder<StatementSyntax> result, UsingStatementSyntax usingStatement)
    {
        // First, convert the using-statement into a using-declaration.
        result.Add(Convert(usingStatement));
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
                var usingHasEndOfLineTrivia = usingStatement.CloseParenToken.TrailingTrivia
                    .Any(SyntaxKind.EndOfLineTrivia);
                if (!usingHasEndOfLineTrivia)
                {
                    var newFirstStatement = statements.First()
                        .WithPrependedLeadingTrivia(ElasticCarriageReturnLineFeed);
                    statements = statements.Replace(statements.First(), newFirstStatement);
                }

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
            usingStatement.UsingKeyword.WithAppendedTrailingTrivia(ElasticMarker),
            modifiers: default,
            usingStatement.Declaration,
            SemicolonToken).WithTrailingTrivia(usingStatement.CloseParenToken.TrailingTrivia);
    }
}
