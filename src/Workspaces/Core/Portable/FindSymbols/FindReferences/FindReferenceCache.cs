// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // TODO : this can be all moved down to compiler side.
    internal static class FindReferenceCache
    {
        private static readonly ReaderWriterLockSlim s_gate = new ReaderWriterLockSlim();
        private static readonly Dictionary<SemanticModel, Entry> s_cache = new Dictionary<SemanticModel, Entry>();

        public static SymbolInfo GetSymbolInfo(SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
        {
            var nodeCache = GetNodeCache(model);
            if (nodeCache == null)
            {
                return model.GetSymbolInfo(node, cancellationToken);
            }

            return nodeCache.GetOrAdd(node, n => model.GetSymbolInfo(n, cancellationToken));
        }

        public static IAliasSymbol GetAliasInfo(
            ISemanticFactsService semanticFacts, SemanticModel model, SyntaxToken token, CancellationToken cancellationToken)
        {
            if (semanticFacts == null)
            {
                return model.GetAliasInfo(token.Parent, cancellationToken);
            }

            var entry = GetCachedEntry(model);
            if (entry == null)
            {
                return model.GetAliasInfo(token.Parent, cancellationToken);
            }

            if (entry.AliasNameSet == null)
            {
                var set = semanticFacts.GetAliasNameSet(model, cancellationToken);
                Interlocked.CompareExchange(ref entry.AliasNameSet, set, null);
            }

            if (entry.AliasNameSet.Contains(token.ValueText))
            {
                return model.GetAliasInfo(token.Parent, cancellationToken);
            }

            return null;
        }

        public static ImmutableArray<SyntaxToken> GetIdentifierOrGlobalNamespaceTokensWithText(
            ISyntaxFactsService syntaxFacts,
            SemanticModel model,
            SyntaxNode root,
            SourceText sourceText,
            string text,
            CancellationToken cancellationToken)
        {
            var normalized = syntaxFacts.IsCaseSensitive ? text : text.ToLowerInvariant();

            var entry = GetCachedEntry(model);
            if (entry == null)
            {
                return GetIdentifierOrGlobalNamespaceTokensWithText(syntaxFacts, root, sourceText, normalized, cancellationToken);
            }

            return entry.IdentifierCache.GetOrAdd(normalized,
                key => GetIdentifierOrGlobalNamespaceTokensWithText(
                    syntaxFacts, root, sourceText, key, cancellationToken));
        }

        private static ImmutableArray<SyntaxToken> GetIdentifierOrGlobalNamespaceTokensWithText(
            ISyntaxFactsService syntaxFacts, SyntaxNode root, SourceText sourceText,
            string text, CancellationToken cancellationToken)
        {
            // identifier is not escaped
            if (sourceText != null)
            {
                return GetTokensFromText(syntaxFacts, root, sourceText, text, IsCandidate, cancellationToken);
            }

            // identifier is escaped
            return root.DescendantTokens(descendIntoTrivia: true).Where(IsCandidate).ToImmutableArray();

            bool IsCandidate(SyntaxToken t)
                => syntaxFacts.IsGlobalNamespaceKeyword(t) || (syntaxFacts.IsIdentifier(t) && syntaxFacts.TextMatch(t.ValueText, text));
        }

        private static ImmutableArray<SyntaxToken> GetTokensFromText(
            ISyntaxFactsService syntaxFacts, SyntaxNode root, SourceText content, string text, Func<SyntaxToken, bool> candidate, CancellationToken cancellationToken)
        {
            if (text.Length == 0)
            {
                return ImmutableArray<SyntaxToken>.Empty;
            }

            var result = ImmutableArray.CreateBuilder<SyntaxToken>();

            var index = 0;
            while ((index = content.IndexOf(text, index, syntaxFacts.IsCaseSensitive)) >= 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var nextIndex = index + text.Length;

                var token = root.FindToken(index, findInsideTrivia: true);
                var span = token.Span;
                if (!token.IsMissing && span.Start == index && span.Length == text.Length && candidate(token))
                {
                    result.Add(token);
                }

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
            var entry = GetCachedEntry(model);
            if (entry == null)
            {
                return GetConstructorInitializerTokens(syntaxFacts, root, cancellationToken);
            }

            if (entry.ConstructorInitializerCache == null)
            {
                var initializers = GetConstructorInitializerTokens(syntaxFacts, root, cancellationToken);
                Interlocked.CompareExchange(ref entry.ConstructorInitializerCache, initializers, null);
            }

            return entry.ConstructorInitializerCache;
        }

        private static List<SyntaxToken> GetConstructorInitializerTokens(
            ISyntaxFactsService syntaxFacts, SyntaxNode root, CancellationToken cancellationToken)
        {
            var initializers = new List<SyntaxToken>();
            foreach (var constructor in syntaxFacts.GetConstructors(root, cancellationToken))
            {
                foreach (var token in constructor.DescendantTokens(descendIntoTrivia: false))
                {
                    if (!syntaxFacts.IsThisConstructorInitializer(token) && !syntaxFacts.IsBaseConstructorInitializer(token))
                    {
                        continue;
                    }

                    initializers.Add(token);
                }
            }

            return initializers;
        }

        private static ConcurrentDictionary<SyntaxNode, SymbolInfo> GetNodeCache(SemanticModel model)
        {
            var entry = GetCachedEntry(model);
            if (entry == null)
            {
                return null;
            }

            return entry.SymbolInfoCache;
        }

        private static Entry GetCachedEntry(SemanticModel model)
        {
            using (s_gate.DisposableRead())
            {
                if (s_cache.TryGetValue(model, out var entry))
                {
                    return entry;
                }

                return null;
            }
        }

        private static readonly Func<SemanticModel, Entry> s_entryCreator = _ => new Entry();

        public static void Start(SemanticModel model)
        {
            Debug.Assert(model != null);

            using (s_gate.DisposableWrite())
            {
                var entry = s_cache.GetOrAdd(model, s_entryCreator);
                entry.Count++;
            }
        }

        public static void Stop(SemanticModel model)
        {
            if (model == null)
            {
                return;
            }

            using (s_gate.DisposableWrite())
            {
                if (!s_cache.TryGetValue(model, out var entry))
                {
                    return;
                }

                entry.Count--;
                if (entry.Count == 0)
                {
                    s_cache.Remove(model);
                }
            }
        }

        private class Entry
        {
            public int Count;
            public ImmutableHashSet<string> AliasNameSet;
            public List<SyntaxToken> ConstructorInitializerCache;

            public readonly ConcurrentDictionary<string, ImmutableArray<SyntaxToken>> IdentifierCache = new ConcurrentDictionary<string, ImmutableArray<SyntaxToken>>();
            public readonly ConcurrentDictionary<SyntaxNode, SymbolInfo> SymbolInfoCache = new ConcurrentDictionary<SyntaxNode, SymbolInfo>();
        }
    }
}
