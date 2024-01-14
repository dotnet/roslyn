// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using SyntaxNodeCache = Microsoft.CodeAnalysis.Syntax.InternalSyntax.SyntaxNodeCache;
using SyntaxTokenWithTrivia = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.SyntaxToken.SyntaxTokenWithTrivia;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxNodeCacheTests : CSharpTestBase
    {
        [Fact]
        public void TryGetNode_With1Child()
        {
            var child0 = new SyntaxTokenWithTrivia(SyntaxKind.IntKeyword, null, null);
            SyntaxNodeCache.AddNode(child0, child0.GetCacheHash());

            var listOf1 = new Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.PredefinedTypeSyntax(SyntaxKind.PredefinedType, child0);
            SyntaxNodeCache.AddNode(listOf1, listOf1.GetCacheHash());

            var listCached = SyntaxNodeCache.TryGetNode(listOf1.RawKind, child0, SyntaxNodeCache.GetDefaultNodeFlags(), out _);
            Assert.NotNull(listCached);
        }

        [Fact]
        public void TryGetNode_With1Of2Children()
        {
            const int nIterations = 1_000_000;

            for (int i = 0; i < nIterations; i++)
            {
                var child0 = new SyntaxTokenWithTrivia(SyntaxKind.InternalKeyword, null, null);
                var child1 = new SyntaxTokenWithTrivia(SyntaxKind.StaticKeyword, null, null);
                SyntaxNodeCache.AddNode(child0, child0.GetCacheHash());
                SyntaxNodeCache.AddNode(child1, child1.GetCacheHash());

                var listOf2 = new Microsoft.CodeAnalysis.Syntax.InternalSyntax.SyntaxList.WithTwoChildren(child0, child1);
                SyntaxNodeCache.AddNode(listOf2, listOf2.GetCacheHash());

                var listCached = SyntaxNodeCache.TryGetNode(listOf2.RawKind, child0, child1, SyntaxNodeCache.GetDefaultNodeFlags(), out _);
                Assert.NotNull(listCached);

                var listOf1 = SyntaxNodeCache.TryGetNode(listOf2.RawKind, child0, SyntaxNodeCache.GetDefaultNodeFlags(), out _);
                Assert.True(listOf2 != listOf1, $"{i} iterations");
            }
        }

        [Fact]
        public void TryGetNode_With2Of3Children()
        {
            const int nIterations = 1_000_000;

            for (int i = 0; i < nIterations; i++)
            {
                var child0 = new SyntaxTokenWithTrivia(SyntaxKind.InternalKeyword, null, null);
                var child1 = new SyntaxTokenWithTrivia(SyntaxKind.StaticKeyword, null, null);
                var child2 = new SyntaxTokenWithTrivia(SyntaxKind.ReadOnlyKeyword, null, null);
                SyntaxNodeCache.AddNode(child0, child0.GetCacheHash());
                SyntaxNodeCache.AddNode(child1, child1.GetCacheHash());
                SyntaxNodeCache.AddNode(child2, child2.GetCacheHash());

                var listOf3 = new Microsoft.CodeAnalysis.Syntax.InternalSyntax.SyntaxList.WithThreeChildren(child0, child1, child2);
                SyntaxNodeCache.AddNode(listOf3, listOf3.GetCacheHash());

                var listCached = SyntaxNodeCache.TryGetNode(listOf3.RawKind, child0, child1, child2, SyntaxNodeCache.GetDefaultNodeFlags(), out _);
                Assert.NotNull(listCached);

                var listOf2 = SyntaxNodeCache.TryGetNode(listOf3.RawKind, child0, child1, SyntaxNodeCache.GetDefaultNodeFlags(), out _);
                Assert.True(listOf3 != listOf2, $"{i} iterations");
            }
        }
    }
}
