// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Reflection;

namespace Roslyn.Utilities
{
    using Roslyn.Reflection;

    internal static class HashAlgorithmExtensions
    {
        internal static byte[] ComputeHash(this HashAlgorithm algorithm, BlobBuilder builder)
        {
            int remaining = builder.Count;
            foreach (var blob in builder.GetBlobs())
            {
                var segment = blob.GetBytes();
                remaining -= segment.Count;
                if (remaining == 0)
                {
                    algorithm.TransformFinalBlock(segment.Array, segment.Offset, segment.Count);
                }
                else
                {
                    algorithm.TransformBlock(segment.Array, segment.Offset, segment.Count);
                }
            }

            Debug.Assert(remaining == 0);
            return algorithm.Hash;
        }
    }
}
