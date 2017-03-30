// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using static System.FormattableString;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        private static Stream GetStream(byte[] bytes)
            => bytes == null ? null : new MemoryStream(bytes, writable: false);

        private static long GetProjectDataId(int projectId, int nameId)
            => CombineInt32ToInt64(projectId, nameId);

        private static string GetProjectIdString(int projectPathId, int projectNameId)
            => Invariant($"{projectPathId}-{projectNameId}");

        private static string GetDocumentIdString(int projectId, int documentPathId, int documentNameId)
            => Invariant($"{projectId}-{documentPathId}-{documentNameId}");

        private static long CombineInt32ToInt64(int v1, int v2)
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
                using (var tempStream = new MemoryStream(bytes))
                {
                    stream.CopyTo(tempStream);
                    return bytes;
                }
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