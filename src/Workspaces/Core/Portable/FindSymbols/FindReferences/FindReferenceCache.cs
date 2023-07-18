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
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed class FindReferenceCache
    {
        private static readonly ConditionalWeakTable<SemanticModel, FindReferenceCache> s_cache = new();

        public static FindReferenceCache GetCache(SemanticModel model)
            => s_cache.GetValue(model, static model => new(model));

        private readonly SemanticModel _semanticModel;

        private readonly ConcurrentDictionary<SyntaxNode, SymbolInfo> _symbolInfoCache = new();
        private readonly ConcurrentDictionary<string, ImmutableArray<SyntaxToken>> _identifierCache;

        private ImmutableHashSet<string>? _aliasNameSet;
        private ImmutableArray<SyntaxToken> _constructorInitializerCache;

        private FindReferenceCache(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
            _identifierCache = new(comparer: semanticModel.Language switch
            {
                LanguageNames.VisualBasic => StringComparer.OrdinalIgnoreCase,
                LanguageNames.CSharp => StringComparer.Ordinal,
                _ => throw ExceptionUtilities.UnexpectedValue(semanticModel.Language)
            });
        }

        public SymbolInfo GetSymbolInfo(SyntaxNode node, CancellationToken cancellationToken)
        {
            return _symbolInfoCache.GetOrAdd(node, static (n, arg) => arg._semanticModel.GetSymbolInfo(n, arg.cancellationToken), (_semanticModel, cancellationToken));
        }

        public IAliasSymbol? GetAliasInfo(
            ISemanticFactsService semanticFacts, SyntaxToken token, CancellationToken cancellationToken)
        {
            if (_aliasNameSet == null)
            {
                var set = semanticFacts.GetAliasNameSet(_semanticModel, cancellationToken);
                Interlocked.CompareExchange(ref _aliasNameSet, set, null);
            }

            if (_aliasNameSet.Contains(token.ValueText))
                return _semanticModel.GetAliasInfo(token.GetRequiredParent(), cancellationToken);

            return null;
        }

        public async Task<ImmutableArray<SyntaxToken>> FindMatchingIdentifierTokensAsync(
            Document document,
            string identifier,
            CancellationToken cancellationToken)
        {
            if (identifier == "")
            {
                // Certain symbols don't have a name, so we return without further searching since the text-based index
                // and lookup never terminates if searching for an empty string.
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1655431
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1744118
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1820930
                return ImmutableArray<SyntaxToken>.Empty;
            }

            // It's very costly to walk an entire tree.  So if the tree is simple and doesn't contain
            // any unicode escapes in it, then we do simple string matching to find the tokens.
            var info = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);

            // If this document doesn't even contain this identifier (escaped or non-escaped) we don't have to search it at all.
            if (!info.ProbablyContainsIdentifier(identifier))
                return ImmutableArray<SyntaxToken>.Empty;

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var root = await _semanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            // If the identifier was escaped in the file then we'll have to do a more involved search that actually
            // walks the root and checks all identifier tokens.
            //
            // otherwise, we can use the text of the document to quickly find candidates and test those directly.
            if (info.ProbablyContainsEscapedIdentifier(identifier))
            {
                return _identifierCache.GetOrAdd(identifier, _ => FindMatchingIdentifierTokensFromTree());
            }
            else
            {
                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                return _identifierCache.GetOrAdd(identifier, _ => FindMatchingIdentifierTokensFromText(text));
            }

            bool IsMatch(SyntaxToken token)
                => !token.IsMissing && syntaxFacts.IsIdentifier(token) && syntaxFacts.TextMatch(token.ValueText, identifier);

            ImmutableArray<SyntaxToken> FindMatchingIdentifierTokensFromTree()
            {
                using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var result);
                Recurse(root);
                return result.ToImmutable();

                void Recurse(SyntaxNode node)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (var child in node.ChildNodesAndTokens())
                    {
                        if (child.IsNode)
                        {
                            Recurse(child.AsNode()!);
                        }
                        else if (child.IsToken)
                        {
                            var token = child.AsToken();
                            if (IsMatch(token))
                                result.Add(token);

                            if (token.HasStructuredTrivia)
                            {
                                // structured trivia can only be leading trivia
                                foreach (var trivia in token.LeadingTrivia)
                                {
                                    if (trivia.HasStructure)
                                        Recurse(trivia.GetStructure()!);
                                }
                            }
                        }
                    }
                }
            }

            ImmutableArray<SyntaxToken> FindMatchingIdentifierTokensFromText(SourceText sourceText)
            {
                using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var result);

                var index = 0;
                while ((index = sourceText.IndexOf(identifier, index, syntaxFacts.IsCaseSensitive)) >= 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var token = root.FindToken(index, findInsideTrivia: true);
                    var span = token.Span;
                    if (span.Start == index && span.Length == identifier.Length && IsMatch(token))
                        result.Add(token);

                    var nextIndex = index + identifier.Length;
                    nextIndex = Math.Max(nextIndex, token.SpanStart);
                    index = nextIndex;
                }

                return result.ToImmutable();
            }
        }

        public IEnumerable<SyntaxToken> GetConstructorInitializerTokens(
            ISyntaxFactsService syntaxFacts, SyntaxNode root, CancellationToken cancellationToken)
        {
            // this one will only get called when we know given document contains constructor initializer.
            // no reason to use text to check whether it exist first.
            if (_constructorInitializerCache.IsDefault)
            {
                var initializers = GetConstructorInitializerTokensWorker(syntaxFacts, root, cancellationToken);
                ImmutableInterlocked.InterlockedInitialize(ref _constructorInitializerCache, initializers);
            }

            return _constructorInitializerCache;
        }

        private static ImmutableArray<SyntaxToken> GetConstructorInitializerTokensWorker(
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
    }
}
