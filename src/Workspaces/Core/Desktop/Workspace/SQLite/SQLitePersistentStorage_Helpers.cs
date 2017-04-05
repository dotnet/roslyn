// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
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
                    if (stream.Length <= MaxPooledByteArrayLength)
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

            // Couldn't use our pool.  Just copy the bytes out of the stream entirely.
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

        internal const long MaxPooledByteArrayLength = 128 * 1024;

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
                s_byteArrayPool.Push(bytes);
            }
        }
    }
}