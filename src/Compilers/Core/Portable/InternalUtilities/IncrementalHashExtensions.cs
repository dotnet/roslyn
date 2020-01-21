// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
            hash.AppendData(segment.Array, segment.Offset, segment.Count);
        }
    }
}
