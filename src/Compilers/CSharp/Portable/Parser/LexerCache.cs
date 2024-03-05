// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// #define COLLECT_STATS

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class LexerCache
    {
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

        private readonly TextKeyedCache<SyntaxTrivia> _triviaMap;
        private readonly TextKeyedCache<SyntaxToken> _tokenMap;
        private readonly CachingIdentityFactory<string, SyntaxKind> _keywordKindMap;
        internal const int MaxKeywordLength = 10;

        internal LexerCache()
        {
            _triviaMap = TextKeyedCache<SyntaxTrivia>.GetInstance();
            _tokenMap = TextKeyedCache<SyntaxToken>.GetInstance();
            _keywordKindMap = s_keywordKindPool.Allocate();
        }

        internal void Free()
        {
            _keywordKindMap.Free();
            _triviaMap.Free();
            _tokenMap.Free();
        }

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
