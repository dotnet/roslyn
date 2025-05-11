// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.TextEditor;

[UseExportProvider]
public sealed class TextBufferAssociatedViewServiceTests
{
    [Fact]
    public void SanityCheck()
    {
        var viewMock = new Mock<IWpfTextView>(MockBehavior.Strict);
        var viewMock2 = new Mock<IWpfTextView>(MockBehavior.Strict);

        var contentType = new Mock<IContentType>(MockBehavior.Strict);
        contentType.Setup(c => c.IsOfType(ContentTypeNames.RoslynContentType)).Returns(true);

        var bufferMock = new Mock<ITextBuffer>(MockBehavior.Strict);
        bufferMock.Setup(b => b.ContentType).Returns(contentType.Object);

        var bufferCollection = new Collection<ITextBuffer>([bufferMock.Object]);
        var dummyReason = ConnectionReason.BufferGraphChange;

        var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
        var service = Assert.IsType<TextBufferAssociatedViewService>(exportProvider.GetExportedValue<ITextBufferAssociatedViewService>());

        ((ITextViewConnectionListener)service).SubjectBuffersConnected(viewMock.Object, dummyReason, bufferCollection);
        Assert.Equal(1, service.GetAssociatedTextViews(bufferMock.Object).Count());

        ((ITextViewConnectionListener)service).SubjectBuffersDisconnected(viewMock.Object, dummyReason, bufferCollection);
        Assert.Equal(0, service.GetAssociatedTextViews(bufferMock.Object).Count());

        ((ITextViewConnectionListener)service).SubjectBuffersConnected(viewMock.Object, dummyReason, bufferCollection);
        ((ITextViewConnectionListener)service).SubjectBuffersConnected(viewMock2.Object, dummyReason, bufferCollection);
        Assert.Equal(2, service.GetAssociatedTextViews(bufferMock.Object).Count());

        ((ITextViewConnectionListener)service).SubjectBuffersDisconnected(viewMock.Object, dummyReason, bufferCollection);
        Assert.Equal(1, service.GetAssociatedTextViews(bufferMock.Object).Count());

        ((ITextViewConnectionListener)service).SubjectBuffersDisconnected(viewMock2.Object, dummyReason, bufferCollection);
        Assert.Equal(0, service.GetAssociatedTextViews(bufferMock.Object).Count());
    }
}
