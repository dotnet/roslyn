// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private static byte[] GetBytes(Stream stream)
        {
            // If we were provided a memory stream to begin with, we can preallocate the right size
            // byte[] to copy into.  Note: this is potentially allocating large buffers.  Those will
            // be GC'ed, but they can have a negative effect on the large object heap as compaction
            // is rarer there.  Unfortunately, the .Net sqlite wrapper library does not expose any
            // way to just get access to the underlying sqlite blob to read/write directly to.
            if (stream is MemoryStream memoryStream)
            {
                var bytes = new byte[memoryStream.Length];
                var readResult = memoryStream.Read(bytes, offset: 0, count: bytes.Length);

                // See https://referencesource.microsoft.com/#mscorlib/system/io/memorystream.cs,339
                // Memory stream always copies the full amount requested into the byte[].  So we 
                // should always be good with a single read.
                Contract.ThrowIfFalse(readResult == bytes.Length);
                return bytes;
            }
            else
            {
                using (var tempStream = new MemoryStream())
                {
                    stream.CopyTo(tempStream);
                    return tempStream.ToArray();
                }
            }
        }
    }
}