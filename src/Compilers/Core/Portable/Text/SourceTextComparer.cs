// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Text
{
    internal class SourceTextComparer : IEqualityComparer<SourceText?>
    {
        public static readonly SourceTextComparer Instance = new SourceTextComparer();

        public bool Equals(SourceText? x, SourceText? y)
        {
            if (x == null)
            {
                return y == null;
            }
            else if (y == null)
            {
                return false;
            }

            return x.ContentEquals(y);
        }

        public int GetHashCode(SourceText? obj)
        {
            if (obj is null)
            {
                return 0;
            }

            // GetContentHash returns a 16-byte, well-distributed, xx-hash-128 value.
            // So reading the first 4 bytes as an int is always safe, and will give a good hash code back for this instance.
            var contentHash = obj.GetContentHash();
            return MemoryMarshal.Read<int>(contentHash.AsSpan());
        }
    }
}
