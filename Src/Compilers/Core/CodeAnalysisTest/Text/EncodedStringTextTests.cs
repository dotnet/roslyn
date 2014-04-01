// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using System.Threading.Tasks;
using ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class EncodedStringTextTests : TestBase
    {
        private byte[] GetBytes(Encoding encoding, string source)
        {
            var preamble = encoding.GetPreamble();
            var content = encoding.GetBytes(source);

            var bytes = new byte[preamble.Length + content.Length];
            preamble.CopyTo(bytes, 0);
            content.CopyTo(bytes, preamble.Length);

            return bytes;
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
            bytes = GetBytes(encoding, text);
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
            bytes = GetBytes(encoding, text);
            using (var stream = new MemoryStream(bytes))
            {
                Assert.Equal(text, EncodedStringText.DecodeIfNotBinary(stream, encoding));
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        public void Decode_NonUtf8()
        {
            var encoding1252 = Encoding.GetEncoding(1252);
            var utf8 = new UTF8Encoding(false, true);
            var text = "abc def baz aeiouy äëïöüû";
            var bytes = GetBytes(encoding1252, text);

            // 1252 should not decode to UTF-8
            using (var stream = new MemoryStream(bytes))
            {
                Assert.Throws(typeof(DecoderFallbackException), () => EncodedStringText.Decode(stream, utf8));
                Assert.True(stream.CanRead);
            }

            // Detect encoding should correctly pick 1252
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
            var bytes = GetBytes(utf8, text);

            // Detect encoding should correctly pick UTF-8
            using (var stream = new MemoryStream(bytes))
            {
                Assert.Equal(text, EncodedStringText.DetectEncodingAndDecode(stream));
                Assert.True(stream.CanRead);
            }
        }

        [WorkItem(611805)]
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
    }
}
