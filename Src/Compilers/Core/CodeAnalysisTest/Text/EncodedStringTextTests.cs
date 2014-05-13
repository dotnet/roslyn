// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class EncodedStringTextTests : TestBase
    {
        private static EncodedStringText CreateMemoryStreamBasedEncodedText(string text, Encoding writeEncoding, Encoding readEncodingOpt)
        {
            byte[] bytes = writeEncoding.GetBytesWithPreamble(text);

            // For testing purposes, create a bigger buffer so that we verify 
            // that the implementation only uses the part that's covered by the stream and not the entire array.
            byte[] buffer = new byte[bytes.Length + 10];
            bytes.CopyTo(buffer, 0);

            using (var stream = new MemoryStream(buffer, 0, bytes.Length, writable: true, publiclyVisible: true))
            {
                return new EncodedStringText(stream, readEncodingOpt);
            }
        }

        [Fact]
        public void CheckSum002()
        {
            var data = CreateMemoryStreamBasedEncodedText("The quick brown fox jumps over the lazy dog", Encoding.ASCII, readEncodingOpt: null);

            // this is known to be "2fd4e1c6 7a2d28fc ed849ee1 bb76e739 1b93eb12", see http://en.wikipedia.org/wiki/SHA-1
            var checksum = data.GetSha1Checksum();
            Assert.Equal("2fd4e1c6 7a2d28fc ed849ee1 bb76e739 1b93eb12", StringTextTest.ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum003()
        {
            var data = CreateMemoryStreamBasedEncodedText("The quick brown fox jumps over the lazy dog", Encoding.Unicode, readEncodingOpt: null);

            var checksum = data.GetSha1Checksum();
            Assert.Equal("9d0047c0 8c84a7ef a55a955e aa3b4aae f62c9c39", StringTextTest.ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum004()
        {
            var data = CreateMemoryStreamBasedEncodedText("The quick brown fox jumps over the lazy dog", Encoding.BigEndianUnicode, readEncodingOpt: null);

            var checksum = data.GetSha1Checksum();
            Assert.Equal("72b2beae c76188ac 5b38c16c 4f9d518a 2be0a34c", StringTextTest.ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum006()
        {
            var data = CreateMemoryStreamBasedEncodedText("", Encoding.ASCII, readEncodingOpt: null);

            // this is known to be "da39a3ee 5e6b4b0d 3255bfef 95601890 afd80709", see http://en.wikipedia.org/wiki/SHA-1
            var checksum = data.GetSha1Checksum();
            Assert.Equal("da39a3ee 5e6b4b0d 3255bfef 95601890 afd80709", StringTextTest.ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum007()
        {
            var data = CreateMemoryStreamBasedEncodedText("", Encoding.Unicode, readEncodingOpt: null);

            var checksum = data.GetSha1Checksum();
            Assert.Equal("d62636d8 caec13f0 4e28442a 0a6fa1af eb024bbb", StringTextTest.ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum008()
        {
            var data = CreateMemoryStreamBasedEncodedText("", Encoding.BigEndianUnicode, readEncodingOpt: null);

            var checksum = data.GetSha1Checksum();
            Assert.Equal("26237800 2c95ae7e 29535cb9 f438db21 9adf98f5", StringTextTest.ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void TryReadByteOrderMark()
        {
            Assert.Null(EncodedStringText.TryReadByteOrderMark(new MemoryStream(new byte[0])));

            Assert.Null(EncodedStringText.TryReadByteOrderMark(new MemoryStream(new byte[] { 0xef })));
            Assert.Null(EncodedStringText.TryReadByteOrderMark(new MemoryStream(new byte[] { 0xef, 0xbb })));
            Assert.Equal("Unicode (UTF-8)", EncodedStringText.TryReadByteOrderMark(new MemoryStream(new byte[] { 0xef, 0xBB, 0xBF })).EncodingName);

            Assert.Null(EncodedStringText.TryReadByteOrderMark(new MemoryStream(new byte[] { 0xff })));
            Assert.Equal("Unicode", EncodedStringText.TryReadByteOrderMark(new MemoryStream(new byte[] { 0xff, 0xfe })).EncodingName);

            Assert.Null(EncodedStringText.TryReadByteOrderMark(new MemoryStream(new byte[] { 0xfe })));
            Assert.Equal("Unicode (Big-Endian)", EncodedStringText.TryReadByteOrderMark(new MemoryStream(new byte[] { 0xfe, 0xff })).EncodingName);
        }

        [Fact]
        public void DecodeIfNotBinary()
        {
            var encoding = Encoding.GetEncoding(1252);
            var bytes = new byte[] { 0x81, 0x8D, 0x8F, 0x90, 0x9D };

            using (var stream = new MemoryStream(bytes))
            {
                Assert.Equal("\x81\x8D\x8F\x90\x9D", EncodedStringText.DecodeIfNotBinary(stream, encoding));
                Assert.True(stream.CanRead);
            }

            var text = "abc def baz aeiouy äëïöüû";
            bytes = encoding.GetBytesWithPreamble(text);
            using (var stream = new MemoryStream(bytes))
            {
                Assert.Equal(text, EncodedStringText.DecodeIfNotBinary(stream, encoding));
                Assert.True(stream.CanRead);
            }

            // Test binary detection with a real binary
            using (var stream = new MemoryStream(ProprietaryTestResources.NetFX.v4_0_30319.System))
            {
                Assert.Throws(typeof(InvalidDataException), () => EncodedStringText.DecodeIfNotBinary(stream, encoding));
                Assert.True(stream.CanRead);
            }

            // Large file decode
            text = new String('x', 1024 * 1024);
            bytes = encoding.GetBytesWithPreamble(text);
            using (var stream = new MemoryStream(bytes))
            {
                Assert.Equal(text, EncodedStringText.DecodeIfNotBinary(stream, encoding));
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void DecodeIfNotBinary_NulCharacters()
        {
            Action<string, Encoding, bool> verify = (text, encoding, shallThrow) =>
            {
                using (var stream = new MemoryStream(encoding.GetBytes(text)))
                {
                    if (shallThrow)
                    {
                        Assert.Throws(typeof(InvalidDataException), () => EncodedStringText.DecodeIfNotBinary(stream, encoding));
                    }
                    else
                    {
                        Assert.Equal(text, EncodedStringText.DecodeIfNotBinary(stream, encoding));
                        Assert.True(stream.CanRead);
                    }
                }
            };

            verify("", Encoding.BigEndianUnicode, false);
            verify("\0abc", Encoding.GetEncoding(437), false);
            verify("a\0bc", Encoding.UTF8, false);
            verify("abc\0", Encoding.GetEncoding(1252), false);
            verify("a\0b\0c", Encoding.UTF32, false);

            verify("\0\0abc", Encoding.Unicode, true);
            verify("a\0\0bc", Encoding.ASCII, true);
            verify("abc\0\0", Encoding.Default, true);
        }

        [Fact]
        public void Decode_NonUtf8()
        {
            var utf8 = new UTF8Encoding(false, true);
            var text = "abc def baz aeiouy " + Encoding.Default.GetString(new byte[] { 0x80, 0x92, 0xA4, 0xB6, 0xC9, 0xDB, 0xED, 0xFF });
            var bytes = Encoding.Default.GetBytesWithPreamble(text);

            // Encoding.Default should not decode to UTF-8
            using (var stream = new MemoryStream(bytes))
            {
                Assert.Throws(typeof(DecoderFallbackException), () => EncodedStringText.Decode(stream, utf8));
                Assert.True(stream.CanRead);
            }

            // Detect encoding should correctly pick Encoding.Default
            using (var stream = new MemoryStream(bytes))
            {
                Assert.Equal(text, EncodedStringText.DetectEncodingAndDecode(stream));
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void Decode_Utf8()
        {
            var utf8 = new UTF8Encoding(false, true);
            var text = "abc def baz aeiouy äëïöüû";
            var bytes = utf8.GetBytesWithPreamble(text);

            // Detect encoding should correctly pick UTF-8
            using (var stream = new MemoryStream(bytes))
            {
                Assert.Equal(text, EncodedStringText.DetectEncodingAndDecode(stream));
                Assert.True(stream.CanRead);
            }
        }

        [WorkItem(611805, "DevDiv")]
        [Fact]
        public void TestMultithreadedDecoding()
        {
            const string expectedText =
                "\r\n" +
                "class Program\r\n" +
                "{\r\n" +
                "    static void Main()\r\n" +
                "    {\r\n" +
                "        string s = \"class C { \u0410\u0411\u0412 x; }\";\r\n" +
                "        foreach (char ch in s) System.Console.WriteLine(\"{0:x2}\", (int)ch);\r\n" +
                "    }\r\n" +
                "}\r\n";

            var encoding = new UTF8Encoding(false);
            string path = Temp.CreateFile().WriteAllBytes(encoding.GetBytes(expectedText)).Path;

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 };
            Parallel.For(0, 500, parallelOptions, i =>
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    string actualText;

                    actualText = EncodedStringText.DetectEncodingAndDecode(stream);
                    Assert.Equal(expectedText, actualText);
                }
            });
        }

        [Fact]
        public void MemoryStreamBasedEncodedText1()
        {
            var encodings = new Encoding[]
            {
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            };

            foreach (var writeEncoding in encodings)
            {
                foreach (var readEncoding in encodings)
                {
                    var text = CreateMemoryStreamBasedEncodedText("foo", writeEncoding, readEncoding);
                    Assert.Equal(1, text.Lines.Count);
                    Assert.Equal(3, text.Lines[0].Span.Length);
                }
            }
        }

        [Fact]
        public void MemoryStreamBasedEncodedText2()
        {
            var writeEncodings = new Encoding[]
            {
                new UnicodeEncoding(bigEndian: true, byteOrderMark: true),
                new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            };

            var readEncodings = new Encoding[]
            {
                new UnicodeEncoding(bigEndian: true, byteOrderMark: true),
                new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
                new UnicodeEncoding(bigEndian: true, byteOrderMark: false),
                new UnicodeEncoding(bigEndian: false, byteOrderMark: false),
                null,
            };

            foreach (var writeEncoding in writeEncodings)
            {
                foreach (var readEncoding in readEncodings)
                {
                    var text = CreateMemoryStreamBasedEncodedText("foo", writeEncoding, readEncoding);
                    Assert.Equal(1, text.Lines.Count);
                    Assert.Equal(3, text.Lines[0].Span.Length);
                }
            }
        }
    }
}
