// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Roslyn.Utilities
{
    internal static class RoslynIncrementalHashExtensions
    {
        internal static void AppendData(this RoslynIncrementalHash hash, IEnumerable<Blob> blobs)
        {
            foreach (var blob in blobs)
            {
                hash.AppendData(blob.GetBytes());
            }
        }

        internal static void AppendData(this RoslynIncrementalHash hash, IEnumerable<ArraySegment<byte>> blobs)
        {
            foreach (var blob in blobs)
            {
                hash.AppendData(blob);
            }
        }

        internal static void AppendData(this RoslynIncrementalHash hash, ArraySegment<byte> segment)
        {
            RoslynDebug.AssertNotNull(segment.Array);
            hash.AppendData(segment.Array, segment.Offset, segment.Count);
        }
    }
}
