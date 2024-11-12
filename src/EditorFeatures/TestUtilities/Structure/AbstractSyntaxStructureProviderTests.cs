// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure;

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

    protected static RegionData Region(string textSpanName, string hintSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
        => new(textSpanName, hintSpanName, bannerText, autoCollapse, isDefaultCollapsed);

    protected static RegionData Region(string textSpanName, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
        => new(textSpanName, textSpanName, bannerText, autoCollapse, isDefaultCollapsed);

    private static BlockSpan CreateBlockSpan(
        RegionData regionData,
        IDictionary<string, ImmutableArray<TextSpan>> spans)
    {
        var (textSpanName, hintSpanName, bannerText, autoCollapse, isDefaultCollapsed) = regionData;

        Assert.True(spans.ContainsKey(textSpanName) && spans[textSpanName].Length == 1, $"Test did not specify '{textSpanName}' span.");
        Assert.True(spans.ContainsKey(hintSpanName) && spans[hintSpanName].Length == 1, $"Test did not specify '{hintSpanName}' span.");

        var textSpan = spans[textSpanName][0];
        var hintSpan = spans[hintSpanName][0];

        return new BlockSpan(
            isCollapsible: true,
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

public readonly record struct RegionData(
    string TextSpanName,
    string HintSpanName,
    string BannerText,
    bool AutoCollapse,
    bool IsDefaultCollapsed);
