// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace Roslyn.Utilities
{
    internal static class IncrementalHashExtensions
    {
        internal static void AppendData(this IncrementalHash hash, IEnumerable<Blob> blobs)
        {
            foreach (var blob in blobs)
            {
                hash.AppendData(blob.GetBytes());
            }
        }

        internal static void AppendData(this IncrementalHash hash, IEnumerable<ArraySegment<byte>> blobs)
        {
            foreach (var blob in blobs)
            {
                hash.AppendData(blob);
            }
        }

        internal static void AppendData(this IncrementalHash hash, ArraySegment<byte> segment)
        {
            RoslynDebug.AssertNotNull(segment.Array);
            hash.AppendData(segment.Array, segment.Offset, segment.Count);
        }
    }
}
