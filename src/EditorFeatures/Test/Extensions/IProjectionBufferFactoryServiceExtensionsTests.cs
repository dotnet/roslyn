// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;

[UseExportProvider]
public class IProjectionBufferFactoryServiceExtensionsTests
{
    [Fact]
    public void TestCreateElisionBufferWithoutIndentation()
    {
        var exportProvider = EditorTestCompositions.Editor.ExportProviderFactory.CreateExportProvider();
        var contentTypeRegistryService = exportProvider.GetExportedValue<IContentTypeRegistryService>();
        var textBuffer = exportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer(
@"  line 1
  line 2
  line 3", contentTypeRegistryService.GetContentType("text"));

        var elisionBuffer = IProjectionBufferFactoryServiceExtensions.CreateProjectionBufferWithoutIndentation(
            exportProvider.GetExportedValue<IProjectionBufferFactoryService>(),
            exportProvider.GetExportedValue<IEditorOptionsFactoryService>().GlobalOptions,
            contentType: null,
            exposedSpans: textBuffer.CurrentSnapshot.GetFullSpan());

        var elisionSnapshot = elisionBuffer.CurrentSnapshot;
        Assert.Equal(3, elisionSnapshot.LineCount);

        foreach (var line in elisionSnapshot.Lines)
        {
            Assert.True(line.GetText().StartsWith("line", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void TestCreateProjectionBuffer()
    {
        var composition = EditorTestCompositions.EditorFeatures;
        var exportProvider = composition.ExportProviderFactory.CreateExportProvider();
        var contentTypeRegistryService = exportProvider.GetExportedValue<IContentTypeRegistryService>();
        var textBuffer = exportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer(
@"  line 1
  line 2
  line 3
  line 4", contentTypeRegistryService.GetContentType("text"));

        var projectionBuffer = IProjectionBufferFactoryServiceExtensions.CreateProjectionBuffer(
            exportProvider.GetExportedValue<IProjectionBufferFactoryService>(),
            exportProvider.GetExportedValue<IContentTypeRegistryService>(),
            exportProvider.GetExportedValue<IEditorOptionsFactoryService>().GlobalOptions,
            textBuffer.CurrentSnapshot,
            "...",
            LineSpan.FromBounds(1, 2), LineSpan.FromBounds(3, 4));

        var projectionSnapshot = projectionBuffer.CurrentSnapshot;
        Assert.Equal(4, projectionSnapshot.LineCount);

        var lines = projectionSnapshot.Lines.ToList();
        Assert.Equal("...", lines[0].GetText());
        Assert.Equal("  line 2", lines[1].GetText());
        Assert.Equal("...", lines[2].GetText());
        Assert.Equal("  line 4", lines[3].GetText());
    }

    [Fact]
    public void TestCreateProjectionBufferWithoutIndentation()
    {
        var composition = EditorTestCompositions.EditorFeatures;
        var exportProvider = composition.ExportProviderFactory.CreateExportProvider();
        var contentTypeRegistryService = exportProvider.GetExportedValue<IContentTypeRegistryService>();
        var textBuffer = exportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer(
@"  line 1
  line 2
  line 3
    line 4", contentTypeRegistryService.GetContentType("text"));

        var projectionBuffer = IProjectionBufferFactoryServiceExtensions.CreateProjectionBufferWithoutIndentation(
            exportProvider.GetExportedValue<IProjectionBufferFactoryService>(),
            exportProvider.GetExportedValue<IContentTypeRegistryService>(),
            exportProvider.GetExportedValue<IEditorOptionsFactoryService>().GlobalOptions,
            textBuffer.CurrentSnapshot,
            "...",
            LineSpan.FromBounds(0, 1), LineSpan.FromBounds(2, 4));

        var projectionSnapshot = projectionBuffer.CurrentSnapshot;
        Assert.Equal(4, projectionSnapshot.LineCount);

        var lines = projectionSnapshot.Lines.ToList();
        Assert.Equal("line 1", lines[0].GetText());
        Assert.Equal("...", lines[1].GetText());
        Assert.Equal("line 3", lines[2].GetText());
        Assert.Equal("  line 4", lines[3].GetText());
    }
}
