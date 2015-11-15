// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.PEWriter
{
    public class BlobTests
    {
        [Fact]
        public void Ctor()
        {
            var builder = new BlobBuilder();
            Assert.Equal(BlobBuilder.DefaultChunkSize, builder.BufferSize);

            builder = new BlobBuilder(0);
            Assert.Equal(BlobBuilder.MinChunkSize, builder.BufferSize);

            builder = new BlobBuilder(10001);
            Assert.Equal(10001, builder.BufferSize);
        }

        [Fact]
        public void Ctor_Errors()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BlobBuilder(-1));
        }

        [Fact]
        public void CountClear()
        {
            var builder = new BlobBuilder();
            Assert.Equal(0, builder.Count);

            builder.WriteByte(1);
            Assert.Equal(1, builder.Count);

            builder.WriteInt32(4);
            Assert.Equal(5, builder.Count);

            builder.Clear();
            Assert.Equal(0, builder.Count);

            builder.WriteInt64(1);
            Assert.Equal(8, builder.Count);

            AssertEx.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, builder.ToArray());
        }

        private void TestContentEquals(byte[] left, byte[] right)
        {
            var builder1 = new BlobBuilder(0);
            builder1.WriteBytes(left);

            var builder2 = new BlobBuilder(0);
            builder2.WriteBytes(right);

            bool expected = ByteSequenceComparer.Equals(left, right);
            Assert.Equal(expected, builder1.ContentEquals(builder2));
        }

        [Fact]
        public void ContentEquals()
        {
            var builder = new BlobBuilder();
            Assert.True(builder.ContentEquals(builder));
            Assert.False(builder.ContentEquals(null));

            TestContentEquals(new byte[] { }, new byte[] { });
            TestContentEquals(new byte[] { 1 }, new byte[] { });
            TestContentEquals(new byte[] { }, new byte[] { 1 });
            TestContentEquals(new byte[] { 1 }, new byte[] { 1 });

            TestContentEquals(
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, 
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });

            TestContentEquals(
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 },
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });

            TestContentEquals(
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });

            TestContentEquals(
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });

            TestContentEquals(
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });

            TestContentEquals(
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 },
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });

            TestContentEquals(
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 },
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 });

            TestContentEquals(
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 },
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 });

            TestContentEquals(
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 99, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 },
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 });

            TestContentEquals(
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 },
                new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 99, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 });
        }

        [Fact]
        public void GetBlobs()
        {
            var builder = new BlobBuilder(16);
            builder.WriteBytes(1, 100);

            var blobs = builder.GetBlobs().ToArray();
            Assert.Equal(2, blobs.Length);
            Assert.Equal(16, blobs[0].Length);
            Assert.Equal(100 - 16, blobs[1].Length);

            builder.WriteByte(1);

            blobs = builder.GetBlobs().ToArray();
            Assert.Equal(3, blobs.Length);
            Assert.Equal(16, blobs[0].Length);
            Assert.Equal(16, blobs[0].GetUnderlyingBuffer().Array.Length);
            Assert.Equal(100 - 16, blobs[1].Length);
            Assert.Equal(100 - 16, blobs[1].GetUnderlyingBuffer().Array.Length);
            Assert.Equal(1, blobs[2].Length);
            Assert.Equal(100 - 16, blobs[2].GetUnderlyingBuffer().Array.Length);

            builder.Clear();

            blobs = builder.GetBlobs().ToArray();
            Assert.Equal(1, blobs.Length);
            Assert.Equal(0, blobs[0].Length);

            // Clear uses the first buffer:
            Assert.Equal(16, blobs[0].GetUnderlyingBuffer().Array.Length);
        }

        [Fact]
        public void GetChunks_DestructingEnum()
        {
            for (int j = 1; j < 5; j++)
            {
                var builder = new BlobBuilder(16);

                for (int i = 0; i < j; i++)
                {
                    builder.WriteBytes((byte)i, 16);
                }

                int n = 0;
                foreach (var chunk in builder.GetChunks())
                {
                    n++;
                }

                Assert.Equal(j, n);

                var chunks = new HashSet<BlobBuilder>();
                foreach (var chunk in builder.GetChunks())
                {
                    chunks.Add(chunk);
                    chunk.ClearChunk();
                }

                Assert.Equal(j, chunks.Count);
            }
        }

        [Fact]
        public void ToArray()
        {
            var builder = new BlobBuilder(16);

            AssertEx.Equal(new byte[] { }, builder.ToArray(0, 0));

            for (int i = 0; i < 13; i++)
            {
                builder.WriteByte((byte)i);
            }

            builder.WriteUInt32(0xaabbccdd);

            AssertEx.Equal(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0xDD, 0xCC, 0xBB, 0xAA }, builder.ToArray());
            AssertEx.Equal(new byte[] { }, builder.ToArray(0, 0));
            AssertEx.Equal(new byte[] { 0 }, builder.ToArray(0, 1));
            AssertEx.Equal(new byte[] { 1 }, builder.ToArray(1, 1));

            AssertEx.Equal(new byte[] { }, builder.ToArray(14, 0));
            AssertEx.Equal(new byte[] { }, builder.ToArray(15, 0));
            AssertEx.Equal(new byte[] { }, builder.ToArray(16, 0));
            AssertEx.Equal(new byte[] { }, builder.ToArray(17, 0));

            AssertEx.Equal(new byte[] { 0xdd }, builder.ToArray(13, 1));
            AssertEx.Equal(new byte[] { 0xcc }, builder.ToArray(14, 1));
            AssertEx.Equal(new byte[] { 0xbb }, builder.ToArray(15, 1));
            AssertEx.Equal(new byte[] { 0xaa }, builder.ToArray(16, 1));

            AssertEx.Equal(new byte[] { 0xdd, 0xcc }, builder.ToArray(13, 2));
            AssertEx.Equal(new byte[] { 0xcc, 0xbb }, builder.ToArray(14, 2));
            AssertEx.Equal(new byte[] { 0xbb, 0xaa }, builder.ToArray(15, 2));

            AssertEx.Equal(new byte[] { 0xdd, 0xcc, 0xbb }, builder.ToArray(13, 3));
            AssertEx.Equal(new byte[] { 0xcc, 0xbb, 0xaa }, builder.ToArray(14, 3));

            AssertEx.Equal(new byte[] { 0xdd, 0xcc, 0xbb, 0xaa }, builder.ToArray(13, 4));
        }

        [Fact]
        public void ToArray_Errors()
        {
            var builder = new BlobBuilder(16);
            builder.WriteByte(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.ToArray(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.ToArray(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.ToArray(0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.ToArray(1, 1));
        }

        [Fact]
        public void ToImmutableArray()
        {
            var builder = new BlobBuilder(16);

            AssertEx.Equal(new byte[] { }, builder.ToArray(0, 0));

            for (int i = 0; i < 13; i++)
            {
                builder.WriteByte((byte)i);
            }

            builder.WriteUInt32(0xaabbccdd);

            AssertEx.Equal(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0xDD, 0xCC, 0xBB, 0xAA }, builder.ToImmutableArray());
            AssertEx.Equal(new byte[] { }, builder.ToImmutableArray(0, 0));
            AssertEx.Equal(new byte[] { 0 }, builder.ToImmutableArray(0, 1));
            AssertEx.Equal(new byte[] { 1 }, builder.ToImmutableArray(1, 1));

            AssertEx.Equal(new byte[] { }, builder.ToImmutableArray(14, 0));
            AssertEx.Equal(new byte[] { }, builder.ToImmutableArray(15, 0));
            AssertEx.Equal(new byte[] { }, builder.ToImmutableArray(16, 0));
            AssertEx.Equal(new byte[] { }, builder.ToImmutableArray(17, 0));

            AssertEx.Equal(new byte[] { 0xdd }, builder.ToImmutableArray(13, 1));
            AssertEx.Equal(new byte[] { 0xcc }, builder.ToImmutableArray(14, 1));
            AssertEx.Equal(new byte[] { 0xbb }, builder.ToImmutableArray(15, 1));
            AssertEx.Equal(new byte[] { 0xaa }, builder.ToImmutableArray(16, 1));

            AssertEx.Equal(new byte[] { 0xdd, 0xcc }, builder.ToImmutableArray(13, 2));
            AssertEx.Equal(new byte[] { 0xcc, 0xbb }, builder.ToImmutableArray(14, 2));
            AssertEx.Equal(new byte[] { 0xbb, 0xaa }, builder.ToImmutableArray(15, 2));

            AssertEx.Equal(new byte[] { 0xdd, 0xcc, 0xbb }, builder.ToImmutableArray(13, 3));
            AssertEx.Equal(new byte[] { 0xcc, 0xbb, 0xaa }, builder.ToImmutableArray(14, 3));

            AssertEx.Equal(new byte[] { 0xdd, 0xcc, 0xbb, 0xaa }, builder.ToImmutableArray(13, 4));
        }

        [Fact]
        public void ToImmutableArray_Errors()
        {
            var builder = new BlobBuilder(16);
            builder.WriteByte(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.ToImmutableArray(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.ToImmutableArray(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.ToImmutableArray(0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.ToImmutableArray(1, 1));
        }

        [Fact]
        public void WriteContentToStream()
        {
            var builder = new BlobBuilder(16);
            for (int i = 0; i < 20; i++)
            {
                builder.WriteByte((byte)i);
            }

            var stream = new MemoryStream();
            builder.WriteContentTo(stream);
            AssertEx.Equal(new byte[] 
            {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13
            }, stream.ToArray());

            builder.WriteByte(0xff);

            builder.WriteContentTo(stream);
            AssertEx.Equal(new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13,
                0xff,
            }, stream.ToArray());
        }

        [Fact]
        public void WriteContentToStream_Errors()
        {
            var builder = new BlobBuilder(16);
            builder.WriteByte(1);

            Assert.Throws<ArgumentNullException>(() => builder.WriteContentTo((Stream)null));
            Assert.Throws<NotSupportedException>(() => builder.WriteContentTo(new MemoryStream(new byte[] { 1 }, writable: false)));
        }

        [Fact]
        public void WriteContentToBlobWriter()
        {
            var builder = new BlobBuilder(16);
            for (int i = 0; i < 20; i++)
            {
                builder.WriteByte((byte)i);
            }

            var writer = new BlobWriter(256);
            builder.WriteContentTo(ref writer);
            AssertEx.Equal(new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13
            }, writer.ToArray());

            builder.WriteByte(0xff);

            builder.WriteContentTo(ref writer);
            AssertEx.Equal(new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13,
                0xff,
            }, writer.ToArray());
        }

        [Fact]
        public void WriteContentToBlobBuilder()
        {
            var builder1 = new BlobBuilder(16);
            for (int i = 0; i < 20; i++)
            {
                builder1.WriteByte((byte)i);
            }

            var builder2 = new BlobBuilder(256);
            builder1.WriteContentTo(builder2);
            AssertEx.Equal(new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13
            }, builder2.ToArray());

            builder1.WriteByte(0xff);

            builder1.WriteContentTo(builder2);
            AssertEx.Equal(new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13,
                0xff,
            }, builder2.ToArray());
        }

        [Fact]
        public void LinkSuffix1()
        {
            var builder1 = new BlobBuilder(16);
            builder1.WriteByte(1);
            builder1.WriteByte(2);
            builder1.WriteByte(3);

            var builder2 = new BlobBuilder(16);
            builder2.WriteByte(4);

            builder1.LinkSuffix(builder2);

            AssertEx.Equal(new byte[] { 1, 2, 3, 4 }, builder1.ToArray());
        }

        [Fact]
        public void LinkPrefix1()
        {
            var builder1 = new BlobBuilder(16);
            builder1.WriteByte(1);
            builder1.WriteByte(2);
            builder1.WriteByte(3);

            var builder2 = new BlobBuilder(16);
            builder2.WriteByte(4);

            builder1.LinkPrefix(builder2);

            AssertEx.Equal(new byte[] { 4, 1, 2, 3 }, builder1.ToArray());
        }

        [Fact]
        public void Link()
        {
            var builder1 = new BlobBuilder(16);
            builder1.WriteByte(1);

            var builder2 = new BlobBuilder(16);
            builder2.WriteByte(2);

            var builder3 = new BlobBuilder(16);
            builder3.WriteByte(3);

            var builder4 = new BlobBuilder(16);
            builder4.WriteByte(4);

            var builder5 = new BlobBuilder(16);
            builder5.WriteByte(5);

            builder2.LinkPrefix(builder1);
            AssertEx.Equal(new byte[] { 1, 2 }, builder2.ToArray());
            Assert.Throws<InvalidOperationException>(() => builder1.ToArray());
            Assert.Throws<InvalidOperationException>(() => builder2.LinkPrefix(builder1));
            Assert.Throws<InvalidOperationException>(() => builder1.WriteByte(0xff));
            Assert.Throws<InvalidOperationException>(() => builder1.WriteBytes(1, 10));
            Assert.Throws<InvalidOperationException>(() => builder1.WriteBytes(new byte[] { 1 }));
            Assert.Throws<InvalidOperationException>(() => builder1.ReserveBytes(1));
            Assert.Throws<InvalidOperationException>(() => builder1.GetBlobs());
            Assert.Throws<InvalidOperationException>(() => builder1.ContentEquals(builder1));
            Assert.Throws<InvalidOperationException>(() => builder1.WriteUTF16("str"));
            Assert.Throws<InvalidOperationException>(() => builder1.WriteUTF8("str", allowUnpairedSurrogates: false));

            builder2.LinkSuffix(builder3);
            AssertEx.Equal(new byte[] { 1, 2, 3 }, builder2.ToArray());
            Assert.Throws<InvalidOperationException>(() => builder3.LinkPrefix(builder5));

            builder2.LinkPrefix(builder4);
            AssertEx.Equal(new byte[] { 4, 1, 2, 3 }, builder2.ToArray());
            Assert.Throws<InvalidOperationException>(() => builder4.LinkPrefix(builder5));

            builder2.LinkSuffix(builder5);
            AssertEx.Equal(new byte[] { 4, 1, 2, 3, 5 }, builder2.ToArray());
        }

        [Fact]
        public unsafe void Write_Errors()
        {
            var builder = new BlobBuilder(16);
            Assert.Throws<ArgumentNullException>(() => builder.WriteUTF16((char[])null));
            Assert.Throws<ArgumentNullException>(() => builder.WriteUTF16((string)null));
            Assert.Throws<ArgumentNullException>(() => builder.WriteUTF8(null, allowUnpairedSurrogates: true));
            Assert.Throws<ArgumentNullException>(() => builder.WriteUTF8(null, allowUnpairedSurrogates: true));
            Assert.Throws<ArgumentNullException>(() => builder.TryWriteBytes((Stream)null, 0));
            Assert.Throws<ArgumentNullException>(() => builder.WriteBytes(null));
            Assert.Throws<ArgumentNullException>(() => builder.WriteBytes(null, 0, 0));
            Assert.Throws<ArgumentNullException>(() => builder.WriteBytes((byte*)null, 0));
            Assert.Throws<ArgumentNullException>(() => builder.WriteBytes(default(ImmutableArray<byte>)));
            Assert.Throws<ArgumentNullException>(() => builder.WriteBytes(default(ImmutableArray<byte>), 0, 0));

            var bw = default(BlobWriter);
            Assert.Throws<ArgumentNullException>(() => builder.WriteContentTo(ref bw));
            Assert.Throws<ArgumentNullException>(() => builder.WriteContentTo((Stream)null));
            Assert.Throws<ArgumentNullException>(() => builder.WriteContentTo((BlobBuilder)null));

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.TryWriteBytes(new MemoryStream(), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.WriteBytes(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.WriteBytes(new byte[] { }, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.WriteBytes(new byte[] { }, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.WriteBytes(new byte[] { }, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.WriteBytes(ImmutableArray<byte>.Empty, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.WriteBytes(ImmutableArray<byte>.Empty, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.WriteBytes(ImmutableArray<byte>.Empty, 1, -1));
        }

        [Fact]
        public void ReserveBytes1()
        {
            var builder = new BlobBuilder(16);
            var writer0 = builder.ReserveBytes(0);
            var writer1 = builder.ReserveBytes(1);
            var writer2 = builder.ReserveBytes(2);
            Assert.Equal(3, builder.Count);
            AssertEx.Equal(new byte[] { 0, 0, 0 }, builder.ToArray());

            Assert.Equal(0, writer0.Length);
            Assert.Equal(0, writer0.RemainingBytes);

            writer1.WriteBoolean(true);
            Assert.Equal(1, writer1.Length);
            Assert.Equal(0, writer1.RemainingBytes);

            writer2.WriteByte(1);
            Assert.Equal(2, writer2.Length);
            Assert.Equal(1, writer2.RemainingBytes);
        }

        [Fact]
        public void ReserveBytes2()
        {
            var builder = new BlobBuilder(16);
            var writer = builder.ReserveBytes(17);
            writer.WriteBytes(1, 17);

            var blobs = builder.GetBlobs().ToArray();
            Assert.Equal(1, blobs.Length);
            AssertEx.Equal(new byte[] 
            {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01
            }, blobs[0].GetUnderlyingBuffer().ToArray());
        }

        // TODO: 
        // WriteBytes(byte*)
        // WriteBytes(stream)
        // WriteBytes(byte[])
        // WriteBytes(IA<byte>)
        // WriteReference

        private static void TestCompressedUnsignedInteger(byte[] expected, int value)
        {
            var writer = new BlobWriter(4);
            writer.WriteCompressedInteger((uint)value);
            AssertEx.Equal(expected, writer.ToArray());

            var builder = new BlobBuilder();
            builder.WriteCompressedInteger((uint)value);
            AssertEx.Equal(expected, builder.ToArray());
        }

        private static void TestCompressedSignedInteger(byte[] expected, int value)
        {
            var writer = new BlobWriter(4);
            writer.WriteCompressedSignedInteger(value);
            AssertEx.Equal(expected, writer.ToArray());

            var builder = new BlobBuilder();
            builder.WriteCompressedSignedInteger(value);
            AssertEx.Equal(expected, builder.ToArray());
        }

        [Fact]
        public void CompressUnsignedIntegersFromSpecExamples()
        {
            // These examples are straight from the CLI spec.

            TestCompressedUnsignedInteger(new byte[] { 0x00 }, 0);
            TestCompressedUnsignedInteger(new byte[] { 0x03 }, 0x03);
            TestCompressedUnsignedInteger(new byte[] { 0x7f }, 0x7F);                    
            TestCompressedUnsignedInteger(new byte[] { 0x80, 0x80 }, 0x80);              
            TestCompressedUnsignedInteger(new byte[] { 0xAE, 0x57 }, 0x2E57);            
            TestCompressedUnsignedInteger(new byte[] { 0xBF, 0xFF }, 0x3FFF);            
            TestCompressedUnsignedInteger(new byte[] { 0xC0, 0x00, 0x40, 0x00 }, 0x4000);
            TestCompressedUnsignedInteger(new byte[] { 0xDF, 0xFF, 0xFF, 0xFF }, 0x1FFFFFFF);
        }

        [Fact]
        public void CompressSignedIntegersFromSpecExamples()
        {
            // These examples are straight from the CLI spec.
            TestCompressedSignedInteger(new byte[] { 0x00 }, 0);
            TestCompressedSignedInteger(new byte[] { 0x02 }, 1);
            TestCompressedSignedInteger(new byte[] { 0x06 }, 3);
            TestCompressedSignedInteger(new byte[] { 0x7f }, -1);
            TestCompressedSignedInteger(new byte[] { 0x7b }, -3);
            TestCompressedSignedInteger(new byte[] { 0x80, 0x80 }, 64);
            TestCompressedSignedInteger(new byte[] { 0x01 }, -64);
            TestCompressedSignedInteger(new byte[] { 0xC0, 0x00, 0x40, 0x00 }, 8192);
            TestCompressedSignedInteger(new byte[] { 0x80, 0x01 }, -8192);
            TestCompressedSignedInteger(new byte[] { 0xDF, 0xFF, 0xFF, 0xFE }, 268435455);
            TestCompressedSignedInteger(new byte[] { 0xC0, 0x00, 0x00, 0x01 }, -268435456);
        }

        [Fact]
        public void WritePrimitive()
        {
            var writer = new BlobBuilder(17);

            writer.WriteUInt32(0x11223344);
            writer.WriteUInt16(0x5566);
            writer.WriteByte(0x77);
            writer.WriteUInt64(0x8899aabbccddeeff);
            writer.WriteInt32(-1);
            writer.WriteInt16(-2);
            writer.WriteSByte(-3);
            writer.WriteBoolean(true);
            writer.WriteBoolean(false);
            writer.WriteInt64(unchecked((long)0xfedcba0987654321));
            writer.WriteDateTime(new DateTime(0x1112223334445556));
            writer.WriteDecimal(102030405060.70m);
            writer.WriteDouble(double.NaN);
            writer.WriteSingle(float.NegativeInfinity);

            AssertEx.Equal(new byte[] 
            {
                0x44, 0x33, 0x22, 0x11,
                0x66, 0x55,
                0x77,
                0xff, 0xee, 0xdd, 0xcc, 0xbb, 0xaa, 0x99, 0x88,
                0xff, 0xff, 0xff, 0xff, 
                0xfe, 0xff,
                0xfd,
                0x01,
                0x00,
                0x21, 0x43, 0x65, 0x87, 0x09, 0xBA, 0xDC, 0xFE,
                0x56, 0x55, 0x44, 0x34, 0x33, 0x22, 0x12, 0x11,
                0x02, 0xD6, 0xE0, 0x9A, 0x94, 0x47, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF8, 0xFF,
                0x00, 0x00, 0x80, 0xFF
            }, writer.ToArray());
        }

        [Fact]
        public void WriteBytes1()
        {
            var writer = new BlobBuilder(4);

            writer.WriteBytes(new byte[] { 1, 2, 3, 4 });
            writer.WriteBytes(new byte[] { });
            writer.WriteBytes(new byte[] { }, 0, 0);
            writer.WriteBytes(new byte[] { 5, 6, 7, 8 });
            writer.WriteBytes(new byte[] { 9 });
            writer.WriteBytes(new byte[] { 0x0a }, 0, 0);
            writer.WriteBytes(new byte[] { 0x0b }, 0, 1);
            writer.WriteBytes(new byte[] { 0x0c }, 1, 0);
            writer.WriteBytes(new byte[] { 0x0d, 0x0e }, 1, 1);

            AssertEx.Equal(new byte[]
            {
                0x01, 0x02, 0x03, 0x04,
                0x05, 0x06, 0x07, 0x08,
                0x09,
                0x0b,
                0x0e
            }, writer.ToArray());
        }

        [Fact]
        public void WriteBytes2()
        {
            var writer = new BlobBuilder(4);

            writer.WriteBytes(0xff, 0);
            writer.WriteBytes(1, 4);
            writer.WriteBytes(0xff, 0);
            writer.WriteBytes(2, 10);
            writer.WriteBytes(0xff, 0);
            writer.WriteBytes(3, 1);
            
            AssertEx.Equal(new byte[]
            {
                0x01, 0x01, 0x01, 0x01,
                0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02,
                0x03
            }, writer.ToArray());
        }

        [Fact]
        public void WriteAlignPad()
        {
            var writer = new BlobBuilder(4);

            writer.WriteByte(0x01);
            writer.PadTo(2);
            writer.WriteByte(0x02);
            writer.Align(4);
            writer.Align(4);

            writer.WriteByte(0x03);
            writer.Align(4);

            writer.WriteByte(0x04);
            writer.WriteByte(0x05);
            writer.Align(8);

            writer.WriteByte(0x06);
            writer.Align(2);
            writer.Align(1);

            AssertEx.Equal(new byte[]
            {
                0x01, 0x00, 0x02, 0x00,
                0x03, 0x00, 0x00, 0x00,
                0x04, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x06, 0x00
            }, writer.ToArray());
        }

        [Fact]
        public void WriteUTF16()
        {
            var writer = new BlobBuilder(4);

            writer.WriteUTF16("");
            writer.WriteUTF16("a");
            writer.WriteUTF16("");
            writer.WriteUTF16(new char[0]);

            writer.WriteUTF16(new char[] { '\ud800' });           // hi surrogate
            writer.WriteUTF16("\udc00");                          // lo surrogate
            writer.WriteUTF16("\ud800\udc00");                    // pair
            writer.WriteUTF16(new char[] { '\udc00', '\ud800' }); // lo + hi
            writer.WriteUTF16("\u1234");       

            AssertEx.Equal(new byte[]
            {
                0x61, 0x00,
                0x00, 0xD8,
                0x00, 0xDC,
                0x00, 0xD8, 0x00, 0xDC,
                0x00, 0xDC, 0x00, 0xD8,
                0x34, 0x12
            }, writer.ToArray());
        }

        [Fact]
        public void WriteSerializedString()
        {
            var writer = new BlobBuilder(4);

            writer.WriteSerializedString("");
            writer.WriteSerializedString("a");
            writer.WriteSerializedString(null);
            writer.WriteSerializedString("");

            writer.WriteSerializedString("\ud800");       // hi surrogate
            writer.WriteSerializedString("\udc00");       // lo surrogate
            writer.WriteSerializedString("\ud800\udc00"); // pair
            writer.WriteSerializedString("\udc00\ud800"); // lo + hi
            writer.WriteSerializedString("\u1234");

            AssertEx.Equal(new byte[]
            {
                0x00,
                0x01, 0x61,
                0xff,
                0x00,
                0x03, 0xED, 0xA0, 0x80,
                0x03, 0xED, 0xB0, 0x80,
                0x04, 0xF0, 0x90, 0x80, 0x80,
                0x06, 0xED, 0xB0, 0x80, 0xED, 0xA0, 0x80,
                0x03, 0xE1, 0x88, 0xB4
            }, writer.ToArray());
        }

        [Fact]
        public void WriteUTF8_AllowUnpairedSurrogates()
        {
            var writer = new BlobBuilder(4);
            writer.WriteUTF8("a", allowUnpairedSurrogates: true);
            writer.WriteUTF8("", allowUnpairedSurrogates: true);
            writer.WriteUTF8("bc", allowUnpairedSurrogates: true);
            writer.WriteUTF8("d", allowUnpairedSurrogates: true);
            writer.WriteUTF8("", allowUnpairedSurrogates: true);

            writer.WriteUTF8(Encoding.UTF8.GetString(new byte[]
            {
                0x00,
                0xC2, 0x80,
                0xE1, 0x88, 0xB4
            }), allowUnpairedSurrogates: true);

            writer.WriteUTF8("\0\ud800", allowUnpairedSurrogates: true);       // hi surrogate
            writer.WriteUTF8("\0\udc00", allowUnpairedSurrogates: true);       // lo surrogate
            writer.WriteUTF8("\0\ud800\udc00", allowUnpairedSurrogates: true); // pair
            writer.WriteUTF8("\0\udc00\ud800", allowUnpairedSurrogates: true); // lo + hi

            AssertEx.Equal(new byte[]
            {
                (byte)'a',
                (byte)'b', (byte)'c',
                (byte)'d',
                0x00, 0xC2, 0x80, 0xE1, 0x88, 0xB4,

                0x00, 0xED, 0xA0, 0x80,
                0x00, 0xED, 0xB0, 0x80,
                0x00, 0xF0, 0x90, 0x80, 0x80,
                0x00, 0xED, 0xB0, 0x80, 0xED, 0xA0, 0x80

            }, writer.ToArray());
        }

        [Fact]
        public void WriteUTF8_ReplaceUnpairedSurrogates()
        {
            var writer = new BlobBuilder(4);
            writer.WriteUTF8("a", allowUnpairedSurrogates: false);
            writer.WriteUTF8("", allowUnpairedSurrogates: false);
            writer.WriteUTF8("bc", allowUnpairedSurrogates: false);
            writer.WriteUTF8("d", allowUnpairedSurrogates: false);
            writer.WriteUTF8("", allowUnpairedSurrogates: false);

            writer.WriteUTF8(Encoding.UTF8.GetString(new byte[]
            {
                0x00,
                0xC2, 0x80,
                0xE1, 0x88, 0xB4
            }), allowUnpairedSurrogates: false);

            writer.WriteUTF8("\0\ud800", allowUnpairedSurrogates: false);       // hi surrogate
            writer.WriteUTF8("\0\udc00", allowUnpairedSurrogates: false);       // lo surrogate
            writer.WriteUTF8("\0\ud800\udc00", allowUnpairedSurrogates: false); // pair
            writer.WriteUTF8("\0\udc00\ud800", allowUnpairedSurrogates: false); // lo + hi

            AssertEx.Equal(new byte[]
            {
                (byte)'a',
                (byte)'b', (byte)'c',
                (byte)'d',
                0x00, 0xC2, 0x80, 0xE1, 0x88, 0xB4,

                0x00, 0xEF, 0xBF, 0xBD,
                0x00, 0xEF, 0xBF, 0xBD,
                0x00, 0xF0, 0x90, 0x80, 0x80,
                0x00, 0xEF, 0xBF, 0xBD, 0xEF, 0xBF, 0xBD

            }, writer.ToArray());
        }

        [Fact]
        public void EmptyWrites()
        {
            var writer = new BlobWriter(16);
            writer.WriteBytes(1, 16);
            writer.WriteBytes(new byte[] { });
            writer.WriteBytes(2, 0);
            writer.WriteUTF8("", allowUnpairedSurrogates: false);
            writer.WriteUTF16("");
            Assert.Equal(16, writer.Length);
        }

        [Fact]
        public void Pooled()
        {
            var builder1 = PooledBlobBuilder.GetInstance();
            var builder2 = PooledBlobBuilder.GetInstance();
            var builder3 = new BlobBuilder();

            builder1.WriteByte(1);
            builder2.WriteByte(2);
            builder3.WriteByte(3);

            // mix pooled with non-pooled
            builder1.LinkPrefix(builder3);

            builder1.Free();
            builder2.Free();
        }

        [Fact]
        public void ProperStreamRead()
        {
            var firstRead = true;
            var sourceArray = new byte[] { 1, 2, 3, 4 };
            int sourceOffset = 0;

            var stream = new TestStream(readFunc: (buf, offset, count) =>
            {
                if (firstRead)
                {
                    count = count / 2;
                    firstRead = false;
                }
                Array.Copy(sourceArray, sourceOffset, buf, offset, count);
                sourceOffset += count;
                return count;
            });

            var builder = PooledBlobBuilder.GetInstance(sourceArray.Length);
            Assert.Equal(sourceArray.Length, builder.TryWriteBytes(stream, sourceArray.Length));
            Assert.Equal(sourceArray, builder.ToArray());

            builder.Free();
        }

        [Fact]
        public void PrematureEndOfStream()
        {
            var sourceArray = new byte[] { 1, 2, 3, 4 };
            var stream = new MemoryStream(sourceArray);

            var destArray = new byte[6];
            var builder = PooledBlobBuilder.GetInstance(destArray.Length);

            // Try to write more bytes than exist in the stream
            Assert.Equal(4, builder.TryWriteBytes(stream, 6));

            Assert.Equal(sourceArray, builder.ToArray());

            builder.Free();
        }
    }
}
