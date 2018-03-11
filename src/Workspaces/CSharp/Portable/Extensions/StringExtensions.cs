// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class StringExtensions
    {
        public static string EscapeIdentifier(this string identifier, SyntaxContext context = null)
        {
            var nullIndex = identifier.IndexOf('\0');
            if (nullIndex >= 0)
            {
                identifier = identifier.Substring(0, nullIndex);
            }

            var needsEscaping = SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None;

            var isQueryContext = context?.IsInQuery ?? false;
            var isAsyncContext = context?.IsWithinAsyncMethod ?? false;

            // Check if we need to escape this contextual keyword
            needsEscaping = needsEscaping ||
                (isQueryContext && SyntaxFacts.IsQueryContextualKeyword(SyntaxFacts.GetContextualKeywordKind(identifier))) ||
                (isAsyncContext && SyntaxFacts.GetContextualKeywordKind(identifier) == SyntaxKind.AwaitKeyword);

            return needsEscaping ? "@" + identifier : identifier;
        }

        public static SyntaxToken ToIdentifierToken(this string identifier, SyntaxContext context = null)
        {
            var escaped = identifier.EscapeIdentifier(context);

            if (escaped.Length == 0 || escaped[0] != '@')
            {
                return SyntaxFactory.Identifier(escaped);
            }

            var unescaped = identifier.StartsWith("@", StringComparison.Ordinal)
                ? identifier.Substring(1)
                : identifier;

            var token = SyntaxFactory.Identifier(
                default, SyntaxKind.None, "@" + unescaped, unescaped, default);

            if (!identifier.StartsWith("@", StringComparison.Ordinal))
            {
                token = token.WithAdditionalAnnotations(Simplifier.Annotation);
            }

            return token;
        }

        public static IdentifierNameSyntax ToIdentifierName(this string identifier)
        {
            return SyntaxFactory.IdentifierName(identifier.ToIdentifierToken());
        }
    }
}
