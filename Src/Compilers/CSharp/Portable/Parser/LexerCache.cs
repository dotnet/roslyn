// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// #define COLLECT_STATS

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class LexerCache
    {
        private static readonly ObjectPool<CachingIdentityFactory<string, SyntaxKind>> keywordKindPool =
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

        private readonly TextKeyedCache<SyntaxTrivia> triviaMap;
        private readonly TextKeyedCache<SyntaxToken> tokenMap;
        private readonly CachingIdentityFactory<string, SyntaxKind> keywordKindMap;

        internal LexerCache()
        {
            triviaMap = TextKeyedCache<SyntaxTrivia>.GetInstance();
            tokenMap = TextKeyedCache<SyntaxToken>.GetInstance();
            keywordKindMap = keywordKindPool.Allocate();
        }

        internal void Free()
        {
            keywordKindMap.Free();
            triviaMap.Free();
            tokenMap.Free();
        }

        internal bool TryGetKeywordKind(string key, out SyntaxKind kind)
        {
            if (key.Length > 10)
            {
                kind = SyntaxKind.None;
                return false;
            }

            kind = keywordKindMap.GetOrMakeValue(key);
            return kind != SyntaxKind.None;
        }

        internal SyntaxTrivia LookupTrivia(
            char[] textBuffer,
            int keyStart,
            int keyLength,
            int hashCode,
            Func<SyntaxTrivia> createTriviaFunction)
        {
            var value = triviaMap.FindItem(textBuffer, keyStart, keyLength, hashCode);

            if (value == null)
            {
                value = createTriviaFunction();
                triviaMap.AddItem(textBuffer, keyStart, keyLength, hashCode, value);
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


        internal SyntaxToken LookupToken(
            char[] textBuffer,
            int keyStart,
            int keyLength,
            int hashCode,
            Func<SyntaxToken> createTokenFunction)
        {
            var value = tokenMap.FindItem(textBuffer, keyStart, keyLength, hashCode);

            if (value == null)
            {
#if COLLECT_STATS
                    Miss();
#endif
                value = createTokenFunction();
                tokenMap.AddItem(textBuffer, keyStart, keyLength, hashCode, value);
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