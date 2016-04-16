// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        public static IEnumerable<SyntaxToken> GetIdentifierOrGlobalNamespaceTokensWithText(
            ISyntaxFactsService syntaxFacts, Document document, VersionStamp version, SemanticModel model, SyntaxNode root, SourceText sourceText,
            string text, CancellationToken cancellationToken)
        {
            var normalized = syntaxFacts.IsCaseSensitive ? text : text.ToLowerInvariant();

            var entry = GetCachedEntry(model);
            if (entry == null)
            {
                return GetIdentifierOrGlobalNamespaceTokensWithText(syntaxFacts, document, version, root, sourceText, normalized, cancellationToken);
            }

            return entry.IdentifierCache.GetOrAdd(normalized, key => GetIdentifierOrGlobalNamespaceTokensWithText(syntaxFacts, document, version, root, sourceText, key, cancellationToken));
        }

        private static IList<SyntaxToken> GetIdentifierOrGlobalNamespaceTokensWithText(
            ISyntaxFactsService syntaxFacts, Document document, VersionStamp version, SyntaxNode root, SourceText sourceText,
            string text, CancellationToken cancellationToken)
        {
            Func<SyntaxToken, bool> candidate = t =>
                syntaxFacts.IsGlobalNamespaceKeyword(t) || (syntaxFacts.IsIdentifier(t) && syntaxFacts.TextMatch(t.ValueText, text));

            // identifier is not escaped
            if (sourceText != null)
            {
                return GetTokensFromText(syntaxFacts, document, version, root, sourceText, text, candidate, cancellationToken);
            }

            // identifier is escaped
            return root.DescendantTokens(descendIntoTrivia: true).Where(candidate).ToList();
        }

        private static IList<SyntaxToken> GetTokensFromText(
            ISyntaxFactsService syntaxFacts, Document document, VersionStamp version, SyntaxNode root,
            SourceText content, string text, Func<SyntaxToken, bool> candidate, CancellationToken cancellationToken)
        {
            if (text.Length > 0)
            {
                using (var positions = SharedPools.BigDefault<List<int>>().GetPooledObject())
                {
                    if (SyntaxTreeIdentifierInfo.TryGetIdentifierLocations(document, version, text, positions.Object, cancellationToken))
                    {
                        return GetTokensFromText(root, positions.Object, text, candidate, cancellationToken).ToList();
                    }
                }

                return GetTokensFromText(syntaxFacts, root, content, text, candidate, cancellationToken).ToList();
            }

            return SpecializedCollections.EmptyList<SyntaxToken>();
        }

        private static IEnumerable<SyntaxToken> GetTokensFromText(
            SyntaxNode root, List<int> positions, string text, Func<SyntaxToken, bool> candidate, CancellationToken cancellationToken)
        {
            foreach (var index in positions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var token = root.FindToken(index, findInsideTrivia: true);

                var span = token.Span;
                if (!token.IsMissing && span.Start == index && span.Length == text.Length && candidate(token))
                {
                    yield return token;
                }
            }
        }

        private static IEnumerable<SyntaxToken> GetTokensFromText(
            ISyntaxFactsService syntaxFacts, SyntaxNode root, SourceText content, string text, Func<SyntaxToken, bool> candidate, CancellationToken cancellationToken)
        {
            var index = 0;
            while ((index = content.IndexOf(text, index, syntaxFacts.IsCaseSensitive)) >= 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var nextIndex = index + text.Length;

                var token = root.FindToken(index, findInsideTrivia: true);
                var span = token.Span;
                if (!token.IsMissing && span.Start == index && span.Length == text.Length && candidate(token))
                {
                    yield return token;
                }

                nextIndex = Math.Max(nextIndex, token.SpanStart);
                index = nextIndex;
            }
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
                Entry entry;
                if (s_cache.TryGetValue(model, out entry))
                {
                    return entry;
                }

                return null;
            }
        }

        private static readonly Func<SemanticModel, Entry> s_entryCreator = _ => new Entry();

        public static void Start(SemanticModel model)
        {
            Contract.Requires(model != null);

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
                Entry entry;
                if (!s_cache.TryGetValue(model, out entry))
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

            public readonly ConcurrentDictionary<string, IList<SyntaxToken>> IdentifierCache = new ConcurrentDictionary<string, IList<SyntaxToken>>();
            public readonly ConcurrentDictionary<SyntaxNode, SymbolInfo> SymbolInfoCache = new ConcurrentDictionary<SyntaxNode, SymbolInfo>();
        }
    }
}
