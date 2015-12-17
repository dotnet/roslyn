// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Editor.Implementation.Workspaces;
using Microsoft.CodeAnalysis.Host;
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

        [Fact]
        public void TestCreateFromTemporaryStorage()
        {
            var textFactory = CreateMockTextFactoryService();
            var temporaryStorageService = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);

            var text = Text.SourceText.From("Hello, World!");

            // Create a temporary storage location
            using (var temporaryStorage = temporaryStorageService.CreateTemporaryTextStorage(System.Threading.CancellationToken.None))
            {
                // Write text into it
                temporaryStorage.WriteTextAsync(text).Wait();

                // Read text back from it
                var text2 = temporaryStorage.ReadTextAsync().Result;

                Assert.NotSame(text, text2);
                Assert.Equal(text.ToString(), text2.ToString());
                Assert.Equal(text2.Encoding, null);
            }
        }

        [Fact]
        public void TestCreateFromTemporaryStorageWithEncoding()
        {
            var textFactory = CreateMockTextFactoryService();
            var temporaryStorageService = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);

            var text = Text.SourceText.From("Hello, World!", Encoding.ASCII);

            // Create a temporary storage location
            using (var temporaryStorage = temporaryStorageService.CreateTemporaryTextStorage(System.Threading.CancellationToken.None))
            {
                // Write text into it
                temporaryStorage.WriteTextAsync(text).Wait();

                // Read text back from it
                var text2 = temporaryStorage.ReadTextAsync().Result;

                Assert.NotSame(text, text2);
                Assert.Equal(text.ToString(), text2.ToString());
                Assert.Equal(text2.Encoding, Encoding.ASCII);
            }
        }

        private EditorTextFactoryService CreateMockTextFactoryService()
        {
            var mockTextBufferFactoryService = new Mock<ITextBufferFactoryService>();
            mockTextBufferFactoryService
                .Setup(t => t.CreateTextBuffer(It.IsAny<TextReader>(), It.IsAny<IContentType>()))
                .Returns<TextReader, IContentType>((reader, contentType) =>
                {
                    var text = reader.ReadToEnd();

                    var mockSnapshot = new Mock<ITextSnapshot>();
                    mockSnapshot.Setup(s => s.GetText()).Returns(text);

                    var mockTextBuffer = new Mock<ITextBuffer>();
                    mockTextBuffer.Setup(b => b.CurrentSnapshot).Returns(mockSnapshot.Object);
                    return mockTextBuffer.Object;
                });

            return new EditorTextFactoryService(mockTextBufferFactoryService.Object, new Mock<IContentTypeRegistryService>().Object);
        }

        private void TestCreateTextInferredEncoding(byte[] bytes, Encoding defaultEncoding, Encoding expectedEncoding)
        {
            var factory = CreateMockTextFactoryService();
            using (var stream = new MemoryStream(bytes))
            {
                var text = factory.CreateText(stream, defaultEncoding);
                Assert.Equal(expectedEncoding, text.Encoding);
            }
        }
    }
}
