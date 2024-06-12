// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure
{
    [UseExportProvider]
    public abstract class AbstractSyntaxStructureProviderTests
    {
        protected abstract string LanguageName { get; }

        protected virtual string WorkspaceKind => CodeAnalysis.WorkspaceKind.Host;

        internal virtual BlockStructureOptions GetDefaultOptions()
            => new()
            {
                MaximumBannerLength = 120,
                IsMetadataAsSource = WorkspaceKind == CodeAnalysis.WorkspaceKind.MetadataAsSource,
            };

        private Task<ImmutableArray<BlockSpan>> GetBlockSpansAsync(Document document, BlockStructureOptions options, int position)
            => GetBlockSpansWorkerAsync(document, options, position);

        internal abstract Task<ImmutableArray<BlockSpan>> GetBlockSpansWorkerAsync(Document document, BlockStructureOptions options, int position);

        private protected Task VerifyBlockSpansAsync(string markupCode, params RegionData[] expectedRegionData)
            => VerifyBlockSpansAsync(markupCode, GetDefaultOptions(), expectedRegionData);

        private protected async Task VerifyBlockSpansAsync(string markupCode, BlockStructureOptions options, params RegionData[] expectedRegionData)
        {
            using var workspace = TestWorkspace.Create(WorkspaceKind, LanguageName, compilationOptions: null, parseOptions: null, content: markupCode);

            var hostDocument = workspace.Documents.Single();
            Contract.ThrowIfNull(hostDocument.CursorPosition);
            var position = hostDocument.CursorPosition.Value;

            var expectedRegions = expectedRegionData.Select(data => CreateBlockSpan(data, hostDocument.AnnotatedSpans)).ToArray();

            var document = workspace.CurrentSolution.GetRequiredDocument(hostDocument.Id);
            var actualRegions = await GetBlockSpansAsync(document, options, position);

            Assert.True(expectedRegions.Length == actualRegions.Length, $"Expected {expectedRegions.Length} regions but there were {actualRegions.Length}");

            for (var i = 0; i < expectedRegions.Length; i++)
            {
                AssertRegion(expectedRegions[i], actualRegions[i]);
            }
        }

        protected async Task VerifyNoBlockSpansAsync(string markupCode)
        {
            using var workspace = TestWorkspace.Create(WorkspaceKind, LanguageName, compilationOptions: null, parseOptions: null, content: markupCode);

            var hostDocument = workspace.Documents.Single();
            Contract.ThrowIfNull(hostDocument.CursorPosition);
            var position = hostDocument.CursorPosition.Value;

            var document = workspace.CurrentSolution.GetRequiredDocument(hostDocument.Id);
            var options = GetDefaultOptions();
            var actualRegions = await GetBlockSpansAsync(document, options, position);

            Assert.True(actualRegions.Length == 0, $"Expected no regions but found {actualRegions.Length}.");
        }

        protected static RegionData Region(string textSpanName, string hintSpanName, string primaryTextSpanName, string primaryHintSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
            => new RegionData(textSpanName, hintSpanName, primaryTextSpanName, primaryHintSpanName, bannerText, autoCollapse, isDefaultCollapsed);

        protected static RegionData Region(string textSpanName, string hintSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
            => new RegionData(textSpanName, hintSpanName, primaryTextSpanName: null, primaryHintSpanName: null, bannerText, autoCollapse, isDefaultCollapsed);

        protected static RegionData Region(string textSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
            => new RegionData(textSpanName, textSpanName, primaryTextSpanName: null, primaryHintSpanName: null, bannerText, autoCollapse, isDefaultCollapsed);

        private static BlockSpan CreateBlockSpan(
            RegionData regionData,
            IDictionary<string, ImmutableArray<TextSpan>> spans)
        {
            var (textSpanName, hintSpanName, primaryTextSpanName, primaryHintSpanName, bannerText, autoCollapse, isDefaultCollapsed) = regionData;

            Assert.True(spans.ContainsKey(textSpanName) && spans[textSpanName].Length == 1, $"Test did not specify '{textSpanName}' span.");
            Assert.True(spans.ContainsKey(hintSpanName) && spans[hintSpanName].Length == 1, $"Test did not specify '{hintSpanName}' span.");

            var textSpan = spans[textSpanName][0];
            var hintSpan = spans[hintSpanName][0];

            TextSpan? primaryTextSpan = null, primaryHintSpan = null;
            if (primaryTextSpanName != null || primaryHintSpanName != null)
            {
                Contract.ThrowIfNull(primaryTextSpanName);
                Contract.ThrowIfNull(primaryHintSpanName);
                Assert.True(spans.ContainsKey(primaryTextSpanName) && spans[primaryTextSpanName].Length == 1, $"Test did not specify '{primaryTextSpanName}' span.");
                Assert.True(spans.ContainsKey(primaryHintSpanName) && spans[primaryHintSpanName].Length == 1, $"Test did not specify '{primaryHintSpanName}' span.");

                primaryTextSpan = spans[primaryTextSpanName][0];
                primaryHintSpan = spans[primaryHintSpanName][0];
            }

            return new BlockSpan(
                isCollapsible: true,
                textSpan: textSpan,
                hintSpan: hintSpan,
                primarySpans: primaryTextSpan is null || primaryHintSpan is null
                    ? null
                    : (primaryTextSpan.Value, primaryHintSpan.Value),
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

    public readonly struct RegionData
    {
        public readonly string TextSpanName;
        public readonly string HintSpanName;
        public readonly string? PrimaryTextSpanName;
        public readonly string? PrimaryHintSpanName;
        public readonly string BannerText;
        public readonly bool AutoCollapse;
        public readonly bool IsDefaultCollapsed;

        public RegionData(string textSpanName, string hintSpanName, string? primaryTextSpanName, string? primaryHintSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed)
        {
            TextSpanName = textSpanName;
            HintSpanName = hintSpanName;
            PrimaryTextSpanName = primaryTextSpanName;
            PrimaryHintSpanName = primaryHintSpanName;
            BannerText = bannerText;
            AutoCollapse = autoCollapse;
            IsDefaultCollapsed = isDefaultCollapsed;
        }

        public override bool Equals(object obj)
        {
            return obj is RegionData other
                && TextSpanName == other.TextSpanName
                && HintSpanName == other.HintSpanName
                && BannerText == other.BannerText
                && AutoCollapse == other.AutoCollapse
                && IsDefaultCollapsed == other.IsDefaultCollapsed;
        }

        public override int GetHashCode()
        {
            var hashCode = -1426774128;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TextSpanName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HintSpanName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BannerText);
            hashCode = hashCode * -1521134295 + AutoCollapse.GetHashCode();
            hashCode = hashCode * -1521134295 + IsDefaultCollapsed.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(
            out string textSpanName,
            out string hintSpanName,
            out string? primaryTextSpanName,
            out string? primaryHintSpanName,
            out string bannerText,
            out bool autoCollapse,
            out bool isDefaultCollapsed)
        {
            textSpanName = this.TextSpanName;
            hintSpanName = this.HintSpanName;
            primaryTextSpanName = PrimaryTextSpanName;
            primaryHintSpanName = PrimaryHintSpanName;
            bannerText = this.BannerText;
            autoCollapse = this.AutoCollapse;
            isDefaultCollapsed = this.IsDefaultCollapsed;
        }
    }
}
