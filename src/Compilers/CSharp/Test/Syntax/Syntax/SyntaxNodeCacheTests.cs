// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Syntax.InternalSyntax;
using Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;
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
            SyntaxNodeCache.AddNode(child0, SyntaxNodeCache.GetCacheHash(child0));

            var listOf1 = new PredefinedTypeSyntax(SyntaxKind.PredefinedType, child0);
            SyntaxNodeCache.AddNode(listOf1, SyntaxNodeCache.GetCacheHash(listOf1));

            var listCached = (PredefinedTypeSyntax)SyntaxNodeCache.TryGetNode(listOf1.RawKind, child0, SyntaxNodeCache.GetDefaultNodeFlags(), out _);
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
                SyntaxNodeCache.AddNode(child0, SyntaxNodeCache.GetCacheHash(child0));
                SyntaxNodeCache.AddNode(child1, SyntaxNodeCache.GetCacheHash(child1));

                var listOf2 = new CodeAnalysis.Syntax.InternalSyntax.SyntaxList.WithTwoChildren(child0, child1);
                SyntaxNodeCache.AddNode(listOf2, SyntaxNodeCache.GetCacheHash(listOf2));

                var listCached = (CodeAnalysis.Syntax.InternalSyntax.SyntaxList.WithTwoChildren)SyntaxNodeCache.TryGetNode(listOf2.RawKind, child0, child1, SyntaxNodeCache.GetDefaultNodeFlags(), out _);
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
                SyntaxNodeCache.AddNode(child0, SyntaxNodeCache.GetCacheHash(child0));
                SyntaxNodeCache.AddNode(child1, SyntaxNodeCache.GetCacheHash(child1));
                SyntaxNodeCache.AddNode(child2, SyntaxNodeCache.GetCacheHash(child2));

                var listOf3 = new CodeAnalysis.Syntax.InternalSyntax.SyntaxList.WithThreeChildren(child0, child1, child2);
                SyntaxNodeCache.AddNode(listOf3, SyntaxNodeCache.GetCacheHash(listOf3));

                var listCached = (CodeAnalysis.Syntax.InternalSyntax.SyntaxList.WithThreeChildren)SyntaxNodeCache.TryGetNode(listOf3.RawKind, child0, child1, child2, SyntaxNodeCache.GetDefaultNodeFlags(), out _);
                Assert.NotNull(listCached);

                var listOf2 = SyntaxNodeCache.TryGetNode(listOf3.RawKind, child0, child1, SyntaxNodeCache.GetDefaultNodeFlags(), out _);
                Assert.True(listOf3 != listOf2, $"{i} iterations");
            }
        }
    }
}
