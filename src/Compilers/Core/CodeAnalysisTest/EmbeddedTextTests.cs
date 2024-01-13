// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;
using System.Text;
using System.IO.Compression;
using Roslyn.Test.Utilities;
using System.Linq;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class EmbeddedTextTests
    {
        [Fact]
        public void FromBytes_ArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>("filePath", () => EmbeddedText.FromBytes(null, default(ArraySegment<byte>)));
            Assert.Throws<ArgumentException>("filePath", () => EmbeddedText.FromBytes("", default(ArraySegment<byte>)));
            Assert.Throws<ArgumentNullException>("bytes", () => EmbeddedText.FromBytes("path", default(ArraySegment<byte>)));
            Assert.Throws<ArgumentException>("checksumAlgorithm", () => EmbeddedText.FromBytes("path", new ArraySegment<byte>(new byte[0], 0, 0), SourceHashAlgorithm.None));
        }

        [Fact]
        public void FromSource_ArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>("filePath", () => EmbeddedText.FromSource(null, null));
            Assert.Throws<ArgumentException>("filePath", () => EmbeddedText.FromSource("", null));
            Assert.Throws<ArgumentNullException>("text", () => EmbeddedText.FromSource("path", null));

            // no encoding
            Assert.Throws<ArgumentException>("text", () => EmbeddedText.FromSource("path", SourceText.From("source")));

            // embedding not allowed
            Assert.Throws<ArgumentException>("text", () => EmbeddedText.FromSource("path", SourceText.From(new byte[0], 0, Encoding.UTF8, canBeEmbedded: false)));
            Assert.Throws<ArgumentException>("text", () => EmbeddedText.FromSource("path", SourceText.From(new MemoryStream(new byte[0]), Encoding.UTF8, canBeEmbedded: false)));
        }

        [Fact]
        public void FromStream_ArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>("filePath", () => EmbeddedText.FromStream(null, null));
            Assert.Throws<ArgumentException>("filePath", () => EmbeddedText.FromStream("", null));
            Assert.Throws<ArgumentNullException>("stream", () => EmbeddedText.FromStream("path", null));
            Assert.Throws<ArgumentException>("stream", () => EmbeddedText.FromStream("path", new CannotReadStream()));
            Assert.Throws<ArgumentException>("stream", () => EmbeddedText.FromStream("path", new CannotSeekStream()));
            Assert.Throws<ArgumentException>("checksumAlgorithm", () => EmbeddedText.FromStream("path", new MemoryStream(), SourceHashAlgorithm.None));
        }

        [Fact]
        public void FromStream_IOErrors()
        {
            Assert.Throws<IOException>(() => EmbeddedText.FromStream("path", new HugeStream()));
            Assert.Throws<EndOfStreamException>(() => EmbeddedText.FromStream("path", new TruncatingStream(10)));
            Assert.Throws<EndOfStreamException>(() => EmbeddedText.FromStream("path", new TruncatingStream(1000)));
            Assert.Throws<IOException>(() => EmbeddedText.FromStream("path", new ReadFailsStream()));
        }

        private const string SmallSource = @"class P {}";
        private const string LargeSource = @"
//////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////
class Program 
{
    static void Main() {}
}
//////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////
";

        [Fact]
        public void FromBytes_Empty()
        {
            var text = EmbeddedText.FromBytes("pathToEmpty", new ArraySegment<byte>(new byte[0], 0, 0), SourceHashAlgorithm.Sha1);
            Assert.Equal("pathToEmpty", text.FilePath);
            Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm);
            AssertEx.Equal(SourceText.CalculateChecksum(new byte[0], 0, 0, SourceHashAlgorithm.Sha1), text.Checksum);
            AssertEx.Equal(new byte[] { 0, 0, 0, 0 }, text.Blob);
        }

        [Fact]
        public void FromStream_Empty()
        {
            var text = EmbeddedText.FromStream("pathToEmpty", new MemoryStream(new byte[0]), SourceHashAlgorithm.Sha1);
            var checksum = SourceText.CalculateChecksum(new byte[0], 0, 0, SourceHashAlgorithm.Sha1);

            Assert.Equal("pathToEmpty", text.FilePath);
            Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm);
            AssertEx.Equal(checksum, text.Checksum);
            AssertEx.Equal(new byte[] { 0, 0, 0, 0 }, text.Blob);
        }

        [Fact]
        public void FromSource_Empty()
        {
            var source = SourceText.From("", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), SourceHashAlgorithm.Sha1);
            var text = EmbeddedText.FromSource("pathToEmpty", source);
            var checksum = SourceText.CalculateChecksum(new byte[0], 0, 0, SourceHashAlgorithm.Sha1);

            Assert.Equal("pathToEmpty", text.FilePath);
            Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm);
            AssertEx.Equal(checksum, text.Checksum);
            AssertEx.Equal(new byte[] { 0, 0, 0, 0 }, text.Blob);
        }

        [Fact]
        public void FromBytes_Small()
        {
            var bytes = Encoding.UTF8.GetBytes(SmallSource);
            var checksum = SourceText.CalculateChecksum(bytes, 0, bytes.Length, SourceHashAlgorithm.Sha1);
            var text = EmbeddedText.FromBytes("pathToSmall", new ArraySegment<byte>(bytes, 0, bytes.Length));

            Assert.Equal("pathToSmall", text.FilePath);
            Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm);
            AssertEx.Equal(checksum, text.Checksum);
            AssertEx.Equal(new byte[] { 0, 0, 0, 0 }, text.Blob.Take(4));
            AssertEx.Equal(bytes, text.Blob.Skip(4));
        }

        [Fact]
        public void FromBytes_SmallSpan()
        {
            var bytes = Encoding.UTF8.GetBytes(SmallSource);
            var paddedBytes = new byte[] { 0 }.Concat(bytes).Concat(new byte[] { 0 }).ToArray();
            var checksum = SourceText.CalculateChecksum(bytes, 0, bytes.Length, SourceHashAlgorithm.Sha1);
            var text = EmbeddedText.FromBytes("pathToSmall", new ArraySegment<byte>(paddedBytes, 1, bytes.Length));

            Assert.Equal("pathToSmall", text.FilePath);
            AssertEx.Equal(checksum, text.Checksum);
            Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm);
            AssertEx.Equal(new byte[] { 0, 0, 0, 0 }, text.Blob.Take(4));
            AssertEx.Equal(bytes, text.Blob.Skip(4));
        }

        [Fact]
        public void FromSource_Small()
        {
            var source = SourceText.From(SmallSource, Encoding.UTF8, SourceHashAlgorithm.Sha1);
            var text = EmbeddedText.FromSource("pathToSmall", source);

            Assert.Equal("pathToSmall", text.FilePath);
            Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm);
            AssertEx.Equal(source.GetChecksum(), text.Checksum);
            AssertEx.Equal(new byte[] { 0, 0, 0, 0 }, text.Blob.Take(4));
            AssertEx.Equal(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(SmallSource)), text.Blob.Skip(4));
        }

        [Fact]
        public void FromBytes_Large()
        {
            var bytes = Encoding.Unicode.GetBytes(LargeSource);
            var checksum = SourceText.CalculateChecksum(bytes, 0, bytes.Length, SourceHashAlgorithms.Default);
            var text = EmbeddedText.FromBytes("pathToLarge", new ArraySegment<byte>(bytes, 0, bytes.Length), SourceHashAlgorithms.Default);

            Assert.Equal("pathToLarge", text.FilePath);
            Assert.Equal(SourceHashAlgorithms.Default, text.ChecksumAlgorithm);
            AssertEx.Equal(checksum, text.Checksum);
            AssertEx.Equal(BitConverter.GetBytes(bytes.Length), text.Blob.Take(4));
            AssertEx.Equal(bytes, Decompress(text.Blob.Skip(4)));
        }

        [Fact]
        public void FromBytes_LargeSpan()
        {
            var bytes = Encoding.Unicode.GetBytes(LargeSource);
            var paddedBytes = new byte[] { 0 }.Concat(bytes).Concat(new byte[] { 0 }).ToArray();
            var checksum = SourceText.CalculateChecksum(bytes, 0, bytes.Length, SourceHashAlgorithms.Default);
            var text = EmbeddedText.FromBytes("pathToLarge", new ArraySegment<byte>(paddedBytes, 1, bytes.Length), SourceHashAlgorithms.Default);

            Assert.Equal("pathToLarge", text.FilePath);
            AssertEx.Equal(checksum, text.Checksum);
            Assert.Equal(SourceHashAlgorithms.Default, text.ChecksumAlgorithm);
            AssertEx.Equal(BitConverter.GetBytes(bytes.Length), text.Blob.Take(4));
            AssertEx.Equal(bytes, Decompress(text.Blob.Skip(4)));
        }

        [Fact]
        public void FromSource_Large()
        {
            var source = SourceText.From(LargeSource, Encoding.Unicode, SourceHashAlgorithms.Default);
            var text = EmbeddedText.FromSource("pathToLarge", source);

            Assert.Equal("pathToLarge", text.FilePath);
            Assert.Equal(SourceHashAlgorithms.Default, text.ChecksumAlgorithm);
            AssertEx.Equal(source.GetChecksum(), text.Checksum);
            AssertEx.Equal(BitConverter.GetBytes(Encoding.Unicode.GetPreamble().Length + LargeSource.Length * sizeof(char)), text.Blob.Take(4));
            AssertEx.Equal(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(LargeSource)), Decompress(text.Blob.Skip(4)));
        }

        [Fact]
        public void FromTextReader_Small()
        {
            var expected = SourceText.From(SmallSource, Encoding.UTF8, SourceHashAlgorithm.Sha1);
            var expectedEmbedded = EmbeddedText.FromSource("pathToSmall", expected);

            var actual = SourceText.From(new StringReader(SmallSource), SmallSource.Length, Encoding.UTF8, SourceHashAlgorithm.Sha1);
            var actualEmbedded = EmbeddedText.FromSource(expectedEmbedded.FilePath, actual);

            Assert.Equal(expectedEmbedded.FilePath, actualEmbedded.FilePath);
            Assert.Equal(expectedEmbedded.ChecksumAlgorithm, actualEmbedded.ChecksumAlgorithm);
            AssertEx.Equal(expectedEmbedded.Checksum, actualEmbedded.Checksum);
            AssertEx.Equal(expectedEmbedded.Blob, actualEmbedded.Blob);
        }

        [Fact]
        public void FromTextReader_Large()
        {
            var expected = SourceText.From(LargeSource, Encoding.UTF8, SourceHashAlgorithm.Sha1);
            var expectedEmbedded = EmbeddedText.FromSource("pathToSmall", expected);

            var actual = SourceText.From(new StringReader(LargeSource), LargeSource.Length, Encoding.UTF8, SourceHashAlgorithm.Sha1);
            var actualEmbedded = EmbeddedText.FromSource(expectedEmbedded.FilePath, actual);

            Assert.Equal(expectedEmbedded.FilePath, actualEmbedded.FilePath);
            Assert.Equal(expectedEmbedded.ChecksumAlgorithm, actualEmbedded.ChecksumAlgorithm);
            AssertEx.Equal(expectedEmbedded.Checksum, actualEmbedded.Checksum);
            AssertEx.Equal(expectedEmbedded.Blob, actualEmbedded.Blob);
        }

        [Fact]
        public void FromSource_Precomputed()
        {
            byte[] bytes = Encoding.ASCII.GetBytes(LargeSource);
            bytes[0] = 0xFF; // invalid ASCII, should be reflected in checksum, blob.

            foreach (bool useStream in new[] { true, false })
            {
                var source = useStream ?
                    SourceText.From(new MemoryStream(bytes), Encoding.ASCII, SourceHashAlgorithm.Sha1, canBeEmbedded: true) :
                    SourceText.From(bytes, bytes.Length, Encoding.ASCII, SourceHashAlgorithm.Sha1, canBeEmbedded: true);

                var text = EmbeddedText.FromSource("pathToPrecomputed", source);
                Assert.Equal("pathToPrecomputed", text.FilePath);
                Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm);
                AssertEx.Equal(SourceText.CalculateChecksum(bytes, 0, bytes.Length, SourceHashAlgorithm.Sha1), source.GetChecksum());
                AssertEx.Equal(source.GetChecksum(), text.Checksum);
                AssertEx.Equal(BitConverter.GetBytes(bytes.Length), text.Blob.Take(4));
                AssertEx.Equal(bytes, Decompress(text.Blob.Skip(4)));
            }
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/12603")]
        public void FromBytes_EncodingFallbackCase()
        {
            var source = EncodedStringText.Create(new MemoryStream(new byte[] { 0xA9, 0x0D, 0x0A }), canBeEmbedded: true);
            var text = EmbeddedText.FromSource("pathToLarge", source);

            Assert.Equal("pathToLarge", text.FilePath);
            Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm);
            AssertEx.Equal(source.GetChecksum(), text.Checksum);
        }

        private byte[] Decompress(IEnumerable<byte> bytes)
        {
            var destination = new MemoryStream();
            using (var source = new DeflateStream(new MemoryStream(bytes.ToArray()), CompressionMode.Decompress))
            {
                source.CopyTo(destination);
            }

            return destination.ToArray();
        }

        private sealed class CannotReadStream : MemoryStream
        {
            public override bool CanRead => false;
        }

        private sealed class CannotSeekStream : MemoryStream
        {
            public override bool CanSeek => false;
        }

        private sealed class HugeStream : MemoryStream
        {
            public override long Length => (long)int.MaxValue + 1;
        }

        private sealed class TruncatingStream : MemoryStream
        {
            public TruncatingStream(long length)
            {
                Length = length;
            }

            public override long Length { get; }
            public override int Read(byte[] buffer, int offset, int count) => 0;
        }

        private sealed class ReadFailsStream : MemoryStream
        {
            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new IOException();
            }
        }
    }
}
