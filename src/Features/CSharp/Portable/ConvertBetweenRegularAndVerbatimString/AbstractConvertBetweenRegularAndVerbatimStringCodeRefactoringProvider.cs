// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Text;
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
    internal abstract class AbstractConvertBetweenRegularAndVerbatimStringCodeRefactoringProvider<
        TStringExpressionSyntax>
        : CodeRefactoringProvider
        where TStringExpressionSyntax : SyntaxNode
    {
        private const char DoubleQuote = '"';

        protected abstract bool IsAppropriateLiteralKind(TStringExpressionSyntax literalExpression);
        protected abstract ImmutableArray<SyntaxToken> GetSubStringTokens(TStringExpressionSyntax literalExpression);
        protected abstract bool IsVerbatim(TStringExpressionSyntax literalExpression);
        protected abstract TStringExpressionSyntax CreateVerbatimStringExpression(IVirtualCharService charService, StringBuilder sb, TStringExpressionSyntax stringExpression);
        protected abstract TStringExpressionSyntax CreateRegularStringExpression(IVirtualCharService charService, StringBuilder sb, TStringExpressionSyntax stringExpression);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var literalExpression = await context.TryGetRelevantNodeAsync<TStringExpressionSyntax>().ConfigureAwait(false);
            if (literalExpression == null || !IsAppropriateLiteralKind(literalExpression))
                return;

            var (document, _, cancellationToken) = context;

            var syntaxFacts = CSharpSyntaxFacts.Instance;
            var charService = document.GetRequiredLanguageService<IVirtualCharService>();

            var subStringTokens = GetSubStringTokens(literalExpression);
            // First, ensure that we understand all text parts of the interpolation.
            foreach (var subToken in subStringTokens)
            {
                var chars = charService.TryConvertToVirtualChars(subToken);
                if (chars.IsDefault)
                    return;
            }

            // Offer to convert to a verbatim string if the normal string contains simple
            // escapes that can be directly embedded in the verbatim string.
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            if (IsVerbatim(literalExpression))
            {
                // always offer to convert from verbatim string to normal string.
                context.RegisterRefactoring(new MyCodeAction(
                    CSharpFeaturesResources.Convert_to_regular_string,
                    c => ConvertToRegularStringAsync(document, literalExpression, c)));
            }
            else if (ContainsSimpleEscape(charService, sourceText, subStringTokens))
            {
                context.RegisterRefactoring(new MyCodeAction(
                    CSharpFeaturesResources.Convert_to_verbatim_string,
                    c => ConvertToVerbatimStringAsync(document, literalExpression, c)));
            }
        }

        private async Task<Document> ConvertAsync(
            Document document, TStringExpressionSyntax stringExpression,
            Func<IVirtualCharService, StringBuilder, TStringExpressionSyntax, TStringExpressionSyntax> convert,
            CancellationToken cancellationToken)
        {
            using var _ = PooledStringBuilder.GetInstance(out var sb);

            var charService = document.GetRequiredLanguageService<IVirtualCharService>();
            var newStringExpression = convert(charService, sb.Builder, stringExpression).WithTriviaFrom(stringExpression);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            return document.WithSyntaxRoot(root.ReplaceNode(stringExpression, newStringExpression));
        }

        private Task<Document> ConvertToVerbatimStringAsync(Document document, TStringExpressionSyntax stringExpression, CancellationToken cancellationToken)
            => ConvertAsync(document, stringExpression, CreateVerbatimStringExpression, cancellationToken);

        private Task<Document> ConvertToRegularStringAsync(Document document, TStringExpressionSyntax stringExpression, CancellationToken cancellationToken)
            => ConvertAsync(document, stringExpression, CreateRegularStringExpression, cancellationToken);

        protected static void AddVerbatimStringText(
            IVirtualCharService charService, StringBuilder sb, SyntaxToken stringToken)
        {
            var chars = charService.TryConvertToVirtualChars(stringToken);

            foreach (var ch in chars)
            {
                // just build the verbatim string by concatenating all the chars in the original
                // string.  The only exception are double-quotes which need to be doubled up in the
                // final string.
                sb.Append(ch.Char);

                if (ch.Char == DoubleQuote)
                    sb.Append(ch.Char);
            }
        }

        protected static void AddRegularStringText(
            IVirtualCharService charService, StringBuilder sb, SyntaxToken stringToken)
        {
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
                    sb.Append(ch);
                }
            }
        }

        private bool ContainsSimpleEscape(
            IVirtualCharService charService, SourceText text, ImmutableArray<SyntaxToken> subTokens)
        {
            foreach (var subToken in subTokens)
            {
                var chars = charService.TryConvertToVirtualChars(subToken);

                // This was checked above.
                Debug.Assert(!chars.IsDefault);
                if (ContainsSimpleEscape(text, chars))
                    return true;
            }

            return false;
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
