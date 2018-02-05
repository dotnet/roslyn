// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.TextEditor
{
    public class TextBufferAssociatedViewServiceTests
    {
        [Fact]
        public void SanityCheck()
        {
            var viewMock = new Mock<IWpfTextView>();
            var viewMock2 = new Mock<IWpfTextView>();

            var contentType = new Mock<IContentType>();
            contentType.Setup(c => c.IsOfType(ContentTypeNames.RoslynContentType)).Returns(true);

            var bufferMock = new Mock<ITextBuffer>();
            bufferMock.Setup(b => b.ContentType).Returns(contentType.Object);

            var bufferCollection = new Collection<ITextBuffer>(SpecializedCollections.SingletonEnumerable(bufferMock.Object).ToList());
            var dummyReason = ConnectionReason.BufferGraphChange;

            var service = new TextBufferAssociatedViewService();

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
}
