// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    [UseExportProvider]
    public class IProjectionBufferFactoryServiceExtensionsTests
    {
        [Fact]
        public void TestCreateElisionBufferWithoutIndentation()
        {
            var exportProvider = TestExportProvider.ExportProviderWithCSharpAndVisualBasic;
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
            Assert.Equal(elisionSnapshot.LineCount, 3);

            foreach (var line in elisionSnapshot.Lines)
            {
                Assert.True(line.GetText().StartsWith("line", StringComparison.Ordinal));
            }
        }

        [Fact]
        public void TestCreateProjectionBuffer()
        {
            var exportProvider = TestExportProvider.ExportProviderWithCSharpAndVisualBasic;
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
            Assert.Equal(projectionSnapshot.LineCount, 4);

            var lines = projectionSnapshot.Lines.ToList();
            Assert.Equal(lines[0].GetText(), "...");
            Assert.Equal(lines[1].GetText(), "  line 2");
            Assert.Equal(lines[2].GetText(), "...");
            Assert.Equal(lines[3].GetText(), "  line 4");
        }

        [Fact]
        public void TestCreateProjectionBufferWithoutIndentation()
        {
            var exportProvider = TestExportProvider.ExportProviderWithCSharpAndVisualBasic;
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
            Assert.Equal(projectionSnapshot.LineCount, 4);

            var lines = projectionSnapshot.Lines.ToList();
            Assert.Equal(lines[0].GetText(), "line 1");
            Assert.Equal(lines[1].GetText(), "...");
            Assert.Equal(lines[2].GetText(), "line 3");
            Assert.Equal(lines[3].GetText(), "  line 4");
        }
    }
}
