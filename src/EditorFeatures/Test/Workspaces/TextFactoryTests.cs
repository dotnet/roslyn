// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    [UseExportProvider]
    public class TextFactoryTests
    {
        private readonly byte[] _nonUTF8StringBytes = new byte[] { 0x80, 0x92, 0xA4, 0xB6, 0xC9, 0xDB, 0xED, 0xFF };

        [Fact, WorkItem(1038018, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1038018"), WorkItem(1041792, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1041792")]
        public void TestCreateTextFallsBackToSystemDefaultEncoding()
        {
            TestCreateTextInferredEncoding(
                _nonUTF8StringBytes,
                defaultEncoding: null,
                expectedEncoding: Encoding.Default);
        }

        [Fact, WorkItem(1038018, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1038018")]
        public void TestCreateTextFallsBackToUTF8Encoding()
        {
            TestCreateTextInferredEncoding(
                new ASCIIEncoding().GetBytes("Test"),
                defaultEncoding: null,
                expectedEncoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
        }

        [Fact, WorkItem(1038018, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1038018")]
        public void TestCreateTextFallsBackToProvidedDefaultEncoding()
        {
            TestCreateTextInferredEncoding(
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetBytes("Test"),
                defaultEncoding: Encoding.GetEncoding(1254),
                expectedEncoding: Encoding.GetEncoding(1254));
        }

        [Fact, WorkItem(1038018, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1038018")]
        public void TestCreateTextUsesByteOrderMarkIfPresent()
        {
            TestCreateTextInferredEncoding(
                Encoding.UTF8.GetPreamble().Concat(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetBytes("Test")).ToArray(),
                defaultEncoding: Encoding.GetEncoding(1254),
                expectedEncoding: Encoding.UTF8);
        }

        [Fact]
        public async Task TestCreateFromTemporaryStorage()
        {
            var textFactory = CreateMockTextFactoryService();
            var temporaryStorageService = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);

            var text = SourceText.From("Hello, World!");

            // Create a temporary storage location
            using var temporaryStorage = temporaryStorageService.CreateTemporaryTextStorage(System.Threading.CancellationToken.None);
            // Write text into it
            await temporaryStorage.WriteTextAsync(text);

            // Read text back from it
            var text2 = await temporaryStorage.ReadTextAsync();

            Assert.NotSame(text, text2);
            Assert.Equal(text.ToString(), text2.ToString());
            Assert.Null(text2.Encoding);
        }

        [Fact]
        public async Task TestCreateFromTemporaryStorageWithEncoding()
        {
            var textFactory = CreateMockTextFactoryService();
            var temporaryStorageService = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);

            var text = SourceText.From("Hello, World!", Encoding.ASCII);

            // Create a temporary storage location
            using var temporaryStorage = temporaryStorageService.CreateTemporaryTextStorage(System.Threading.CancellationToken.None);
            // Write text into it
            await temporaryStorage.WriteTextAsync(text);

            // Read text back from it
            var text2 = await temporaryStorage.ReadTextAsync();

            Assert.NotSame(text, text2);
            Assert.Equal(text.ToString(), text2.ToString());
            Assert.Equal(text2.Encoding, Encoding.ASCII);
        }

        private EditorTextFactoryService CreateMockTextFactoryService()
        {
            var mockTextBufferFactoryService = new Mock<ITextBufferFactoryService>();
            mockTextBufferFactoryService
                .Setup(t => t.CreateTextBuffer(It.IsAny<TextReader>(), It.IsAny<IContentType>()))
                .Returns<TextReader, IContentType>((reader, contentType) =>
                {
                    var text = reader.ReadToEnd();

                    var mockImage = new Mock<ITextImage>();
                    mockImage.Setup(i => i.GetText(It.IsAny<Span>())).Returns(text);

                    var mockSnapshot = new Mock<ITextSnapshot2>();
                    mockSnapshot.Setup(s => s.TextImage).Returns(mockImage.Object);
                    mockSnapshot.Setup(s => s.GetText()).Returns(text);

                    var mockTextBuffer = new Mock<ITextBuffer>();
                    mockTextBuffer.Setup(b => b.CurrentSnapshot).Returns(mockSnapshot.Object);
                    return mockTextBuffer.Object;
                });

            return new EditorTextFactoryService(new FakeTextBufferCloneService(), mockTextBufferFactoryService.Object, new Mock<IContentTypeRegistryService>().Object);
        }

        private void TestCreateTextInferredEncoding(byte[] bytes, Encoding defaultEncoding, Encoding expectedEncoding)
        {
            var factory = CreateMockTextFactoryService();
            using var stream = new MemoryStream(bytes);
            var text = factory.CreateText(stream, defaultEncoding);
            Assert.Equal(expectedEncoding, text.Encoding);
        }

        private class FakeTextBufferCloneService : ITextBufferCloneService
        {
            public ITextBuffer CloneWithUnknownContentType(SnapshotSpan span) => throw new NotImplementedException();

            public ITextBuffer CloneWithUnknownContentType(ITextImage textImage) => throw new NotImplementedException();

            public ITextBuffer CloneWithRoslynContentType(SourceText sourceText) => throw new NotImplementedException();

            public ITextBuffer Clone(SourceText sourceText, IContentType contentType) => throw new NotImplementedException();

        }
    }
}
