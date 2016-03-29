// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class StringTextDecodingTests : TestBase
    {
        private static SourceText CreateMemoryStreamBasedEncodedText(string text, Encoding writeEncoding, Encoding readEncodingOpt, SourceHashAlgorithm algorithm = SourceHashAlgorithm.Sha1)
        {
            byte[] bytes = writeEncoding.GetBytesWithPreamble(text);

            return CreateMemoryStreamBasedEncodedText(bytes, readEncodingOpt, algorithm);
        }

        private static SourceText CreateMemoryStreamBasedEncodedText(byte[] bytes, Encoding readEncodingOpt, SourceHashAlgorithm algorithm = SourceHashAlgorithm.Sha1)
        {
            // For testing purposes, create a bigger buffer so that we verify 
            // that the implementation only uses the part that's covered by the stream and not the entire array.
            byte[] buffer = new byte[bytes.Length + 10];
            bytes.CopyTo(buffer, 0);

            using (var stream = new MemoryStream(buffer, 0, bytes.Length, writable: true, publiclyVisible: true))
            {
                return EncodedStringText.Create(stream, readEncodingOpt, algorithm);
            }
        }

        private static SourceText CreateMemoryStreamBasedEncodedText(byte[] bytes,
            Func<Encoding> getEncoding,
            Encoding readEncodingOpt = null,
            SourceHashAlgorithm algorithm = SourceHashAlgorithm.Sha1)
        {
            // For testing purposes, create a bigger buffer so that we verify 
            // that the implementation only uses the part that's covered by the stream and not the entire array.
            byte[] buffer = new byte[bytes.Length + 10];
            bytes.CopyTo(buffer, 0);

            using (var stream = new MemoryStream(buffer, 0, bytes.Length, writable: true, publiclyVisible: true))
            {
                return EncodedStringText.Create(stream, getEncoding, readEncodingOpt, algorithm);
            }
        }

        [Fact]
        public void ShiftJisGetEncoding()
        {
            var sjis = Encoding.GetEncoding(932);
            var data = CreateMemoryStreamBasedEncodedText(TestResources.General.ShiftJisSource, () => sjis);

            Assert.Equal(932, data.Encoding?.WindowsCodePage);
            Assert.Equal(sjis.GetString(TestResources.General.ShiftJisSource), data.ToString());
        }

        [Fact]
        public void ShiftJisFile()
        {
            var sjis = Encoding.GetEncoding(932);
            var data = CreateMemoryStreamBasedEncodedText(TestResources.General.ShiftJisSource, sjis);

            Assert.Equal(932, data.Encoding?.WindowsCodePage);
            Assert.Equal(sjis.GetString(TestResources.General.ShiftJisSource), data.ToString());
        }

        [Fact]
        public void CheckSum002()
        {
            var data = CreateMemoryStreamBasedEncodedText("The quick brown fox jumps over the lazy dog", Encoding.ASCII, readEncodingOpt: null);

            // this is known to be "2fd4e1c6 7a2d28fc ed849ee1 bb76e739 1b93eb12", see http://en.wikipedia.org/wiki/SHA-1
            var checksum = data.GetChecksum();
            Assert.Equal("2fd4e1c6 7a2d28fc ed849ee1 bb76e739 1b93eb12", StringTextTest.ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum003()
        {
            var data = CreateMemoryStreamBasedEncodedText("The quick brown fox jumps over the lazy dog", Encoding.Unicode, readEncodingOpt: null);

            var checksum = data.GetChecksum();
            Assert.Equal("9d0047c0 8c84a7ef a55a955e aa3b4aae f62c9c39", StringTextTest.ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum004()
        {
            var data = CreateMemoryStreamBasedEncodedText("The quick brown fox jumps over the lazy dog", Encoding.BigEndianUnicode, readEncodingOpt: null);

            var checksum = data.GetChecksum();
            Assert.Equal("72b2beae c76188ac 5b38c16c 4f9d518a 2be0a34c", StringTextTest.ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum006()
        {
            var data = CreateMemoryStreamBasedEncodedText("", Encoding.ASCII, readEncodingOpt: null);

            // this is known to be "da39a3ee 5e6b4b0d 3255bfef 95601890 afd80709", see http://en.wikipedia.org/wiki/SHA-1
            var checksum = data.GetChecksum();
            Assert.Equal("da39a3ee 5e6b4b0d 3255bfef 95601890 afd80709", StringTextTest.ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum007()
        {
            var data = CreateMemoryStreamBasedEncodedText("", Encoding.Unicode, readEncodingOpt: null);

            var checksum = data.GetChecksum();
            Assert.Equal("d62636d8 caec13f0 4e28442a 0a6fa1af eb024bbb", StringTextTest.ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum008()
        {
            var data = CreateMemoryStreamBasedEncodedText("", Encoding.BigEndianUnicode, readEncodingOpt: null);

            var checksum = data.GetChecksum();
            Assert.Equal("26237800 2c95ae7e 29535cb9 f438db21 9adf98f5", StringTextTest.ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum_SHA256()
        {
            var data = CreateMemoryStreamBasedEncodedText("", Encoding.UTF8, readEncodingOpt: null, algorithm: SourceHashAlgorithm.Sha256);

            var checksum = data.GetChecksum();
            Assert.Equal("f1945cd6 c19e56b3 c1c78943 ef5ec181 16907a4c a1efc40a 57d48ab1 db7adfc5", StringTextTest.ChecksumToHexQuads(checksum));
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem(5663, "https://github.com/dotnet/roslyn/issues/5663")]
        public void Decode_NonUtf8()
        {
            // Unicode text with extended characters that map to interesting code points in CodePage 1252.
            var text = "abc def baz aeiouy \u20ac\u2019\u00a4\u00b6\u00c9\u00db\u00ed\u00ff";

            // The same text encoded in CodePage 1252 which happens to be an illegal sequence if decoded as Utf-8.
            var bytes = new byte[]
            {
                0x61, 0x62, 0x63, 0x20, 0x64, 0x65, 0x66, 0x20, 0x62, 0x61, 0x7a, 0x20, 0x61, 0x65, 0x69, 0x6f, 0x75, 0x79, 0x20,
                0x80, 0x92, 0xA4, 0xB6, 0xC9, 0xDB, 0xED, 0xFF
            };

            var utf8 = new UTF8Encoding(false, true);

            // bytes should not decode to UTF-8
            using (var stream = new MemoryStream(bytes))
            {
                Assert.Throws(typeof(DecoderFallbackException), () =>
                {
                    EncodedStringText.Decode(stream, utf8, SourceHashAlgorithm.Sha1);
                });

                Assert.True(stream.CanRead);
            }

            // Detect encoding should correctly pick CodePage 1252
            using (var stream = new MemoryStream(bytes))
            {
                var sourceText = EncodedStringText.Create(stream);
                Assert.Equal(text, sourceText.ToString());

                // Check for a complete Encoding implementation.
                Assert.Equal(1252, sourceText.Encoding.CodePage);
                Assert.NotNull(sourceText.Encoding.GetEncoder());
                Assert.NotNull(sourceText.Encoding.GetDecoder());
                Assert.Equal(2, sourceText.Encoding.GetMaxByteCount(1));
                Assert.Equal(1, sourceText.Encoding.GetMaxCharCount(1));
                Assert.Equal(text, sourceText.Encoding.GetString(bytes));

                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void Decode_Utf8()
        {
            var utf8 = new UTF8Encoding(false, true);
            var text = "abc def baz aeiouy \u00E4\u00EB\u00EF\u00F6\u00FC\u00FB";
            var bytes = utf8.GetBytesWithPreamble(text);

            // Detect encoding should correctly pick UTF-8
            using (var stream = new MemoryStream(bytes))
            {
                var sourceText = EncodedStringText.Create(stream);
                Assert.Equal(text, sourceText.ToString());
                Assert.Equal(Encoding.UTF8.EncodingName, sourceText.Encoding.EncodingName);
                Assert.True(stream.CanRead);
            }
        }

        [WorkItem(611805, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611805")]
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
                    var sourceText = EncodedStringText.Create(stream);
                    Assert.Equal(expectedText, sourceText.ToString());
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

        [Fact]
        public void FileStreamEncodedText()
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


            var encodings = new Encoding[]
            {
                new UnicodeEncoding(bigEndian: true, byteOrderMark: true),
                new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            };

            foreach (var encoding in encodings)
            {
                var tmpFile = Temp.CreateFile();

                File.WriteAllText(tmpFile.Path, expectedText, encoding);

                using (FileStream fs = new FileStream(tmpFile.Path, FileMode.Open, FileAccess.Read))
                {
                    var encodedText = EncodedStringText.Create(fs);
                    Assert.Equal(encoding.CodePage, encodedText.Encoding.CodePage);
                    Assert.Equal(expectedText, encodedText.ToString());
                }
            }
        }

        [Fact]
        public void FileStreamEncodedTextEmpty()
        {
            var tmpFile = Temp.CreateFile();

            using (FileStream fs = new FileStream(tmpFile.Path, FileMode.Open, FileAccess.Read))
            {
                var encodedText = EncodedStringText.Create(fs);
                Assert.Equal(0, encodedText.Length);
            }
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem(2081, "https://github.com/dotnet/roslyn/issues/2081")]
        [WorkItem(5663, "https://github.com/dotnet/roslyn/issues/5663")]
        public void HorizontalEllipsis()
        {
            // Character 0x85 in CodePage 1252 is a horizontal ellipsis.
            // If decoded as Latin-1, then it's incorrectly treated as \u0085 which
            // is a line break ('NEXT LINE').
            byte[] srcBytes = new[] { (byte)0x85 };
            using (var ms = new MemoryStream(srcBytes))
            {
                var sourceText = EncodedStringText.Create(ms);
                Assert.Equal('\u2026', sourceText[0]);
            }
        }
    }
}
