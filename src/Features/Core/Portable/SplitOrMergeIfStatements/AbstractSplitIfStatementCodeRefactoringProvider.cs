// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements;

internal abstract class AbstractSplitIfStatementCodeRefactoringProvider : CodeRefactoringProvider
{
    protected abstract int GetLogicalExpressionKind(ISyntaxKindsService syntaxKinds);

    protected abstract CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText);

    protected abstract ValueTask<SyntaxNode> GetChangedRootAsync(
        Document document,
        SyntaxNode root,
        SyntaxNode ifOrElseIf,
        SyntaxNode leftCondition,
        SyntaxNode rightCondition,
        CancellationToken cancellationToken);

    public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(textSpan.Start);

        if (textSpan.Length > 0 &&
            textSpan != token.Span)
        {
            return;
        }

        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
        var syntaxKinds = document.GetLanguageService<ISyntaxKindsService>();
        var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

        if (IsPartOfBinaryExpressionChain(token, GetLogicalExpressionKind(syntaxKinds), out var rootExpression) &&
            ifGenerator.IsCondition(rootExpression, out var ifOrElseIf))
        {
            context.RegisterRefactoring(
                CreateCodeAction(
                    c => RefactorAsync(document, token.Span, ifOrElseIf.Span, c),
                    syntaxFacts.GetText(syntaxKinds.IfKeyword)),
                ifOrElseIf.Span);
        }
    }

    private async Task<Document> RefactorAsync(Document document, TextSpan tokenSpan, TextSpan ifOrElseIfSpan, CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
        var ifGenerator = document.GetLanguageService<IIfLikeStatementGenerator>();

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var token = root.FindToken(tokenSpan.Start);
        var ifOrElseIf = root.FindNode(ifOrElseIfSpan);

        Debug.Assert(ifGenerator.IsIfOrElseIf(ifOrElseIf));

        var (left, right) = SplitBinaryExpressionChain(token, ifGenerator.GetCondition(ifOrElseIf), syntaxFacts);

        var newRoot = await GetChangedRootAsync(document, root, ifOrElseIf, left, right, cancellationToken).ConfigureAwait(false);
        return document.WithSyntaxRoot(newRoot);
    }

    private static bool IsPartOfBinaryExpressionChain(SyntaxToken token, int syntaxKind, out SyntaxNode rootExpression)
    {
        // Check whether the token is part of a binary expression, and if so,
        // return the topmost binary expression in the chain (e.g. `a && b && c`).

        SyntaxNodeOrToken current = token;

        while (current.Parent?.RawKind == syntaxKind)
        {
            current = current.Parent;
        }

        rootExpression = current.AsNode();
        return current.IsNode;
    }

    private static (SyntaxNode left, SyntaxNode right) SplitBinaryExpressionChain(
        SyntaxToken token, SyntaxNode rootExpression, ISyntaxFactsService syntaxFacts)
    {
        // We have a left-associative binary expression chain, e.g. `a && b && c && d`.
        // Let's say our token is the second `&&` token, between b and c. We'd like to split the chain at this point
        // and build new expressions for the left side and the right side of this token. This will
        // effectively change the associativity from `((a && b) && c) && d` to `(a && b) && (c && d)`.
        // The left side is in the proper shape already and we can build the right side by getting the
        // topmost expression and replacing our parent with our right side. In the example: `(a && b) && c` to `c`.

        syntaxFacts.GetPartsOfBinaryExpression(token.Parent, out var parentLeft, out _, out var parentRight);

        var left = parentLeft;
        var right = rootExpression.ReplaceNode(token.Parent, parentRight);

        return (left, right);
    }
}
