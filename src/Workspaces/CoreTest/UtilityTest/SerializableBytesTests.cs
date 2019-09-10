// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SerializableBytesTests
    {
        [Fact]
        public async Task ReadableStreamTestReadAByteAtATime()
        {
            using var expected = new MemoryStream();
            for (var i = 0; i < 10000; i++)
            {
                expected.WriteByte((byte)(i % byte.MaxValue));
            }

            expected.Position = 0;
            using var stream = await SerializableBytes.CreateReadableStreamAsync(expected, CancellationToken.None);
            Assert.Equal(expected.Length, stream.Length);

            expected.Position = 0;
            stream.Position = 0;
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected.ReadByte(), stream.ReadByte());
            }
        }

        [Fact]
        public async Task ReadableStreamTestReadChunks()
        {
            using var expected = new MemoryStream();
            for (var i = 0; i < 10000; i++)
            {
                expected.WriteByte((byte)(i % byte.MaxValue));
            }

            expected.Position = 0;
            using var stream = await SerializableBytes.CreateReadableStreamAsync(expected, CancellationToken.None);
            Assert.Equal(expected.Length, stream.Length);

            stream.Position = 0;

            var index = 0;
            int count;
            var bytes = new byte[1000];

            while ((count = stream.Read(bytes, 0, bytes.Length)) > 0)
            {
                for (var i = 0; i < count; i++)
                {
                    Assert.Equal((byte)(index % byte.MaxValue), bytes[i]);
                    index++;
                }
            }

            Assert.Equal(index, stream.Length);
        }

        [Fact]
        public async Task ReadableStreamTestReadRandomBytes()
        {
            using var expected = new MemoryStream();
            for (var i = 0; i < 10000; i++)
            {
                expected.WriteByte((byte)(i % byte.MaxValue));
            }

            expected.Position = 0;
            using var stream = await SerializableBytes.CreateReadableStreamAsync(expected, CancellationToken.None);
            Assert.Equal(expected.Length, stream.Length);

            var random = new Random(0);
            for (var i = 0; i < 100; i++)
            {
                var position = random.Next((int)expected.Length);
                expected.Position = position;
                stream.Position = position;

                Assert.Equal(expected.ReadByte(), stream.ReadByte());
            }
        }

        [Fact]
        public void WritableStreamTest1()
        {
            using var expected = new MemoryStream();
            for (var i = 0; i < 10000; i++)
            {
                expected.WriteByte((byte)(i % byte.MaxValue));
            }

            expected.Position = 0;
            using var stream = SerializableBytes.CreateWritableStream();
            for (var i = 0; i < 10000; i++)
            {
                stream.WriteByte((byte)(i % byte.MaxValue));
            }

            StreamEqual(expected, stream);
        }

        [Fact]
        public void WritableStreamTest2()
        {
            using var expected = new MemoryStream();
            for (var i = 0; i < 10000; i++)
            {
                expected.WriteByte((byte)(i % byte.MaxValue));
            }

            expected.Position = 0;
            using var stream = SerializableBytes.CreateWritableStream();
            for (var i = 0; i < 10000; i++)
            {
                stream.WriteByte((byte)(i % byte.MaxValue));
            }

            Assert.Equal(expected.Length, stream.Length);

            stream.Position = 0;

            var index = 0;
            int count;
            var bytes = new byte[1000];

            while ((count = stream.Read(bytes, 0, bytes.Length)) > 0)
            {
                for (var i = 0; i < count; i++)
                {
                    Assert.Equal((byte)(index % byte.MaxValue), bytes[i]);
                    index++;
                }
            }

            Assert.Equal(index, stream.Length);
        }

        [Fact]
        public void WritableStreamTest3()
        {
            using var expected = new MemoryStream();
            using var stream = SerializableBytes.CreateWritableStream();
            var random = new Random(0);
            for (var i = 0; i < 100; i++)
            {
                var position = random.Next(10000);
                WriteByte(expected, stream, position, i);
            }

            StreamEqual(expected, stream);
        }

        [Fact]
        public void WritableStreamTest4()
        {
            using var expected = new MemoryStream();
            using var stream = SerializableBytes.CreateWritableStream();
            var random = new Random(0);
            for (var i = 0; i < 100; i++)
            {
                var position = random.Next(10000);
                WriteByte(expected, stream, position, i);

                var position1 = random.Next(10000);
                var temp = GetInitializedArray(100 + position1);
                Write(expected, stream, position1, temp);
            }

            StreamEqual(expected, stream);
        }

        private static void WriteByte(Stream expected, Stream stream, int position, int value)
        {
            expected.Position = position;
            stream.Position = position;

            var valueByte = (byte)(value % byte.MaxValue);
            expected.WriteByte(valueByte);
            stream.WriteByte(valueByte);
        }

        private static void Write(Stream expected, Stream stream, int position, byte[] array)
        {
            expected.Position = position;
            stream.Position = position;

            expected.Write(array, 0, array.Length);
            stream.Write(array, 0, array.Length);
        }

        private static byte[] GetInitializedArray(int length)
        {
            var temp = new byte[length];
            for (var j = 0; j < temp.Length; j++)
            {
                temp[j] = (byte)(j % byte.MaxValue);
            }

            return temp;
        }

        private static void StreamEqual(Stream expected, Stream stream)
        {
            Assert.Equal(expected.Length, stream.Length);

            var random = new Random(0);

            expected.Position = 0;
            stream.Position = 0;

            var read1 = new byte[10000];
            var read2 = new byte[10000];
            while (expected.Position < expected.Length)
            {
                var count = random.Next(read1.Length) + 1;
                var return1 = expected.Read(read1, 0, count);
                var return2 = stream.Read(read2, 0, count);

                Assert.Equal(return1, return2);

                for (var i = 0; i < return1; i++)
                {
                    Assert.Equal(read1[i], read2[i]);
                }

                Assert.Equal(expected.ReadByte(), stream.ReadByte());
            }
        }
    }
}
