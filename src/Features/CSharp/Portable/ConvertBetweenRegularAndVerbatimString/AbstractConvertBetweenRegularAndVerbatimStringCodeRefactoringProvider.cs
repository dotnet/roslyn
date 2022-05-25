// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertBetweenRegularAndVerbatimString
{
    internal abstract class AbstractConvertBetweenRegularAndVerbatimStringCodeRefactoringProvider<
        TStringExpressionSyntax>
        : CodeRefactoringProvider
        where TStringExpressionSyntax : ExpressionSyntax
    {
        private const char OpenBrace = '{';
        private const char CloseBrace = '}';
        protected const char DoubleQuote = '"';

        protected abstract bool IsInterpolation { get; }
        protected abstract bool IsAppropriateLiteralKind(TStringExpressionSyntax literalExpression);
        protected abstract void AddSubStringTokens(TStringExpressionSyntax literalExpression, ArrayBuilder<SyntaxToken> subTokens);
        protected abstract bool IsVerbatim(TStringExpressionSyntax literalExpression);
        protected abstract TStringExpressionSyntax CreateVerbatimStringExpression(IVirtualCharService charService, StringBuilder sb, TStringExpressionSyntax stringExpression);
        protected abstract TStringExpressionSyntax CreateRegularStringExpression(IVirtualCharService charService, StringBuilder sb, TStringExpressionSyntax stringExpression);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var literalExpression = await context.TryGetRelevantNodeAsync<TStringExpressionSyntax>().ConfigureAwait(false);
            if (literalExpression == null || !IsAppropriateLiteralKind(literalExpression))
                return;

            var (document, _, cancellationToken) = context;

            var charService = document.GetRequiredLanguageService<IVirtualCharLanguageService>();

            using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var subStringTokens);

            // First, ensure that we understand all text parts of the interpolation.
            AddSubStringTokens(literalExpression, subStringTokens);
            foreach (var subToken in subStringTokens)
            {
                var chars = charService.TryConvertToVirtualChars(subToken);
                if (chars.IsDefault)
                    return;
            }

            // Note: This is a generally useful feature on strings.  But it's not likely to be something
            // people want to use a lot.  Make low priority so it doesn't interfere with more
            // commonly useful refactorings.

            if (IsVerbatim(literalExpression))
            {
                // always offer to convert from verbatim string to normal string.
                context.RegisterRefactoring(CodeAction.CreateWithPriority(
                    CodeActionPriority.Low,
                    CSharpFeaturesResources.Convert_to_regular_string,
                    c => ConvertToRegularStringAsync(document, literalExpression, c),
                    nameof(CSharpFeaturesResources.Convert_to_regular_string)));
            }
            else if (ContainsSimpleEscape(charService, subStringTokens))
            {
                // Offer to convert to a verbatim string if the normal string contains simple
                // escapes that can be directly embedded in the verbatim string.
                context.RegisterRefactoring(CodeAction.CreateWithPriority(
                    CodeActionPriority.Low,
                    CSharpFeaturesResources.Convert_to_verbatim_string,
                    c => ConvertToVerbatimStringAsync(document, literalExpression, c),
                    nameof(CSharpFeaturesResources.Convert_to_verbatim_string)));
            }
        }

        private static async Task<Document> ConvertAsync(
            Func<IVirtualCharService, StringBuilder, TStringExpressionSyntax, TStringExpressionSyntax> convert,
            Document document, TStringExpressionSyntax stringExpression, CancellationToken cancellationToken)
        {
            using var _ = PooledStringBuilder.GetInstance(out var sb);

            var charService = document.GetRequiredLanguageService<IVirtualCharLanguageService>();
            var newStringExpression = convert(charService, sb, stringExpression).WithTriviaFrom(stringExpression);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            return document.WithSyntaxRoot(root.ReplaceNode(stringExpression, newStringExpression));
        }

        private Task<Document> ConvertToVerbatimStringAsync(Document document, TStringExpressionSyntax stringExpression, CancellationToken cancellationToken)
            => ConvertAsync(CreateVerbatimStringExpression, document, stringExpression, cancellationToken);

        private Task<Document> ConvertToRegularStringAsync(Document document, TStringExpressionSyntax stringExpression, CancellationToken cancellationToken)
            => ConvertAsync(CreateRegularStringExpression, document, stringExpression, cancellationToken);

        protected void AddVerbatimStringText(
            IVirtualCharService charService, StringBuilder sb, SyntaxToken stringToken)
        {
            var isInterpolation = IsInterpolation;
            var chars = charService.TryConvertToVirtualChars(stringToken);

            foreach (var ch in chars)
            {
                // just build the verbatim string by concatenating all the chars in the original
                // string.  The only exceptions are double-quotes which need to be doubled up in the
                // final string, and curlies which need to be doubled in interpolations.
                ch.AppendTo(sb);

                if (ShouldDouble(ch, isInterpolation))
                    ch.AppendTo(sb);
            }

            static bool ShouldDouble(VirtualChar ch, bool isInterpolation)
            {
                if (ch == DoubleQuote)
                    return true;

                if (isInterpolation)
                    return IsOpenOrCloseBrace(ch);

                return false;
            }
        }

        private static bool IsOpenOrCloseBrace(VirtualChar ch)
            => ch == OpenBrace || ch == CloseBrace;

        protected void AddRegularStringText(
            IVirtualCharService charService, StringBuilder sb, SyntaxToken stringToken)
        {
            var isInterpolation = IsInterpolation;
            var chars = charService.TryConvertToVirtualChars(stringToken);

            foreach (var ch in chars)
            {
                if (charService.TryGetEscapeCharacter(ch, out var escaped))
                {
                    sb.Append('\\');
                    sb.Append(escaped);
                }
                else
                {
                    ch.AppendTo(sb);

                    // if it's an interpolation, we need to double-up open/close braces.
                    if (isInterpolation && IsOpenOrCloseBrace(ch))
                        ch.AppendTo(sb);
                }
            }
        }

        private static bool ContainsSimpleEscape(
            IVirtualCharService charService, ArrayBuilder<SyntaxToken> subTokens)
        {
            foreach (var subToken in subTokens)
            {
                var chars = charService.TryConvertToVirtualChars(subToken);

                // This was checked above.
                Debug.Assert(!chars.IsDefault);
                if (ContainsSimpleEscape(chars))
                    return true;
            }

            return false;
        }

        private static bool ContainsSimpleEscape(VirtualCharSequence chars)
        {
            foreach (var ch in chars)
            {
                // look for two-character escapes that start with  \  .  i.e.  \n  . Note:  \0
                // cannot be encoded into a verbatim string, so don't offer to convert if we have
                // that.
                if (ch.Span.Length == 2 && ch.Rune.Value != 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
