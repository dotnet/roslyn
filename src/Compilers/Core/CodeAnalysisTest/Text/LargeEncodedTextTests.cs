using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class LargeEncodedTextTests : TestBase
    {
        private static SourceText CreateSourceText(string s, Encoding encoding = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(stream, encoding ?? Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
                {
                    sw.Write(s);
                }

                return CreateSourceText(stream, encoding);
            }
        }

        private static SourceText CreateSourceText(Stream stream, Encoding encoding = null)
        {
            return LargeEncodedText.Decode(stream, encoding ?? Encoding.UTF8, SourceHashAlgorithm.Sha1, throwIfBinaryDetected: true);
        }

        private const string HelloWorld = "Hello, world!";

        [Fact]
        public void BasicTest()
        {
            var text = CreateSourceText(HelloWorld);
            Assert.Equal(HelloWorld, text.ToString());
            Assert.Equal(HelloWorld.Length, text.Length);
        }

        [Fact]
        public void EmptyTest()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var text = CreateSourceText(stream);
                Assert.Equal(text.Length, 0);
            }
        }

        [Fact]
        public void IndexerTest()
        {
            var text = CreateSourceText(HelloWorld);
            Assert.Throws(typeof(IndexOutOfRangeException), () => text[-1]);
            Assert.Throws(typeof(IndexOutOfRangeException), () => text[HelloWorld.Length]);
            for(int i = HelloWorld.Length - 1; i >= 0; i--)
            {
                Assert.Equal(HelloWorld[i], text[i]);
            }
        }

        [Fact]
        public void CopyToTest()
        {
            var text = CreateSourceText(HelloWorld);

            const int destOffset = 10;
            char[] buffer = new char[HelloWorld.Length + destOffset];
            
            // Copy the entire text to a non-zero offset in the destination
            text.CopyTo(0, buffer, destOffset, text.Length);

            for (int i = 0; i < destOffset; i++)
            {
                Assert.Equal('\0', buffer[i]);
            }

            for (int i = destOffset; i < buffer.Length; i++)
            {
                Assert.Equal(HelloWorld[i - destOffset], buffer[i]);
            }

            Array.Clear(buffer, 0, buffer.Length);

            // Copy a sub-string
            text.CopyTo(3, buffer, 0, 3);
            Assert.Equal(HelloWorld[3], buffer[0]);
            Assert.Equal(HelloWorld[4], buffer[1]);
            Assert.Equal(HelloWorld[5], buffer[2]);
            for (int i = 3; i < buffer.Length; i++)
            {
                Assert.Equal('\0', buffer[i]);
            }
        }

        [Fact]
        public void CopyToLargeTest()
        {
            // Tests CopyTo at the chunk boundaries
            int targetLength = LargeEncodedText.ChunkSize * 2;

            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
                {
                    while (stream.Length < targetLength)
                    {
                        sw.Write(HelloWorld);
                    }
                }

                var text = CreateSourceText(stream);

                char[] buffer = new char[HelloWorld.Length];
                for (int start = 0; start < text.Length; start += HelloWorld.Length)
                {
                    text.CopyTo(start, buffer, 0, HelloWorld.Length);
                    Assert.Equal(HelloWorld, new string(buffer));
                }
            }
        }
    }
}