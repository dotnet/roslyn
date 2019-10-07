// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Roslyn.Utilities;
using static System.FormattableString;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        private static string GetProjectIdString(int projectPathId, int projectNameId)
            => Invariant($"{projectPathId}-{projectNameId}");

        private static string GetDocumentIdString(int projectId, int documentPathId, int documentNameId)
            => Invariant($"{projectId}-{documentPathId}-{documentNameId}");

        private static long CombineInt32ValuesToInt64(int v1, int v2)
            => ((long)v1 << 32) | (long)v2;

        private static (byte[] bytes, int length, bool fromPool) GetBytes(
            Checksum checksumOpt, CancellationToken cancellationToken)
        {
            // If we weren't passed a checksum, just pass the singleton empty byte array.
            // Note: we don't add this to/from our pool.  But it likely wouldn't be a problem
            // for us to do that as this instance can't actually be mutated since it's just
            // an empty array.
            if (checksumOpt == null)
            {
                return (Array.Empty<byte>(), length: 0, fromPool: false);
            }

            using var stream = SerializableBytes.CreateWritableStream();
            using var writer = new ObjectWriter(stream, cancellationToken: cancellationToken);
            checksumOpt.WriteTo(writer);
            stream.Position = 0;
            return GetBytes(stream);
        }

        private static (byte[] bytes, int length, bool fromPool) GetBytes(Stream stream)
        {
            // Attempt to copy into a pooled byte[] if the stream length is known and it's 
            // less than 128k.  This accounts for 99%+ of all of our streams while keeping
            // a generally small pool around (<10 items) when I've debugged VS.

            if (stream.CanSeek)
            {
                if (stream.Length >= 0 && stream.Length <= int.MaxValue)
                {
                    var length = (int)stream.Length;
                    byte[] bytes;
                    bool fromPool;
                    if (length <= MaxPooledByteArrayLength)
                    {
                        // use a pooled byte[] to store our data in.
                        bytes = GetPooledBytes();
                        fromPool = true;
                    }
                    else
                    {
                        // We knew the length, but it was large.  Copy the stream into that
                        // array, but don't pool it so we don't hold onto huge arrays forever.
                        bytes = new byte[length];
                        fromPool = false;
                    }

                    CopyTo(stream, bytes, length);
                    return (bytes, length, fromPool);
                }
            }

            // Not something we could get the length of. Just copy the bytes out of the stream entirely.
            using (var tempStream = new MemoryStream())
            {
                stream.CopyTo(tempStream);
                var bytes = tempStream.ToArray();
                return (bytes, bytes.Length, fromPool: false);
            }
        }

        private static void CopyTo(Stream stream, byte[] bytes, int length)
        {
            var index = 0;
            int read;
            while (length > 0 && (read = stream.Read(bytes, index, length)) != 0)
            {
                index += read;
                length -= read;
            }
        }

        /// <summary>
        /// Amount of time to wait between flushing writes to disk.  500ms means we can flush
        /// writes to disk two times a second.
        /// </summary>
        private const int FlushAllDelayMS = 500;

        /// <summary>
        /// We use a pool to cache reads/writes that are less than 4k.  Testing with Roslyn,
        /// 99% of all writes (48.5k out of 49.5k) are less than that size.  So this helps
        /// ensure that we can pool as much as possible, without caching excessively large 
        /// arrays (for example, Roslyn does write out nearly 50 chunks that are larger than
        /// 100k each).
        /// </summary>
        internal const long MaxPooledByteArrayLength = 4 * 1024;

        /// <summary>
        /// The max amount of byte[]s we cache.  This caps our cache at 4MB while allowing
        /// us to massively speed up writing (by batching writes).  Because we can write to
        /// disk two times a second.  That means a total of 8MB/s that can be written to disk
        /// using only our cache.  Given that Roslyn itself only writes about 50MB to disk
        /// after several minutes of analysis, this amount of bandwidth is more than sufficient.
        /// </summary>
        private const int MaxPooledByteArrays = 1024;

        private static readonly Stack<byte[]> s_byteArrayPool = new Stack<byte[]>();

        internal static byte[] GetPooledBytes()
        {
            byte[] bytes;
            lock (s_byteArrayPool)
            {
                if (s_byteArrayPool.Count > 0)
                {
                    bytes = s_byteArrayPool.Pop();
                }
                else
                {
                    bytes = new byte[MaxPooledByteArrayLength];
                }
            }

            Array.Clear(bytes, 0, bytes.Length);
            return bytes;
        }

        internal static void ReturnPooledBytes(byte[] bytes)
        {
            lock (s_byteArrayPool)
            {
                if (s_byteArrayPool.Count < MaxPooledByteArrays)
                {
                    s_byteArrayPool.Push(bytes);
                }
            }
        }
    }
}
