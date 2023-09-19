// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PrivateAnalyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
internal sealed class CSharpUseNamedArgumentsCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(PrivateDiagnosticIds.UseNamedArguments);

    public override FixAllProvider? GetFixAllProvider()
    {
        return FixAllProvider.Create((context, document, diagnostics) => FixAllAsync(document, diagnostics, context.CancellationToken).AsNullable());
    }

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use named argument",
                    cancellationToken => FixAllAsync(context.Document, ImmutableArray.Create(diagnostic), cancellationToken),
                    nameof(CSharpUseNamedArgumentsCodeFixProvider)),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException();

        List<SyntaxNode> nodesToReplace = new();
        Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode, SyntaxNode>> nodeReplacementFunctions = new();
        List<SyntaxToken> tokensToReplace = new();
        Dictionary<SyntaxToken, Func<SyntaxToken, SyntaxToken, SyntaxToken>> tokenReplacementFunctions = new();

        foreach (var diagnostic in diagnostics)
        {
            var parameterName = diagnostic.Properties[CSharpUseNamedArgumentsDiagnosticAnalyzer.ParameterNameKey] ?? throw new InvalidOperationException();
            var commentSpan = new TextSpan(
                int.Parse(diagnostic.Properties[CSharpUseNamedArgumentsDiagnosticAnalyzer.CommentSpanStartKey] ?? throw new InvalidOperationException()),
                int.Parse(diagnostic.Properties[CSharpUseNamedArgumentsDiagnosticAnalyzer.CommentSpanLengthKey] ?? throw new InvalidOperationException()));

            var innerNodeForSpan = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            // Replace the entire argument list
            SyntaxNode nodeToReplace;
            Func<SyntaxNode, SyntaxNode, SyntaxNode> nodeReplacementFunction = (originalNode, rewrittenNode) => rewrittenNode;
            SyntaxToken? tokenToReplace = null;
            Func<SyntaxToken, SyntaxToken, SyntaxToken> tokenReplacementFunction = (originalToken, rewrittenToken) => rewrittenToken;

            if (innerNodeForSpan.FirstAncestorOrSelf<ArgumentSyntax>() is { } argument)
            {
                nodeToReplace = argument ?? throw new InvalidOperationException();
                nodeReplacementFunction = (originalNode, rewrittenNode) =>
                    ((ArgumentSyntax)rewrittenNode).WithoutLeadingTrivia()
                        .WithNameColon(SyntaxFactory.NameColon(parameterName).WithLeadingTrivia(rewrittenNode.GetLeadingTrivia()))
                        .WithAdditionalAnnotations(Formatter.Annotation);
            }
            else if (innerNodeForSpan.FirstAncestorOrSelf<AttributeArgumentSyntax>() is { } attributeArgument)
            {
                nodeToReplace = attributeArgument ?? throw new InvalidOperationException();
                nodeReplacementFunction = (originalNode, rewrittenNode) =>
                    ((AttributeArgumentSyntax)rewrittenNode)
                        .WithoutLeadingTrivia()
                        .WithNameColon(SyntaxFactory.NameColon(parameterName).WithLeadingTrivia(rewrittenNode.GetLeadingTrivia()))
                        .WithAdditionalAnnotations(Formatter.Annotation);
            }
            else
            {
                continue;
            }

            if (commentSpan.Start > diagnostic.Location.SourceSpan.Start)
            {
                // The comment was located in the trailing trivia of the argument node. We also know that it's the first
                // MultiLineCommentTrivia in the trailing trivia.
                var commentIndex = nodeToReplace.GetTrailingTrivia().IndexOf(SyntaxKind.MultiLineCommentTrivia);
                var originalReplacementFunction = nodeReplacementFunction;
                nodeReplacementFunction = (originalNode, rewrittenNode) =>
                {
                    rewrittenNode = originalReplacementFunction(originalNode, rewrittenNode);
                    return rewrittenNode.WithTrailingTrivia(rewrittenNode.GetTrailingTrivia().RemoveAt(commentIndex));
                };
            }
            else if (commentSpan.Start >= nodeToReplace.FullSpan.Start)
            {
                // The comment was located in the leading trivia of the argument node. We also know that it's the last
                // MultiLineCommentTrivia in the leading trivia.
                Debug.Assert(commentSpan.Start < nodeToReplace.Span.Start);
                var commentIndex = LastIndexOf(nodeToReplace.GetLeadingTrivia(), SyntaxKind.MultiLineCommentTrivia);
                var originalReplacementFunction = nodeReplacementFunction;
                nodeReplacementFunction = (originalNode, rewrittenNode) =>
                {
                    rewrittenNode = originalReplacementFunction(originalNode, rewrittenNode);
                    return rewrittenNode.WithLeadingTrivia(rewrittenNode.GetLeadingTrivia().RemoveAt(commentIndex));
                };
            }
            else
            {
                // The comment was located in the trailing trivia of the preceding token. We replace this token
                // separately from the updates to the argument.
                var commentTrivia = root.FindTrivia(commentSpan.Start);
                tokenToReplace = commentTrivia.Token;
                var commentIndex = tokenToReplace.Value.TrailingTrivia.IndexOf(commentTrivia);
                tokenReplacementFunction = (originalToken, rewrittenToken) => rewrittenToken.WithTrailingTrivia(rewrittenToken.TrailingTrivia.RemoveAt(commentIndex)).WithAdditionalAnnotations(Formatter.Annotation);
            }

            nodesToReplace.Add(nodeToReplace);
            nodeReplacementFunctions[nodeToReplace] = nodeReplacementFunction;

            if (tokenToReplace is { } token)
            {
                tokensToReplace.Add(token);
                tokenReplacementFunctions[token] = tokenReplacementFunction;
            }
        }

        return document.WithSyntaxRoot(root.ReplaceSyntax(
            nodesToReplace,
            (originalNode, rewrittenNode) => nodeReplacementFunctions[originalNode](originalNode, rewrittenNode),
            tokensToReplace,
            (originalToken, rewrittenToken) => tokenReplacementFunctions[originalToken](originalToken, rewrittenToken),
            Array.Empty<SyntaxTrivia>(),
            (originalTrivia, rewrittenTrivia) => rewrittenTrivia));
    }

    private static int LastIndexOf(SyntaxTriviaList triviaList, SyntaxKind syntaxKind)
    {
        for (var i = triviaList.Count - 1; i >= 0; i--)
        {
            if (triviaList[i].IsKind(syntaxKind))
                return i;
        }

        return -1;
    }
}
