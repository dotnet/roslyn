// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ConvertBetweenRegularAndVerbatimString
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class ConvertBetweenRegularAndVerbatimInterpolatedStringCodeRefactoringProvider :
        CodeRefactoringProvider
    {
        private const char DoubleQuote = '"';

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var stringExpression = await context.TryGetRelevantNodeAsync<InterpolatedStringExpressionSyntax>().ConfigureAwait(false);
            if (stringExpression == null)
                return;

            var (document, _, cancellationToken) = context;

            var syntaxFacts = CSharpSyntaxFacts.Instance;
            var charService = document.GetRequiredLanguageService<IVirtualCharService>();

            // First, ensure that we understand all text parts of the interpolation.
            foreach (var content in stringExpression.Contents)
            {
                if (content is InterpolatedStringTextSyntax interpolatedStringText)
                {
                    var chars = charService.TryConvertToVirtualChars(interpolatedStringText.TextToken);
                    if (chars.IsDefaultOrEmpty)
                        return;
                }
            }

            // Offer to convert to a verbatim string if the normal string contains simple
            // escapes that can be directly embedded in the verbatim string.
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            if (stringExpression.StringStartToken.Kind() == SyntaxKind.InterpolatedVerbatimStringStartToken)
            {
                // always offer to convert from verbatim string to normal string.
                context.RegisterRefactoring(new MyCodeAction(
                    CSharpFeaturesResources.Convert_to_regular_string,
                    c => ConvertToRegularStringAsync(document, stringExpression, c)));
            }
            else if (ContainsSimpleEscape(charService, sourceText, stringExpression))
            {
                context.RegisterRefactoring(new MyCodeAction(
                    CSharpFeaturesResources.Convert_to_verbatim_string,
                    c => ConvertToVerbatimStringAsync(document, stringExpression, c)));
            }
        }

        private Task<Document> ConvertToVerbatimStringAsync(
            Document document, InterpolatedStringExpressionSyntax stringExpression, CancellationToken cancellationToken)
        {
            var newTokenText = CreateVerbatimStringTokenText(document, stringExpression);
            return ReplaceTokenAsync(document, stringExpression, newTokenText, cancellationToken);
        }

        private static string CreateVerbatimStringTokenText(Document document, SyntaxToken stringToken)
        {
            using var _ = PooledStringBuilder.GetInstance(out var sb);

            var charService = document.GetRequiredLanguageService<IVirtualCharService>();
            var chars = charService.TryConvertToVirtualChars(stringToken);

            sb.Builder.Append('@');
            sb.Builder.Append(DoubleQuote);

            foreach (var ch in chars)
            {
                // just build the verbatim string by concatenating all the chars in the original
                // string.  The only exception are double-quotes which need to be doubled up in the
                // final string.
                sb.Builder.Append(ch.Char);

                if (ch.Char == DoubleQuote)
                    sb.Builder.Append(ch.Char);
            }

            sb.Builder.Append(DoubleQuote);
            return sb.Builder.ToString();
        }

        private Task<Document> ConvertToRegularStringAsync(Document document, SyntaxToken stringToken, CancellationToken cancellationToken)
        {
            var newTokenText = CreateRegularStringTokenText(document, stringToken);
            return ReplaceTokenAsync(document, stringToken, newTokenText, cancellationToken);
        }

        private string CreateRegularStringTokenText(Document document, SyntaxToken stringToken)
        {
            using var _ = PooledStringBuilder.GetInstance(out var sb);

            var charService = document.GetRequiredLanguageService<IVirtualCharService>();
            var chars = charService.TryConvertToVirtualChars(stringToken);

            sb.Builder.Append(DoubleQuote);

            foreach (var ch in chars)
            {
                if (charService.TryGetEscapeCharacter(ch, out var escaped))
                {
                    sb.Builder.Append('\\');
                    sb.Builder.Append(escaped);
                }
                else
                {
                    sb.Builder.Append(ch);
                }
            }

            sb.Builder.Append(DoubleQuote);
            return sb.Builder.ToString();
        }

        private static async Task<Document> ReplaceTokenAsync(Document document, SyntaxToken stringToken, string newTokenText, CancellationToken cancellationToken)
        {
            var finalStringToken = SyntaxFactory.Token(
                stringToken.LeadingTrivia,
                SyntaxKind.StringLiteralToken,
                newTokenText, valueText: "",
                stringToken.TrailingTrivia);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceToken(stringToken, finalStringToken);

            return document.WithSyntaxRoot(newRoot);
        }

        private bool ContainsSimpleEscape(SourceText text, VirtualCharSequence chars)
        {
            foreach (var ch in chars)
            {
                // look for two-character escapes that start with  \  .  i.e.  \n  . Note:  \0
                // cannot be enocded into a verbatim string, so don't offer to convert if we have
                // that.
                if (ch.Span.Length == 2 &&
                    ch.Char != 0 &&
                    text[ch.Span.Start] == '\\')
                {
                    return true;
                }
            }

            return false;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            /// <summary>
            /// This is a generally useful feature on strings.  But it's not likely to be something
            /// people want to use a lot.  Make low priority so it doesn't interfere with more
            /// commonly useful refactorings.
            /// </summary>
            internal override CodeActionPriority Priority => CodeActionPriority.Low;

            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
