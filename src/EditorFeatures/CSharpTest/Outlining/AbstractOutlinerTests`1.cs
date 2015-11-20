// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Editor.UnitTests.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public abstract class AbstractOutlinerTests<TSyntaxNode> : AbstractOutlinerTests
        where TSyntaxNode : SyntaxNode
    {
        internal abstract AbstractSyntaxNodeOutliner<TSyntaxNode> CreateOutliner();

        internal IEnumerable<OutliningSpan> GetRegions(TSyntaxNode node)
        {
            var outliner = CreateOutliner();
            return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull();
        }

        internal OutliningSpan GetRegion(TSyntaxNode node)
        {
            var regions = GetRegions(node).ToList();
            Assert.Equal(1, regions.Count);

            return regions[0];
        }

        protected Tuple<string, string, string, bool, bool> Region(string collapseSpanName, string hintSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
        {
            return Tuple.Create(collapseSpanName, hintSpanName, bannerText, autoCollapse, isDefaultCollapsed);
        }

        protected Tuple<string, string, string, bool, bool> Region(string collapseSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
        {
            return Tuple.Create(collapseSpanName, collapseSpanName, bannerText, autoCollapse, isDefaultCollapsed);
        }

        protected void Regions(string markupCode, params Tuple<string, string, string, bool, bool>[] expectedRegions)
        {
            string code;
            int? position;
            IDictionary<string, IList<TextSpan>> spans;
            MarkupTestFile.GetPositionAndSpans(markupCode, out code, out position, out spans);

            Assert.True(position.HasValue, "Can't find node! Test did not specify position.");

            var compilationUnit = SyntaxFactory.ParseCompilationUnit(code);
            var token = compilationUnit.FindToken(position.Value, findInsideTrivia: true);
            var node = token.Parent.FirstAncestorOrSelf<TSyntaxNode>();
            Assert.NotNull(node);

            var actualRegions = GetRegions(node).ToArray();
            Assert.True(expectedRegions.Length == actualRegions.Length, $"Expected {expectedRegions.Length} regions but there were {actualRegions.Length}");

            for (int i = 0; i < actualRegions.Length; i++)
            {
                var actualRegion = actualRegions[i];
                var expectedRegion = expectedRegions[i];

                var collapseSpanName = expectedRegion.Item1;
                var hintSpanName = expectedRegion.Item2;
                var bannerText = expectedRegion.Item3;
                var autoCollapse = expectedRegion.Item4;
                var isDefaultCollapsed = expectedRegion.Item5;

                Assert.True(spans.ContainsKey(collapseSpanName) && spans[collapseSpanName].Count == 1, $"Test did not specify '{collapseSpanName}' span.");
                Assert.True(spans.ContainsKey(hintSpanName) && spans[hintSpanName].Count == 1, $"Test did not specify '{hintSpanName}' span.");

                var collapseSpan = spans[collapseSpanName][0];
                var hintSpan = spans[hintSpanName][0];

                AssertRegion(new OutliningSpan(collapseSpan, hintSpan, bannerText, autoCollapse, isDefaultCollapsed), actualRegion);
            }
        }

        protected void NoRegions(string markupCode)
        {
            string code;
            int? position;
            IDictionary<string, IList<TextSpan>> spans;
            MarkupTestFile.GetPositionAndSpans(markupCode, out code, out position, out spans);

            Assert.True(position.HasValue, "Can't find node! Test did not specify position.");

            var compilationUnit = SyntaxFactory.ParseCompilationUnit(code);
            var token = compilationUnit.FindToken(position.Value, findInsideTrivia: true);
            var node = token.Parent.FirstAncestorOrSelf<TSyntaxNode>();
            Assert.NotNull(node);

            var actualRegions = GetRegions(node).ToArray();
            Assert.True(actualRegions.Length == 0, $"Expected no regions but found {actualRegions.Length}.");
        }
    }
}
