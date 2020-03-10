﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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

        [WorkItem(7225, "https://github.com/dotnet/roslyn/issues/7225")]
        [Fact]
        public void ChecksumAndBOM()
        {
            const string source = "Hello, World!";
            var checksumAlgorithm = SourceHashAlgorithm.Sha1;
            var encodingNoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            var encodingBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

            var checksumNoBOM = ImmutableArray.Create<byte>(0xa, 0xa, 0x9f, 0x2a, 0x67, 0x72, 0x94, 0x25, 0x57, 0xab, 0x53, 0x55, 0xd7, 0x6a, 0xf4, 0x42, 0xf8, 0xf6, 0x5e, 0x1);
            var checksumBOM = ImmutableArray.Create<byte>(0xb2, 0x19, 0x0, 0x9b, 0x61, 0xce, 0xcd, 0x50, 0x7b, 0x2e, 0x56, 0x3c, 0xc0, 0xeb, 0x96, 0xe2, 0xa1, 0xd9, 0x3f, 0xfc);

            // SourceText from string. Checksum should include BOM from explicit encoding.
            VerifyChecksum(SourceText.From(source, encodingNoBOM, checksumAlgorithm), checksumNoBOM);
            VerifyChecksum(SourceText.From(source, encodingBOM, checksumAlgorithm), checksumBOM);

            var bytesNoBOM = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x57, 0x6f, 0x72, 0x6c, 0x64, 0x21 };
            var bytesBOM = new byte[] { 0xef, 0xbb, 0xbf, 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x57, 0x6f, 0x72, 0x6c, 0x64, 0x21 };

            var streamNoBOM = new MemoryStream(bytesNoBOM);
            var streamBOM = new MemoryStream(bytesBOM);

            // SourceText from bytes no BOM. Checksum should ignore explicit encoding.
            VerifyChecksum(SourceText.From(bytesNoBOM, bytesNoBOM.Length, null, checksumAlgorithm), checksumNoBOM);
            VerifyChecksum(SourceText.From(bytesNoBOM, bytesNoBOM.Length, encodingNoBOM, checksumAlgorithm), checksumNoBOM);
            VerifyChecksum(SourceText.From(bytesNoBOM, bytesNoBOM.Length, encodingBOM, checksumAlgorithm), checksumNoBOM);

            // SourceText from bytes with BOM. Checksum should include BOM.
            VerifyChecksum(SourceText.From(bytesBOM, bytesBOM.Length, null, checksumAlgorithm), checksumBOM);
            VerifyChecksum(SourceText.From(bytesBOM, bytesBOM.Length, encodingNoBOM, checksumAlgorithm), checksumBOM);
            VerifyChecksum(SourceText.From(bytesBOM, bytesBOM.Length, encodingBOM, checksumAlgorithm), checksumBOM);

            // SourceText from stream no BOM. Checksum should ignore explicit encoding.
            VerifyChecksum(SourceText.From(streamNoBOM, null, checksumAlgorithm), checksumNoBOM);
            VerifyChecksum(SourceText.From(streamNoBOM, encodingNoBOM, checksumAlgorithm), checksumNoBOM);
            VerifyChecksum(SourceText.From(streamNoBOM, encodingBOM, checksumAlgorithm), checksumNoBOM);

            // SourceText from stream with BOM. Checksum should include BOM.
            VerifyChecksum(SourceText.From(streamBOM, null, checksumAlgorithm), checksumBOM);
            VerifyChecksum(SourceText.From(streamBOM, encodingNoBOM, checksumAlgorithm), checksumBOM);
            VerifyChecksum(SourceText.From(streamBOM, encodingBOM, checksumAlgorithm), checksumBOM);

            // LargeText from stream no BOM. Checksum should ignore explicit encoding.
            VerifyChecksum(LargeText.Decode(streamNoBOM, encodingNoBOM, checksumAlgorithm, throwIfBinaryDetected: false, canBeEmbedded: false), checksumNoBOM);
            VerifyChecksum(LargeText.Decode(streamNoBOM, encodingBOM, checksumAlgorithm, throwIfBinaryDetected: false, canBeEmbedded: false), checksumNoBOM);

            // LargeText from stream with BOM. Checksum should include BOM.
            VerifyChecksum(LargeText.Decode(streamBOM, encodingNoBOM, checksumAlgorithm, throwIfBinaryDetected: false, canBeEmbedded: false), checksumBOM);
            VerifyChecksum(LargeText.Decode(streamBOM, encodingBOM, checksumAlgorithm, throwIfBinaryDetected: false, canBeEmbedded: false), checksumBOM);

            // LargeText from writer no BOM. Checksum includes BOM
            // from explicit encoding. This is inconsistent with the
            // LargeText cases above but LargeTextWriter is only used
            // for unsaved edits where the checksum is ignored.
            VerifyChecksum(FromLargeTextWriter(source, encodingNoBOM, checksumAlgorithm), checksumNoBOM);
            VerifyChecksum(FromLargeTextWriter(source, encodingBOM, checksumAlgorithm), checksumBOM);

            // SourceText from string with changes. Checksum includes BOM from explicit encoding.
            VerifyChecksum(FromChanges(SourceText.From(source, encodingNoBOM, checksumAlgorithm)), checksumNoBOM);
            VerifyChecksum(FromChanges(SourceText.From(source, encodingBOM, checksumAlgorithm)), checksumBOM);

            // SourceText from stream with changes, no BOM. Checksum includes BOM
            // from explicit encoding. This is inconsistent with the SourceText cases but
            // "with changes" is only used for unsaved edits where the checksum is ignored.
            VerifyChecksum(FromChanges(SourceText.From(streamNoBOM, encodingNoBOM, checksumAlgorithm)), checksumNoBOM);
            VerifyChecksum(FromChanges(SourceText.From(streamNoBOM, encodingBOM, checksumAlgorithm)), checksumBOM);

            // SourceText from stream with changes, with BOM. Checksum includes BOM.
            VerifyChecksum(FromChanges(SourceText.From(streamBOM, encodingNoBOM, checksumAlgorithm)), checksumBOM);
            VerifyChecksum(FromChanges(SourceText.From(streamBOM, encodingBOM, checksumAlgorithm)), checksumBOM);
        }

        private static SourceText FromLargeTextWriter(string source, Encoding encoding, SourceHashAlgorithm checksumAlgorithm)
        {
            using (var writer = new LargeTextWriter(encoding, checksumAlgorithm, source.Length))
            {
                writer.Write(source);
                return writer.ToSourceText();
            }
        }

        private static SourceText FromChanges(SourceText text)
        {
            var span = new TextSpan(0, 1);
            var change = new TextChange(span, text.ToString(span));
            var changed = text.WithChanges(change);
            Assert.NotEqual(text, changed);
            return changed;
        }

        private static void VerifyChecksum(SourceText text, ImmutableArray<byte> expectedChecksum)
        {
            var actualChecksum = text.GetChecksum();
            Assert.Equal<byte>(expectedChecksum, actualChecksum);
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

            var encoding = Encoding.UTF8;
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

        [Fact]
        public void FromTextReader()
        {
            var expected = "Text reader source text test";
            var expectedSourceText = SourceText.From(expected);

            var actual = new StringReader(expected);
            var actualSourceText = SourceText.From(actual, expected.Length);

            Assert.Equal<byte>(expectedSourceText.GetChecksum(), actualSourceText.GetChecksum());

            Assert.Same(s_utf8, SourceText.From(actual, expected.Length, s_utf8).Encoding);
            Assert.Same(s_unicode, SourceText.From(actual, expected.Length, s_unicode).Encoding);
            Assert.Null(SourceText.From(actual, expected.Length, null).Encoding);
        }

        [Fact]
        public void FromTextReader_Large()
        {
            var expected = new string('l', SourceText.LargeObjectHeapLimitInChars);
            var expectedSourceText = SourceText.From(expected);

            var actual = new StringReader(expected);
            var actualSourceText = SourceText.From(actual, expected.Length);

            Assert.IsType<LargeText>(actualSourceText);
            Assert.Equal<byte>(expectedSourceText.GetChecksum(), actualSourceText.GetChecksum());

            var utf8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            Assert.Same(s_utf8, SourceText.From(actual, expected.Length, s_utf8).Encoding);
            Assert.Same(s_unicode, SourceText.From(actual, expected.Length, s_unicode).Encoding);
            Assert.Null(SourceText.From(actual, expected.Length, null).Encoding);
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

        [Fact]
        [WorkItem(41903, "https://github.com/dotnet/roslyn/issues/41903")]
        public void WriteWithRangeStartingLaterThanZero()
        {
            var sourceText = SourceText.From("ABCDEFGHIJKLMNOPQRSTUVWXYZ");

            var writer = new StringWriter();
            sourceText.Write(writer, TextSpan.FromBounds(1, sourceText.Length));

            Assert.Equal("BCDEFGHIJKLMNOPQRSTUVWXYZ", writer.ToString());
        }

        public static IEnumerable<object[]> AllRanges(int totalLength) =>
            from start in Enumerable.Range(0, totalLength)
            from length in Enumerable.Range(0, totalLength - start)
            select new object[] { new TextSpan(start, length) };

        [Theory]
        [MemberData(nameof(AllRanges), 10)]
        [WorkItem(41903, "https://github.com/dotnet/roslyn/issues/41903")]
        public void WriteWithAllRanges(TextSpan span)
        {
            const string Text = "0123456789";
            var sourceText = SourceText.From(Text);

            var writer = new StringWriter();
            sourceText.Write(writer, span);

            Assert.Equal(Text.Substring(span.Start, span.Length), writer.ToString());
        }

        [Fact]
        public void WriteWithSpanStartingAfterEndThrowsOutOfRange()
        {
            var ex = Assert.ThrowsAny<ArgumentOutOfRangeException>(() =>
                SourceText.From("ABC").Write(TextWriter.Null, TextSpan.FromBounds(4, 4)));

            Assert.Equal("span", ex.ParamName);
        }

        [Fact]
        public void WriteWithSpanEndingAfterEndThrowsOutOfRange()
        {
            var ex = Assert.ThrowsAny<ArgumentOutOfRangeException>(() =>
                SourceText.From("ABC").Write(TextWriter.Null, TextSpan.FromBounds(2, 4)));

            Assert.Equal("span", ex.ParamName);
        }
    }
}
