// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Helpers to create temporary streams backed by pooled memory
    /// </summary>
    internal static class SerializableBytes
    {
        private const int ChunkSize = SharedPools.ByteBufferSize;

        internal static PooledStream CreateReadableStream(byte[] bytes)
            => CreateReadableStream(bytes, bytes.Length);


        internal static PooledStream CreateReadableStream(byte[] bytes, int length)
        {
            var stream = CreateWritableStream();
            stream.Write(bytes, 0, length);

            stream.Position = 0;
            return stream;
        }

        internal async static Task<PooledStream> CreateReadableStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            var length = stream.Length;

            var chunkCount = (length + ChunkSize - 1) / ChunkSize;
            var chunks = new byte[chunkCount][];

            try
            {
                for (long i = 0, c = 0; i < length; i += ChunkSize, c++)
                {
                    var count = (int)Math.Min(ChunkSize, length - i);
                    var chunk = SharedPools.ByteArray.Allocate();

                    var chunkOffset = 0;
                    while (count > 0)
                    {
                        var bytesRead = await stream.ReadAsync(chunk, chunkOffset, count, cancellationToken).ConfigureAwait(false);
                        if (bytesRead > 0)
                        {
                            count -= bytesRead;
                            chunkOffset += bytesRead;
                        }
                        else
                        {
                            break;
                        }
                    }

                    chunks[c] = chunk;
                }

                var result = new ReadStream(length, chunks);
                chunks = null;
                return result;
            }
            finally
            {
                BlowChunks(chunks);
            }
        }

        // free any chunks remaining
        private static void BlowChunks(byte[][] chunks)
        {
            if (chunks != null)
            {
                for (long c = 0; c < chunks.Length; c++)
                {
                    if (chunks[c] != null)
                    {
                        SharedPools.ByteArray.Free(chunks[c]);
                        chunks[c] = null;
                    }
                }
            }
        }

        internal static PooledStream CreateWritableStream()
        {
            return new ReadWriteStream();
        }

        public class PooledStream : Stream
        {
            protected List<byte[]> chunks;

            protected long position;
            protected long length;

            protected PooledStream(long length, List<byte[]> chunks)
            {
                this.position = 0;
                this.length = length;
                this.chunks = chunks;
            }

            public override long Length
            {
                get
                {
                    return this.length;
                }
            }

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => false;

            public override void Flush()
            {
                // nothing to do, this is a read-only stream
            }

            public override long Position
            {
                get
                {
                    return this.position;
                }

                set
                {
                    if (value < 0 || value >= length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }

                    this.position = value;
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                long target;
                try
                {
                    target = origin switch
                    {
                        SeekOrigin.Begin => offset,
                        SeekOrigin.Current => checked(offset + position),
                        SeekOrigin.End => checked(offset + length),
                        _ => throw new ArgumentOutOfRangeException(nameof(origin)),
                    };
                }
                catch (OverflowException)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }

                if (target < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }

                position = target;
                return target;
            }

            public override int ReadByte()
            {
                if (position >= length)
                {
                    return -1;
                }

                var currentIndex = CurrentChunkIndex;
                var chunk = chunks[currentIndex];

                var currentOffset = CurrentChunkOffset;
                var result = chunk[currentOffset];

                this.position++;
                return result;
            }

            public override int Read(byte[] buffer, int index, int count)
            {
                if (count <= 0 || position >= length)
                {
                    return 0;
                }

                var totalCopyCount = Read(this.chunks, this.position, this.length, buffer, index, count);
                this.position += totalCopyCount;

                return totalCopyCount;
            }

            private static int Read(List<byte[]> chunks, long position, long length, byte[] buffer, int index, int count)
            {
                var oldPosition = position;

                while (count > 0 && position < length)
                {
                    var chunk = chunks[GetChunkIndex(position)];
                    var currentOffset = GetChunkOffset(position);

                    var copyCount = Math.Min(Math.Min(ChunkSize - currentOffset, count), (int)(length - position));
                    Array.Copy(chunk, currentOffset, buffer, index, copyCount);

                    position += copyCount;

                    index += copyCount;
                    count -= copyCount;
                }

                return (int)(position - oldPosition);
            }

            public byte[] ToArray()
            {
                if (this.Length == 0)
                {
                    return Array.Empty<byte>();
                }

                var array = new byte[this.Length];

                // read entire array
                Read(this.chunks, 0, this.length, array, 0, array.Length);
                return array;
            }

            public ImmutableArray<byte> ToImmutableArray()
            {
                var array = ToArray();
                return Roslyn.Utilities.ImmutableArrayExtensions.DangerousCreateFromUnderlyingArray(ref array);
            }

            protected int CurrentChunkIndex { get { return GetChunkIndex(this.position); } }
            protected int CurrentChunkOffset { get { return GetChunkOffset(this.position); } }

            protected static int GetChunkIndex(long value)
            {
                return (int)(value / ChunkSize);
            }

            protected static int GetChunkOffset(long value)
            {
                return (int)(value % ChunkSize);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (chunks != null)
                {
                    foreach (var chunk in chunks)
                    {
                        SharedPools.ByteArray.Free(chunk);
                    }

                    chunks = null;
                }
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }

        private class ReadStream : PooledStream
        {
            public ReadStream(long length, byte[][] chunks)
                : base(length, new List<byte[]>(chunks))
            {

            }
        }

        private class ReadWriteStream : PooledStream
        {
            public ReadWriteStream()
                : base(length: 0, chunks: SharedPools.BigDefault<List<byte[]>>().AllocateAndClear())
            {
                // growing list on EnsureSize shown as perf bottleneck. reuse shared list so that
                // we don't re-allocate as much.
            }

            public override bool CanWrite => true;

            public override long Position
            {
                get
                {
                    return base.Position;
                }

                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }

                    this.position = value;
                }
            }

            private void EnsureCapacity(long value)
            {
                var nextIndex = GetChunkIndex(value);
                for (var i = this.chunks.Count; i <= nextIndex; i++)
                {
                    // allocate memory and initialize it to zero
                    var chunk = SharedPools.ByteArray.Allocate();
                    Array.Clear(chunk, 0, chunk.Length);
                    chunks.Add(chunk);
                }
            }

            public override void SetLength(long value)
            {
                EnsureCapacity(value);

                if (value < length)
                {
                    // truncate the stream

                    var chunkIndex = GetChunkIndex(value);
                    var chunkOffset = GetChunkOffset(value);

                    Array.Clear(chunks[chunkIndex], chunkOffset, chunks[chunkIndex].Length - chunkOffset);

                    var trimIndex = chunkIndex + 1;
                    for (int i = trimIndex; i < chunks.Count; i++)
                    {
                        SharedPools.ByteArray.Free(chunks[i]);
                    }

                    chunks.RemoveRange(trimIndex, chunks.Count - trimIndex);
                }

                length = value;

                if (position > value)
                {
                    position = value;
                }
            }

            public override void WriteByte(byte value)
            {
                EnsureCapacity(this.position + 1);

                var currentIndex = CurrentChunkIndex;
                var currentOffset = CurrentChunkOffset;

                chunks[currentIndex][currentOffset] = value;

                this.position++;
                if (this.position >= length)
                {
                    this.length = this.position;
                }
            }

            public override void Write(byte[] buffer, int index, int count)
            {
                EnsureCapacity(this.position + count);

                var currentIndex = index;
                var countLeft = count;

                while (countLeft > 0)
                {
                    var chunk = chunks[CurrentChunkIndex];
                    var currentOffset = CurrentChunkOffset;

                    var writeCount = Math.Min(ChunkSize - currentOffset, countLeft);
                    Array.Copy(buffer, currentIndex, chunk, currentOffset, writeCount);

                    this.position += writeCount;

                    currentIndex += writeCount;
                    countLeft -= writeCount;
                }

                if (this.position >= length)
                {
                    this.length = this.position;
                }
            }

            protected override void Dispose(bool disposing)
            {
                var temp = this.chunks;

                base.Dispose(disposing);

                SharedPools.BigDefault<List<byte[]>>().ClearAndFree(temp);
            }
        }
    }
}
