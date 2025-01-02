// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// #define COLLECT_STATS

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Syntax.InternalSyntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal class LexerCache
    {
        private static readonly ObjectPool<LexerCache> s_lexerCachePool = new ObjectPool<LexerCache>(() => new LexerCache());

        private static readonly ObjectPool<CachingIdentityFactory<string, SyntaxKind>> s_keywordKindPool =
            CachingIdentityFactory<string, SyntaxKind>.CreatePool(
                            512,
                            (key) =>
                            {
                                var kind = SyntaxFacts.GetKeywordKind(key);
                                if (kind == SyntaxKind.None)
                                {
                                    kind = SyntaxFacts.GetContextualKeywordKind(key);
                                }

                                return kind;
                            });

        private TextKeyedCache<SyntaxTrivia> _triviaMap;
        private TextKeyedCache<SyntaxToken> _tokenMap;
        private CachingIdentityFactory<string, SyntaxKind> _keywordKindMap;
        internal const int MaxKeywordLength = 10;

        private PooledStringBuilder _stringBuilder;
        private readonly char[] _identBuffer;
        private SyntaxListBuilder _leadingTriviaCache;
        private SyntaxListBuilder _trailingTriviaCache;

        private const int LeadingTriviaCacheInitialCapacity = 128;
        private const int TrailingTriviaCacheInitialCapacity = 16;

        private LexerCache()
        {
            _identBuffer = new char[32];

            Initialize();
        }

        [MemberNotNull(nameof(_triviaMap), nameof(_tokenMap), nameof(_keywordKindMap), nameof(_stringBuilder), nameof(_leadingTriviaCache), nameof(_trailingTriviaCache))]
        private void Initialize()
        {
            _triviaMap = TextKeyedCache<SyntaxTrivia>.GetInstance();
            _tokenMap = TextKeyedCache<SyntaxToken>.GetInstance();
            _keywordKindMap = s_keywordKindPool.Allocate();
            _stringBuilder = PooledStringBuilder.GetInstance();

            // Create new trivia caches if the existing ones have grown too large for pooling.
            if (_leadingTriviaCache is null || _leadingTriviaCache.Capacity > LeadingTriviaCacheInitialCapacity * 2)
            {
                _leadingTriviaCache = new SyntaxListBuilder(LeadingTriviaCacheInitialCapacity);
            }

            if (_trailingTriviaCache is null || _trailingTriviaCache.Capacity > TrailingTriviaCacheInitialCapacity * 2)
            {
                _trailingTriviaCache = new SyntaxListBuilder(TrailingTriviaCacheInitialCapacity);
            }
        }

        public static LexerCache Allocate()
        {
            var lexerCache = s_lexerCachePool.Allocate();

            lexerCache.Initialize();

            return lexerCache;
        }

        internal static void Free(LexerCache cache)
        {
            cache.Free();

            s_lexerCachePool.Free(cache);
        }

        private void Free()
        {
            _keywordKindMap.Free();
            _triviaMap.Free();
            _tokenMap.Free();

            // Use a pooled string builder as it's Free method understands whether it's too large
            // to return to the pool
            _stringBuilder.Free();

            _leadingTriviaCache.Clear();
            _trailingTriviaCache.Clear();
        }

        internal StringBuilder StringBuilder => _stringBuilder;
        internal char[] IdentBuffer => _identBuffer;
        internal SyntaxListBuilder LeadingTriviaCache => _leadingTriviaCache;
        internal SyntaxListBuilder TrailingTriviaCache => _trailingTriviaCache;

        internal bool TryGetKeywordKind(string key, out SyntaxKind kind)
        {
            if (key.Length > MaxKeywordLength)
            {
                kind = SyntaxKind.None;
                return false;
            }

            kind = _keywordKindMap.GetOrMakeValue(key);
            return kind != SyntaxKind.None;
        }

        internal SyntaxTrivia LookupTrivia<TArg>(
            char[] textBuffer,
            int keyStart,
            int keyLength,
            int hashCode,
            Func<TArg, SyntaxTrivia> createTriviaFunction,
            TArg data)
        {
            var value = _triviaMap.FindItem(textBuffer, keyStart, keyLength, hashCode);

            if (value == null)
            {
                value = createTriviaFunction(data);
                _triviaMap.AddItem(textBuffer, keyStart, keyLength, hashCode, value);
            }

            return value;
        }

        // TODO: remove this when done tweaking this cache.
#if COLLECT_STATS
        private static int hits = 0;
        private static int misses = 0;

        private static void Hit()
        {
            var h = System.Threading.Interlocked.Increment(ref hits);

            if (h % 10000 == 0)
            {
                Console.WriteLine(h * 100 / (h + misses));
            }
        }

        private static void Miss()
        {
            System.Threading.Interlocked.Increment(ref misses);
        }
#endif

        internal SyntaxToken LookupToken<TArg>(
            char[] textBuffer,
            int keyStart,
            int keyLength,
            int hashCode,
            Func<TArg, SyntaxToken> createTokenFunction,
            TArg data)
        {
            var value = _tokenMap.FindItem(textBuffer, keyStart, keyLength, hashCode);

            if (value == null)
            {
#if COLLECT_STATS
                    Miss();
#endif
                value = createTokenFunction(data);
                _tokenMap.AddItem(textBuffer, keyStart, keyLength, hashCode, value);
            }
            else
            {
#if COLLECT_STATS
                    Hit();
#endif
            }

            return value;
        }
    }
}
