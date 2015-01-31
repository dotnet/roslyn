// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public abstract class AbstractOutlinerTests<TSyntaxNode> :
        AbstractOutlinerTests
        where TSyntaxNode : SyntaxNode
    {
        internal virtual IEnumerable<OutliningSpan> GetRegions(TSyntaxNode node)
        {
            return Enumerable.Empty<OutliningSpan>();
        }

        internal OutliningSpan GetRegion(TSyntaxNode node)
        {
            var regions = GetRegions(node).ToList();
            Assert.Equal(1, regions.Count);

            return regions[0];
        }

        protected void TestTrivia(string expectedRegionName, string code, SyntaxKind expectedKind, bool autoCollapse)
        {
            int caretPosition;
            IList<TextSpan> spans;
            MarkupTestFile.GetPositionAndSpans(code, out code, out caretPosition, out spans);

            var tree = ParseCode(code);
            var trivia = tree.GetRoot().FindTrivia(caretPosition);
            var directive = trivia.GetStructure() as TSyntaxNode;
            Assert.NotNull(directive);
            Assert.Equal(expectedKind, directive.Kind());

            var actualRegions = GetRegions(directive).ToArray();
            if (spans.Count == 0)
            {
                Assert.Equal(0, actualRegions.Length);
                return;
            }

            Assert.Equal(1, actualRegions.Length);
            var expectedRegion = new OutliningSpan(
                spans[0],
                expectedRegionName,
                autoCollapse);

            AssertRegion(expectedRegion, actualRegions[0]);
        }
    }
}
