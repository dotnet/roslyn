﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SyntaxTokenExtensions
    {
        public static bool IsLastTokenOfNode<T>(this SyntaxToken token) where T : SyntaxNode
            => token.IsLastTokenOfNode<T>(out _);

        public static bool IsLastTokenOfNode<T>(this SyntaxToken token, [NotNullWhen(true)] out T node) where T : SyntaxNode
        {
            node = token.GetAncestor<T>();
            return node != null && token == node.GetLastToken(includeZeroWidth: true);
        }

        public static bool IsKindOrHasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            return token.Kind() == kind || token.HasMatchingText(kind);
        }

        public static bool HasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            return token.ToString() == SyntaxFacts.GetText(kind);
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2;
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2
                || token.Kind() == kind3;
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2
                || token.Kind() == kind3
                || token.Kind() == kind4;
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2
                || token.Kind() == kind3
                || token.Kind() == kind4
                || token.Kind() == kind5;
        }

        public static bool IsKind(this SyntaxToken token, params SyntaxKind[] kinds)
        {
            return kinds.Contains(token.Kind());
        }

        public static bool IsOpenBraceOrCommaOfObjectInitializer(this SyntaxToken token)
        {
            return (token.IsKind(SyntaxKind.OpenBraceToken) || token.IsKind(SyntaxKind.CommaToken)) &&
                token.Parent.IsKind(SyntaxKind.ObjectInitializerExpression);
        }

        public static bool IsOpenBraceOfAccessorList(this SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.OpenBraceToken) && token.Parent.IsKind(SyntaxKind.AccessorList);
        }

        /// <summary>
        /// Returns true if this token is something that looks like a C# keyword. This includes 
        /// actual keywords, contextual keywords, and even 'var' and 'dynamic'
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool CouldBeKeyword(this SyntaxToken token)
        {
            if (token.IsKeyword())
            {
                return true;
            }

            if (token.Kind() == SyntaxKind.IdentifierToken)
            {
                var simpleNameText = token.ValueText;
                return simpleNameText == "var" ||
                       simpleNameText == "dynamic" ||
                       SyntaxFacts.GetContextualKeywordKind(simpleNameText) != SyntaxKind.None;
            }

            return false;
        }

        public static bool IsLiteral(this SyntaxToken token)
        {
            switch (token.Kind())
            {
                case SyntaxKind.CharacterLiteralToken:
                case SyntaxKind.FalseKeyword:
                case SyntaxKind.NumericLiteralToken:
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.TrueKeyword:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IntersectsWith(this SyntaxToken token, int position)
        {
            return token.Span.IntersectsWith(position);
        }

        public static SyntaxToken GetPreviousTokenIfTouchingWord(this SyntaxToken token, int position)
        {
            return token.IntersectsWith(position) && IsWord(token)
                ? token.GetPreviousToken(includeSkipped: true)
                : token;
        }

        private static bool IsWord(SyntaxToken token)
        {
            return CSharpSyntaxFacts.Instance.IsWord(token);
        }

        public static SyntaxToken GetNextNonZeroWidthTokenOrEndOfFile(this SyntaxToken token)
        {
            return token.GetNextTokenOrEndOfFile();
        }

        /// <summary>
        /// Determines whether the given SyntaxToken is the first token on a line in the specified SourceText.
        /// </summary>
        public static bool IsFirstTokenOnLine(this SyntaxToken token, SourceText text)
        {
            var previousToken = token.GetPreviousToken(includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
            if (previousToken.Kind() == SyntaxKind.None)
            {
                return true;
            }

            var tokenLine = text.Lines.IndexOf(token.SpanStart);
            var previousTokenLine = text.Lines.IndexOf(previousToken.SpanStart);
            return tokenLine > previousTokenLine;
        }

        public static bool SpansPreprocessorDirective(this IEnumerable<SyntaxToken> tokens)
            => CSharpSyntaxFacts.Instance.SpansPreprocessorDirective(tokens);

        /// <summary>
        /// Retrieves all trivia after this token, including it's trailing trivia and
        /// the leading trivia of the next token.
        /// </summary>
        public static IEnumerable<SyntaxTrivia> GetAllTrailingTrivia(this SyntaxToken token)
        {
            foreach (var trivia in token.TrailingTrivia)
            {
                yield return trivia;
            }

            var nextToken = token.GetNextTokenOrEndOfFile(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);

            foreach (var trivia in nextToken.LeadingTrivia)
            {
                yield return trivia;
            }
        }

        public static bool IsRegularStringLiteral(this SyntaxToken token)
        {
            return token.Kind() == SyntaxKind.StringLiteralToken && !token.IsVerbatimStringLiteral();
        }

        public static bool IsValidAttributeTarget(this SyntaxToken token)
        {
            switch (token.Kind())
            {
                case SyntaxKind.AssemblyKeyword:
                case SyntaxKind.ModuleKeyword:
                case SyntaxKind.FieldKeyword:
                case SyntaxKind.EventKeyword:
                case SyntaxKind.MethodKeyword:
                case SyntaxKind.ParamKeyword:
                case SyntaxKind.PropertyKeyword:
                case SyntaxKind.ReturnKeyword:
                case SyntaxKind.TypeKeyword:
                    return true;

                default:
                    return false;
            }
        }

        public static SyntaxToken WithCommentsFrom(
            this SyntaxToken token,
            IEnumerable<SyntaxTrivia> leadingTrivia,
            IEnumerable<SyntaxTrivia> trailingTrivia,
            params SyntaxNodeOrToken[] trailingNodesOrTokens)
            => token
                .WithPrependedLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia((
                    token.TrailingTrivia.Concat(SyntaxNodeOrTokenExtensions.GetTrivia(trailingNodesOrTokens).Concat(trailingTrivia))).FilterComments(addElasticMarker: false));

        public static SyntaxToken KeepCommentsAndAddElasticMarkers(this SyntaxToken token)
            => token
                    .WithTrailingTrivia(token.TrailingTrivia.FilterComments(addElasticMarker: true))
                    .WithLeadingTrivia(token.LeadingTrivia.FilterComments(addElasticMarker: true));
    }
}
