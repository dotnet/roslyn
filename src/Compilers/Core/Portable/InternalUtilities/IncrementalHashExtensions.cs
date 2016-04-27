// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Reflection;

namespace Roslyn.Utilities
{
    internal static class IncrementalHashExtensions
    {
        internal static void AppendData(this IncrementalHash hash, BlobBuilder builder)
        {
            foreach (var blob in builder.GetBlobs())
            {
                hash.AppendData(blob.GetBytes());
            }
        }

        internal static void AppendData(this IncrementalHash hash, ArraySegment<byte> segment)
        {
            hash.AppendData(segment.Array, segment.Offset, segment.Count);
        }
    }
}
