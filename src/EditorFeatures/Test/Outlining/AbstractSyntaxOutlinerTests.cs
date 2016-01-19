// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Outlining
{
    public abstract class AbstractSyntaxOutlinerTests
    {
        protected abstract string LanguageName { get; }

        protected virtual string WorkspaceKind => TestWorkspace.WorkspaceName;

        internal abstract Task<OutliningSpan[]> GetRegionsAsync(Document document, int position);

        protected async Task VerifyRegionsAsync(string markupCode, params Tuple<string, string, string, bool, bool>[] expectedRegionData)
        {
            using (var workspace = await TestWorkspaceFactory.CreateWorkspaceFromFileAsync(WorkspaceKind, LanguageName, compilationOptions: null, parseOptions: null, content: markupCode))
            {
                var hostDocument = workspace.Documents.Single();
                Assert.True(hostDocument.CursorPosition.HasValue, "Test must specify a position.");
                var position = hostDocument.CursorPosition.Value;

                var expectedRegions = expectedRegionData.Select(data => CreateOutliningSpan(data, hostDocument.AnnotatedSpans)).ToArray();

                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var actualRegions = await GetRegionsAsync(document, position);

                Assert.True(expectedRegions.Length == actualRegions.Length, $"Expected {expectedRegions.Length} regions but there were {actualRegions.Length}");

                for (int i = 0; i < expectedRegions.Length; i++)
                {
                    AssertRegion(expectedRegions[i], actualRegions[i]);
                }
            }
        }

        protected async Task VerifyNoRegionsAsync(string markupCode)
        {
            using (var workspace = await TestWorkspaceFactory.CreateWorkspaceFromFileAsync(WorkspaceKind, LanguageName, compilationOptions: null, parseOptions: null, content: markupCode))
            {
                var hostDocument = workspace.Documents.Single();
                Assert.True(hostDocument.CursorPosition.HasValue, "Test must specify a position.");
                var position = hostDocument.CursorPosition.Value;

                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var actualRegions = await GetRegionsAsync(document, position);

                Assert.True(actualRegions.Length == 0, $"Expected no regions but found {actualRegions.Length}.");
            }
        }

        protected Tuple<string, string, string, bool, bool> Region(string collapseSpanName, string hintSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
        {
            return Tuple.Create(collapseSpanName, hintSpanName, bannerText, autoCollapse, isDefaultCollapsed);
        }

        protected Tuple<string, string, string, bool, bool> Region(string collapseSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
        {
            return Tuple.Create(collapseSpanName, collapseSpanName, bannerText, autoCollapse, isDefaultCollapsed);
        }

        private static OutliningSpan CreateOutliningSpan(Tuple<string, string, string, bool, bool> regionData, IDictionary<string, IList<TextSpan>> spans)
        {
            var collapseSpanName = regionData.Item1;
            var hintSpanName = regionData.Item2;
            var bannerText = regionData.Item3;
            var autoCollapse = regionData.Item4;
            var isDefaultCollapsed = regionData.Item5;

            Assert.True(spans.ContainsKey(collapseSpanName) && spans[collapseSpanName].Count == 1, $"Test did not specify '{collapseSpanName}' span.");
            Assert.True(spans.ContainsKey(hintSpanName) && spans[hintSpanName].Count == 1, $"Test did not specify '{hintSpanName}' span.");

            var collapseSpan = spans[collapseSpanName][0];
            var hintSpan = spans[hintSpanName][0];

            return new OutliningSpan(collapseSpan, hintSpan, bannerText, autoCollapse, isDefaultCollapsed);
        }

        internal static void AssertRegion(OutliningSpan expected, OutliningSpan actual)
        {
            Assert.Equal(expected.TextSpan.Start, actual.TextSpan.Start);
            Assert.Equal(expected.TextSpan.End, actual.TextSpan.End);
            Assert.Equal(expected.HintSpan.Start, actual.HintSpan.Start);
            Assert.Equal(expected.HintSpan.End, actual.HintSpan.End);
            Assert.Equal(expected.BannerText, actual.BannerText);
            Assert.Equal(expected.AutoCollapse, actual.AutoCollapse);
            Assert.Equal(expected.IsDefaultCollapsed, actual.IsDefaultCollapsed);
        }
    }
}
