// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal static class FindReferenceCache
    {
        private static readonly ConditionalWeakTable<SemanticModel, Entry> s_cache = new();

        private static Entry GetEntry(SemanticModel model)
            => s_cache.GetValue(model, static _ => new());

        public static SymbolInfo GetSymbolInfo(SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
        {
            var nodeCache = GetEntry(model).SymbolInfoCache;

            return nodeCache.GetOrAdd(node, static (n, arg) => arg.model.GetSymbolInfo(n, arg.cancellationToken), (model, cancellationToken));
        }

        public static IAliasSymbol? GetAliasInfo(
            ISemanticFactsService semanticFacts, SemanticModel model, SyntaxToken token, CancellationToken cancellationToken)
        {
            if (semanticFacts == null)
                return model.GetAliasInfo(token.GetRequiredParent(), cancellationToken);

            var entry = GetEntry(model);

            if (entry.AliasNameSet == null)
            {
                var set = semanticFacts.GetAliasNameSet(model, cancellationToken);
                Interlocked.CompareExchange(ref entry.AliasNameSet, set, null);
            }

            if (entry.AliasNameSet.Contains(token.ValueText))
                return model.GetAliasInfo(token.GetRequiredParent(), cancellationToken);

            return null;
        }

        public static ImmutableArray<SyntaxToken> GetIdentifierTokensWithText(
            ISyntaxFactsService syntaxFacts,
            SemanticModel model,
            SyntaxNode root,
            SourceText? sourceText,
            string text,
            CancellationToken cancellationToken)
        {
            var normalized = syntaxFacts.IsCaseSensitive ? text : text.ToLowerInvariant();
            var entry = GetEntry(model);

            return entry.IdentifierCache.GetOrAdd(normalized,
                key => GetIdentifierTokensWithText(syntaxFacts, root, sourceText, key, cancellationToken));
        }

        [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", AllowCaptures = false)]
        private static ImmutableArray<SyntaxToken> GetIdentifierTokensWithText(
            ISyntaxFactsService syntaxFacts, SyntaxNode root, SourceText? sourceText,
            string text, CancellationToken cancellationToken)
        {
            if (sourceText != null)
            {
                // identifier is not escaped
                return GetTokensFromText(syntaxFacts, root, sourceText, text, cancellationToken);
            }
            else
            {
                // identifier is escaped.  Have to actually walk the entire tree to find matching tokens.

                using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var result);
                foreach (var token in root.DescendantTokens(descendIntoTrivia: true))
                {
                    if (IsCandidate(syntaxFacts, token, text))
                        result.Add(token);
                }

                return result.ToImmutable();
            }
        }

        private static bool IsCandidate(ISyntaxFactsService syntaxFacts, SyntaxToken token, string text)
            => syntaxFacts.IsIdentifier(token) && syntaxFacts.TextMatch(token.ValueText, text);

        private static ImmutableArray<SyntaxToken> GetTokensFromText(
            ISyntaxFactsService syntaxFacts, SyntaxNode root, SourceText content, string text, CancellationToken cancellationToken)
        {
            if (text.Length == 0)
                return ImmutableArray<SyntaxToken>.Empty;

            using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var result);

            var index = 0;
            while ((index = content.IndexOf(text, index, syntaxFacts.IsCaseSensitive)) >= 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var nextIndex = index + text.Length;

                var token = root.FindToken(index, findInsideTrivia: true);
                var span = token.Span;
                if (!token.IsMissing && span.Start == index && span.Length == text.Length && IsCandidate(syntaxFacts, token, text))
                    result.Add(token);

                nextIndex = Math.Max(nextIndex, token.SpanStart);
                index = nextIndex;
            }

            return result.ToImmutable();
        }

        public static IEnumerable<SyntaxToken> GetConstructorInitializerTokens(
            ISyntaxFactsService syntaxFacts, SemanticModel model, SyntaxNode root, CancellationToken cancellationToken)
        {
            // this one will only get called when we know given document contains constructor initializer.
            // no reason to use text to check whether it exist first.
            var entry = GetEntry(model);

            if (entry.ConstructorInitializerCache.IsDefault)
            {
                var initializers = GetConstructorInitializerTokens(syntaxFacts, root, cancellationToken);
                ImmutableInterlocked.InterlockedInitialize(ref entry.ConstructorInitializerCache, initializers);
            }

            return entry.ConstructorInitializerCache;
        }

        private static ImmutableArray<SyntaxToken> GetConstructorInitializerTokens(
            ISyntaxFactsService syntaxFacts, SyntaxNode root, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var initializers);
            foreach (var constructor in syntaxFacts.GetConstructors(root, cancellationToken))
            {
                foreach (var token in constructor.DescendantTokens(descendIntoTrivia: false))
                {
                    if (syntaxFacts.IsThisConstructorInitializer(token) || syntaxFacts.IsBaseConstructorInitializer(token))
                        initializers.Add(token);
                }
            }

            return initializers.ToImmutable();
        }

        private sealed class Entry
        {
            public ImmutableHashSet<string>? AliasNameSet;
            public ImmutableArray<SyntaxToken> ConstructorInitializerCache;

            public readonly ConcurrentDictionary<string, ImmutableArray<SyntaxToken>> IdentifierCache = new();
            public readonly ConcurrentDictionary<SyntaxNode, SymbolInfo> SymbolInfoCache = new();
        }
    }
}
