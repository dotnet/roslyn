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

        [WorkItem(7225, "https://github.com/dotnet/roslyn/issues/7225")]
        [Fact]
        public void ChecksumAndBOM()
        {
            const string source = "Hello, World!";
            var checksumAlgorigthm = SourceHashAlgorithm.Sha1;
            var encodingNoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            var encodingBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

            var checksumNoBOM = ImmutableArray.Create<byte>(0xa, 0xa, 0x9f, 0x2a, 0x67, 0x72, 0x94, 0x25, 0x57, 0xab, 0x53, 0x55, 0xd7, 0x6a, 0xf4, 0x42, 0xf8, 0xf6, 0x5e, 0x1);
            var checksumBOM = ImmutableArray.Create<byte>(0xb2, 0x19, 0x0, 0x9b, 0x61, 0xce, 0xcd, 0x50, 0x7b, 0x2e, 0x56, 0x3c, 0xc0, 0xeb, 0x96, 0xe2, 0xa1, 0xd9, 0x3f, 0xfc);

            // SourceText from string. Checksum should include BOM from explicit encoding.
            VerifyChecksum(SourceText.From(source, encodingNoBOM, checksumAlgorigthm), checksumNoBOM);
            VerifyChecksum(SourceText.From(source, encodingBOM, checksumAlgorigthm), checksumBOM);

            var bytesNoBOM = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x57, 0x6f, 0x72, 0x6c, 0x64, 0x21 };
            var bytesBOM = new byte[] { 0xef, 0xbb, 0xbf, 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x2c, 0x20, 0x57, 0x6f, 0x72, 0x6c, 0x64, 0x21 };

            var streamNoBOM = new MemoryStream(bytesNoBOM);
            var streamBOM = new MemoryStream(bytesBOM);

            // SourceText from bytes no BOM. Checksum should ignore explicit encoding.
            VerifyChecksum(SourceText.From(bytesNoBOM, bytesNoBOM.Length, null, checksumAlgorigthm), checksumNoBOM);
            VerifyChecksum(SourceText.From(bytesNoBOM, bytesNoBOM.Length, encodingNoBOM, checksumAlgorigthm), checksumNoBOM);
            VerifyChecksum(SourceText.From(bytesNoBOM, bytesNoBOM.Length, encodingBOM, checksumAlgorigthm), checksumNoBOM);

            // SourceText from bytes with BOM. Checksum should include BOM.
            VerifyChecksum(SourceText.From(bytesBOM, bytesBOM.Length, null, checksumAlgorigthm), checksumBOM);
            VerifyChecksum(SourceText.From(bytesBOM, bytesBOM.Length, encodingNoBOM, checksumAlgorigthm), checksumBOM);
            VerifyChecksum(SourceText.From(bytesBOM, bytesBOM.Length, encodingBOM, checksumAlgorigthm), checksumBOM);

            // SourceText from stream no BOM. Checksum should ignore explicit encoding.
            VerifyChecksum(SourceText.From(streamNoBOM, null, checksumAlgorigthm), checksumNoBOM);
            VerifyChecksum(SourceText.From(streamNoBOM, encodingNoBOM, checksumAlgorigthm), checksumNoBOM);
            VerifyChecksum(SourceText.From(streamNoBOM, encodingBOM, checksumAlgorigthm), checksumNoBOM);

            // SourceText from stream with BOM. Checksum should include BOM.
            VerifyChecksum(SourceText.From(streamBOM, null, checksumAlgorigthm), checksumBOM);
            VerifyChecksum(SourceText.From(streamBOM, encodingNoBOM, checksumAlgorigthm), checksumBOM);
            VerifyChecksum(SourceText.From(streamBOM, encodingBOM, checksumAlgorigthm), checksumBOM);

            // LargeText from stream no BOM. Checksum should ignore explicit encoding.
            VerifyChecksum(LargeText.Decode(streamNoBOM, encodingNoBOM, checksumAlgorigthm, throwIfBinaryDetected: false), checksumNoBOM);
            VerifyChecksum(LargeText.Decode(streamNoBOM, encodingBOM, checksumAlgorigthm, throwIfBinaryDetected: false), checksumNoBOM);

            // LargeText from stream with BOM. Checksum should include BOM.
            VerifyChecksum(LargeText.Decode(streamBOM, encodingNoBOM, checksumAlgorigthm, throwIfBinaryDetected: false), checksumBOM);
            VerifyChecksum(LargeText.Decode(streamBOM, encodingBOM, checksumAlgorigthm, throwIfBinaryDetected: false), checksumBOM);

            // LargeText from writer no BOM. Checksum includes BOM
            // from explicit encoding. This is inconsistent with the
            // LargeText cases above but LargeTextWriter is only used
            // for unsaved edits where the checksum is ignored.
            VerifyChecksum(FromLargeTextWriter(source, encodingNoBOM, checksumAlgorigthm), checksumNoBOM);
            VerifyChecksum(FromLargeTextWriter(source, encodingBOM, checksumAlgorigthm), checksumBOM);

            // SourceText from string with changes. Checksum includes BOM from explicit encoding.
            VerifyChecksum(FromChanges(SourceText.From(source, encodingNoBOM, checksumAlgorigthm)), checksumNoBOM);
            VerifyChecksum(FromChanges(SourceText.From(source, encodingBOM, checksumAlgorigthm)), checksumBOM);

            // SourceText from stream with changes, no BOM. Checksum includes BOM
            // from explicit encoding. This is inconsistent with the SourceText cases but
            // "with changes" is only used for unsaved edits where the checksum is ignored.
            VerifyChecksum(FromChanges(SourceText.From(streamNoBOM, encodingNoBOM, checksumAlgorigthm)), checksumNoBOM);
            VerifyChecksum(FromChanges(SourceText.From(streamNoBOM, encodingBOM, checksumAlgorigthm)), checksumBOM);

            // SourceText from stream with changes, with BOM. Checksum includes BOM.
            VerifyChecksum(FromChanges(SourceText.From(streamBOM, encodingNoBOM, checksumAlgorigthm)), checksumBOM);
            VerifyChecksum(FromChanges(SourceText.From(streamBOM, encodingBOM, checksumAlgorigthm)), checksumBOM);
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
