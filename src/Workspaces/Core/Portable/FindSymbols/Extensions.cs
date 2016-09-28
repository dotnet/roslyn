﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal static partial class Extensions
    {
        public static async Task<IEnumerable<SyntaxToken>> GetConstructorInitializerTokensAsync(this Document document, CancellationToken cancellationToken)
        {
            // model should exist already
            SemanticModel model;
            if (!document.TryGetSemanticModel(out model))
            {
                return Contract.FailWithReturn<IEnumerable<SyntaxToken>>("we should never reach here");
            }

            var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFacts == null)
            {
                return SpecializedCollections.EmptyEnumerable<SyntaxToken>();
            }

            return FindReferenceCache.GetConstructorInitializerTokens(syntaxFacts, model, root, cancellationToken);
        }

        internal static async Task<ImmutableArray<SyntaxToken>> GetIdentifierOrGlobalNamespaceTokensWithTextAsync(
            this Document document, string identifier, CancellationToken cancellationToken)
        {
            // model should exist already
            SemanticModel model;
            if (!document.TryGetSemanticModel(out model))
            {
                return Contract.FailWithReturn<ImmutableArray<SyntaxToken>>("we should never reach here");
            }

            // It's very costly to walk an entire tree.  So if the tree is simple and doesn't contain
            // any unicode escapes in it, then we do simple string matching to find the tokens.
            var info = await SyntaxTreeInfo.GetIdentifierInfoAsync(document, cancellationToken).ConfigureAwait(false);
            if (!info.ProbablyContainsIdentifier(identifier))
            {
                return ImmutableArray<SyntaxToken>.Empty;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFacts == null)
            {
                return ImmutableArray<SyntaxToken>.Empty;
            }

            var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var version = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);

            SourceText text = null;
            if (!info.ProbablyContainsEscapedIdentifier(identifier))
            {
                text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            }

            return FindReferenceCache.GetIdentifierOrGlobalNamespaceTokensWithText(syntaxFacts, document, version, model, root, text, identifier, cancellationToken);
        }

        internal static bool TextMatch(this ISyntaxFactsService syntaxFacts, string text1, string text2)
        {
            return syntaxFacts.IsCaseSensitive ? text1 == text2 : string.Equals(text1, text2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
