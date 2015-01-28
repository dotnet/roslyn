// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Editor.Implementation.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public class TextFactoryTests
    {
        private byte[] _nonUTF8StringBytes = new byte[] { 0x80, 0x92, 0xA4, 0xB6, 0xC9, 0xDB, 0xED, 0xFF };

        [Fact, WorkItem(1038018), WorkItem(1041792)]
        public void TestCreateTextFallsBackToSystemDefaultEncoding()
        {
            TestCreateTextInferredEncoding(
                _nonUTF8StringBytes,
                defaultEncoding: null,
                expectedEncoding: Encoding.Default);
        }

        [Fact, WorkItem(1038018)]
        public void TestCreateTextFallsBackToUTF8Encoding()
        {
            TestCreateTextInferredEncoding(
                new ASCIIEncoding().GetBytes("Test"),
                defaultEncoding: null,
                expectedEncoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
        }

        [Fact, WorkItem(1038018)]
        public void TestCreateTextFallsBackToProvidedDefaultEncoding()
        {
            TestCreateTextInferredEncoding(
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetBytes("Test"),
                defaultEncoding: Encoding.GetEncoding(1254),
                expectedEncoding: Encoding.GetEncoding(1254));
        }

        [Fact, WorkItem(1038018)]
        public void TestCreateTextUsesByteOrderMarkIfPresent()
        {
            TestCreateTextInferredEncoding(
                Encoding.UTF8.GetPreamble().Concat(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetBytes("Test")).ToArray(),
                defaultEncoding: Encoding.GetEncoding(1254),
                expectedEncoding: Encoding.UTF8);
        }

        private void TestCreateTextInferredEncoding(byte[] bytes, Encoding defaultEncoding, Encoding expectedEncoding)
        {
            var mockTextBufferFactoryService = new Mock<ITextBufferFactoryService>();
            mockTextBufferFactoryService
                .Setup(t => t.CreateTextBuffer(It.IsAny<TextReader>(), It.IsAny<IContentType>()))
                .Returns<TextReader, IContentType>((reader, contentType) =>
                    {
                        reader.ReadToEnd();

                        var mockTextBuffer = new Mock<ITextBuffer>();
                        mockTextBuffer.Setup(b => b.CurrentSnapshot).Returns(new Mock<ITextSnapshot>().Object);
                        return mockTextBuffer.Object;
                    });

            var factory = new EditorTextFactoryService(mockTextBufferFactoryService.Object, new Mock<IContentTypeRegistryService>().Object);
            using (var stream = new MemoryStream(bytes))
            {
                var text = factory.CreateText(stream, defaultEncoding);
                Assert.Equal(expectedEncoding, text.Encoding);
            }
        }
    }
}
