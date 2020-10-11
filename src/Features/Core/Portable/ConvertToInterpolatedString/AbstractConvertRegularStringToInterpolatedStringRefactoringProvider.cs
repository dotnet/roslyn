// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertToInterpolatedString
{
    /// <summary>
    /// Code refactoring that converts a regular string containing braces to an interpolated string
    /// </summary>
    internal abstract class AbstractConvertRegularStringToInterpolatedStringRefactoringProvider<TExpressionSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var literalExpression = await context.TryGetRelevantNodeAsync<TExpressionSyntax>().ConfigureAwait(false);
            if (literalExpression == null || !IsAppropriateLiteralKind(literalExpression))
                return;

            var stringToken = literalExpression.GetFirstToken();
            if (stringToken.Text is null || (!stringToken.Text.Contains("{") && !stringToken.Text.Contains("}")))
                return;

            var (document, _, cancellationToken) = context;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            // If there is a const keyword, do not offer the refactoring (an interpolated string is not const)
            var declarator = literalExpression.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsVariableDeclarator);
            if (declarator != null)
            {
                var generator = SyntaxGenerator.GetGenerator(document);
                if (generator.GetModifiers(declarator).IsConst)
                    return;
            }

            var isVerbatim = syntaxFacts.IsVerbatimStringLiteral(stringToken);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var interpolatedString = CreateInterpolatedString(document, isVerbatim, literalExpression);

            context.RegisterRefactoring(
                new MyCodeAction(
                    _ => UpdateDocumentAsync(document, root, literalExpression, interpolatedString)),
                literalExpression.Span);
        }

        protected abstract bool IsAppropriateLiteralKind(TExpressionSyntax literalExpression);

        protected abstract string GetTextWithoutQuotes(string text, bool isVerbatim);

        private SyntaxNode CreateInterpolatedString(
            Document document, bool isVerbatimStringLiteral, SyntaxNode literalExpression)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var startToken = generator.CreateInterpolatedStringStartToken(isVerbatimStringLiteral)
                                .WithLeadingTrivia(literalExpression.GetLeadingTrivia());
            var endToken = generator.CreateInterpolatedStringEndToken()
                                .WithTrailingTrivia(literalExpression.GetTrailingTrivia());

            var text = literalExpression.GetFirstToken().Text;
            var textWithEscapedBraces = text.Replace("{", "{{").Replace("}", "}}");
            var textWithoutQuotes = GetTextWithoutQuotes(textWithEscapedBraces, isVerbatimStringLiteral);
            var newNode = generator.InterpolatedStringText(generator.InterpolatedStringTextToken(textWithoutQuotes));

            return generator.InterpolatedStringExpression(startToken, new[] { newNode }, endToken);
        }

        private static Task<Document> UpdateDocumentAsync(Document document, SyntaxNode root, SyntaxNode top, SyntaxNode interpolatedString)
        {
            var newRoot = root.ReplaceNode(top, interpolatedString);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Convert_to_interpolated_string, createChangedDocument)
            {
            }
        }
    }
}
