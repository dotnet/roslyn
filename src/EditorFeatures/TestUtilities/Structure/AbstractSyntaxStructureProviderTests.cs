// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure
{
    [UseExportProvider]
    public abstract class AbstractSyntaxStructureProviderTests
    {
        protected abstract string LanguageName { get; }

        protected virtual string WorkspaceKind => CodeAnalysis.WorkspaceKind.Test;

        private Task<ImmutableArray<BlockSpan>> GetBlockSpansAsync(Document document, int position)
        {
            return GetBlockSpansWorkerAsync(document, position);
        }

        internal abstract Task<ImmutableArray<BlockSpan>> GetBlockSpansWorkerAsync(Document document, int position);

        protected async Task VerifyBlockSpansAsync(string markupCode, params Tuple<string, string, string, bool, bool>[] expectedRegionData)
        {
            using (var workspace = TestWorkspace.Create(WorkspaceKind, LanguageName, compilationOptions: null, parseOptions: null, content: markupCode))
            {
                var hostDocument = workspace.Documents.Single();
                workspace.Options = workspace.Options.WithChangedOption(
                    BlockStructureOptions.MaximumBannerLength, LanguageName, 120);
                Assert.True(hostDocument.CursorPosition.HasValue, "Test must specify a position.");
                var position = hostDocument.CursorPosition.Value;

                var expectedRegions = expectedRegionData.Select(data => CreateBlockSpan(data, hostDocument.AnnotatedSpans)).ToArray();

                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var actualRegions = await GetBlockSpansAsync(document, position);

                Assert.True(expectedRegions.Length == actualRegions.Length, $"Expected {expectedRegions.Length} regions but there were {actualRegions.Length}");

                for (var i = 0; i < expectedRegions.Length; i++)
                {
                    AssertRegion(expectedRegions[i], actualRegions[i]);
                }
            }
        }

        protected async Task VerifyNoBlockSpansAsync(string markupCode)
        {
            using (var workspace = TestWorkspace.Create(WorkspaceKind, LanguageName, compilationOptions: null, parseOptions: null, content: markupCode))
            {
                var hostDocument = workspace.Documents.Single();
                Assert.True(hostDocument.CursorPosition.HasValue, "Test must specify a position.");
                var position = hostDocument.CursorPosition.Value;

                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                var actualRegions = await GetBlockSpansAsync(document, position);

                Assert.True(actualRegions.Length == 0, $"Expected no regions but found {actualRegions.Length}.");
            }
        }

        protected Tuple<string, string, string, bool, bool> Region(string textSpanName, string hintSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
        {
            return Tuple.Create(textSpanName, hintSpanName, bannerText, autoCollapse, isDefaultCollapsed);
        }

        protected Tuple<string, string, string, bool, bool> Region(string textSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
        {
            return Tuple.Create(textSpanName, textSpanName, bannerText, autoCollapse, isDefaultCollapsed);
        }

        private static BlockSpan CreateBlockSpan(
            Tuple<string, string, string, bool, bool> regionData,
            IDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            var textSpanName = regionData.Item1;
            var hintSpanName = regionData.Item2;
            var bannerText = regionData.Item3;
            var autoCollapse = regionData.Item4;
            var isDefaultCollapsed = regionData.Item5;

            Assert.True(spans.ContainsKey(textSpanName) && spans[textSpanName].Length == 1, $"Test did not specify '{textSpanName}' span.");
            Assert.True(spans.ContainsKey(hintSpanName) && spans[hintSpanName].Length == 1, $"Test did not specify '{hintSpanName}' span.");

            var textSpan = spans[textSpanName][0];
            var hintSpan = spans[hintSpanName][0];

            return new BlockSpan(isCollapsible: true,
                textSpan: textSpan,
                hintSpan: hintSpan,
                type: BlockTypes.Nonstructural,
                bannerText: bannerText,
                autoCollapse: autoCollapse,
                isDefaultCollapsed: isDefaultCollapsed);
        }

        internal static void AssertRegion(BlockSpan expected, BlockSpan actual)
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
