// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Text
{
    public class SourceTextTests
    {
        private static readonly Encoding s_utf8 = Encoding.UTF8;
        private static readonly Encoding s_unicode = Encoding.Unicode;
        private const string HelloWorld = "Hello, World!";

        [Fact]
        public void Empty()
        {
            TestIsEmpty(SourceText.From(string.Empty));
            TestIsEmpty(SourceText.From(new byte[0], 0));
            TestIsEmpty(SourceText.From(new MemoryStream()));
        }

        private static void TestIsEmpty(SourceText text)
        {
            Assert.Equal(0, text.Length);
            Assert.Same(string.Empty, text.ToString());
            Assert.Equal(1, text.Lines.Count);
            Assert.Equal(0, text.Lines[0].Span.Length);
        }

        [Fact]
        public void Encoding1()
        {
            var utf8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            Assert.Same(s_utf8, SourceText.From(HelloWorld, s_utf8).Encoding);
            Assert.Same(s_unicode, SourceText.From(HelloWorld, s_unicode).Encoding);

            var bytes = s_unicode.GetBytes(HelloWorld);
            Assert.Same(s_unicode, SourceText.From(bytes, bytes.Length, s_unicode).Encoding);
            Assert.Equal(utf8NoBOM, SourceText.From(bytes, bytes.Length, null).Encoding);

            var stream = new MemoryStream(bytes);
            Assert.Same(s_unicode, SourceText.From(stream, s_unicode).Encoding);
            Assert.Equal(utf8NoBOM, SourceText.From(stream, null).Encoding);
        }

        [Fact]
        public void EncodingBOM()
        {
            var utf8BOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

            var bytes = utf8BOM.GetPreamble().Concat(utf8BOM.GetBytes("abc")).ToArray();
            Assert.Equal(utf8BOM, SourceText.From(bytes, bytes.Length, s_unicode).Encoding);
            Assert.Equal(utf8BOM, SourceText.From(bytes, bytes.Length, null).Encoding);

            var stream = new MemoryStream(bytes);
            Assert.Equal(utf8BOM, SourceText.From(stream, s_unicode).Encoding);
            Assert.Equal(utf8BOM, SourceText.From(stream, null).Encoding);
        }

        [Fact]
        public void ChecksumAlgorithm1()
        {
            Assert.Equal(SourceHashAlgorithm.Sha1, SourceText.From(HelloWorld).ChecksumAlgorithm);
            Assert.Equal(SourceHashAlgorithm.Sha1, SourceText.From(HelloWorld, checksumAlgorithm: SourceHashAlgorithm.Sha1).ChecksumAlgorithm);
            Assert.Equal(SourceHashAlgorithm.Sha256, SourceText.From(HelloWorld, checksumAlgorithm: SourceHashAlgorithm.Sha256).ChecksumAlgorithm);

            var bytes = s_unicode.GetBytes(HelloWorld);
            Assert.Equal(SourceHashAlgorithm.Sha1, SourceText.From(bytes, bytes.Length).ChecksumAlgorithm);
            Assert.Equal(SourceHashAlgorithm.Sha1, SourceText.From(bytes, bytes.Length, checksumAlgorithm: SourceHashAlgorithm.Sha1).ChecksumAlgorithm);
            Assert.Equal(SourceHashAlgorithm.Sha256, SourceText.From(bytes, bytes.Length, checksumAlgorithm: SourceHashAlgorithm.Sha256).ChecksumAlgorithm);

            var stream = new MemoryStream(bytes);
            Assert.Equal(SourceHashAlgorithm.Sha1, SourceText.From(stream).ChecksumAlgorithm);
            Assert.Equal(SourceHashAlgorithm.Sha1, SourceText.From(stream, checksumAlgorithm: SourceHashAlgorithm.Sha1).ChecksumAlgorithm);
            Assert.Equal(SourceHashAlgorithm.Sha256, SourceText.From(stream, checksumAlgorithm: SourceHashAlgorithm.Sha256).ChecksumAlgorithm);
        }

        [Fact]
        public void ContentEquals()
        {
            var f = SourceText.From(HelloWorld, s_utf8);

            Assert.True(f.ContentEquals(SourceText.From(HelloWorld, s_utf8)));
            Assert.False(f.ContentEquals(SourceText.From(HelloWorld + "o", s_utf8)));
            Assert.True(SourceText.From(HelloWorld, s_utf8).ContentEquals(SourceText.From(HelloWorld, s_utf8)));

            var e1 = EncodedStringText.Create(new MemoryStream(s_unicode.GetBytes(HelloWorld)), s_unicode);
            var e2 = EncodedStringText.Create(new MemoryStream(s_utf8.GetBytes(HelloWorld)), s_utf8);

            Assert.True(e1.ContentEquals(e1));
            Assert.True(f.ContentEquals(e1));
            Assert.True(e1.ContentEquals(f));

            Assert.True(e2.ContentEquals(e2));
            Assert.True(e1.ContentEquals(e2));
            Assert.True(e2.ContentEquals(e1));
        }

        [Fact]
        public void IsBinary()
        {
            Assert.False(SourceText.IsBinary(""));

            Assert.False(SourceText.IsBinary("\0abc"));
            Assert.False(SourceText.IsBinary("a\0bc"));
            Assert.False(SourceText.IsBinary("abc\0"));
            Assert.False(SourceText.IsBinary("a\0b\0c"));

            Assert.True(SourceText.IsBinary("\0\0abc"));
            Assert.True(SourceText.IsBinary("a\0\0bc"));
            Assert.True(SourceText.IsBinary("abc\0\0"));

            var encoding = Encoding.GetEncoding(1252);
            Assert.False(SourceText.IsBinary(encoding.GetString(new byte[] { 0x81, 0x8D, 0x8F, 0x90, 0x9D })));
            // Unicode string: äëïöüû
            Assert.False(SourceText.IsBinary("abc def baz aeiouy \u00E4\u00EB\u00EF\u00F6\u00FC\u00FB"));
            Assert.True(SourceText.IsBinary(encoding.GetString(TestResources.NetFX.v4_0_30319.System)));
        }

        [Fact]
        public void FromThrowsIfBinary()
        {
            var bytes = TestResources.NetFX.v4_0_30319.System;
            Assert.Throws<InvalidDataException>(() => SourceText.From(bytes, bytes.Length, throwIfBinaryDetected: true));

            var stream = new MemoryStream(bytes);
            Assert.Throws<InvalidDataException>(() => SourceText.From(stream, throwIfBinaryDetected: true));
        }

        private static void TestTryReadByteOrderMark(Encoding expectedEncoding, int expectedPreambleLength, byte[] data)
        {
            TestTryReadByteOrderMark(expectedEncoding, expectedPreambleLength, data, data == null ? 0 : data.Length);
        }

        private static void TestTryReadByteOrderMark(Encoding expectedEncoding, int expectedPreambleLength, byte[] data, int validLength)
        {
            int actualPreambleLength;
            Encoding actualEncoding = SourceText.TryReadByteOrderMark(data, validLength, out actualPreambleLength);
            if (expectedEncoding == null)
            {
                Assert.Null(actualEncoding);
            }
            else
            {
                Assert.Equal(expectedEncoding, actualEncoding);
            }

            Assert.Equal(expectedPreambleLength, actualPreambleLength);
        }

        [Fact]
        public void TryReadByteOrderMark()
        {
            TestTryReadByteOrderMark(expectedEncoding: null, expectedPreambleLength: 0, data: new byte[0]);
            TestTryReadByteOrderMark(expectedEncoding: null, expectedPreambleLength: 0, data: new byte[] { 0xef });
            TestTryReadByteOrderMark(expectedEncoding: null, expectedPreambleLength: 0, data: new byte[] { 0xef, 0xbb });
            TestTryReadByteOrderMark(expectedEncoding: null, expectedPreambleLength: 0, data: new byte[] { 0xef, 0xBB, 0xBF }, validLength: 2);
            TestTryReadByteOrderMark(expectedEncoding: Encoding.UTF8, expectedPreambleLength: 3, data: new byte[] { 0xef, 0xBB, 0xBF });

            TestTryReadByteOrderMark(expectedEncoding: null, expectedPreambleLength: 0, data: new byte[] { 0xff });
            TestTryReadByteOrderMark(expectedEncoding: Encoding.Unicode, expectedPreambleLength: 2, data: new byte[] { 0xff, 0xfe });

            TestTryReadByteOrderMark(expectedEncoding: null, expectedPreambleLength: 0, data: new byte[] { 0xfe });
            TestTryReadByteOrderMark(expectedEncoding: Encoding.BigEndianUnicode, expectedPreambleLength: 2, data: new byte[] { 0xfe, 0xff });
        }
    }
}
