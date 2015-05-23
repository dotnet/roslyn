using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public class IBufferGraphExtensionsTests
    {
        [Fact]
        public void TestElisionBufferMappingWorkaround()
        {
            var bufferContents = @"
Some text on a line
Another line$$
A third line";

            int position;
            string text;
            MarkupTestFile.GetPosition(bufferContents, out text, out position);

            var exportProvider = TestExportProvider.ExportProviderWithCSharpAndVisualBasic;
            var contentTypeRegistryService = exportProvider.GetExportedValue<IContentTypeRegistryService>();
            var textBuffer = exportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer(text, contentTypeRegistryService.GetContentType("text"));

            var factory = exportProvider.GetExportedValue<IProjectionBufferFactoryService>();
            var exposedSpans = new SnapshotSpan[] { new SnapshotSpan(textBuffer.CurrentSnapshot, Span.FromBounds(position, position)) };
            var elision = factory.CreateElisionBuffer(null, new NormalizedSnapshotSpanCollection(exposedSpans), ElisionBufferOptions.None);
            var textView = exportProvider.GetExportedValue<ITextEditorFactoryService>().CreateTextView(elision);

            var nullSpan = new SnapshotSpan(elision.CurrentSnapshot, Span.FromBounds(0, 0));
            var span = IBufferGraphExtensions.MapUpOrDownToBuffer(textView.BufferGraph, nullSpan, textBuffer);
            Assert.Equal(span.Value, new SnapshotSpan(textBuffer.CurrentSnapshot, new Span(position, 0)));
        }
    }
}
