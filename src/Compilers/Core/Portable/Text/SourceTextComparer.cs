// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

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

            var checksum = obj.GetChecksum();
            var contentsHash = !checksum.IsDefault ? Hash.CombineValues(checksum) : 0;
            var encodingHash = obj.Encoding != null ? obj.Encoding.GetHashCode() : 0;

            return Hash.Combine(obj.Length,
                Hash.Combine(contentsHash,
                Hash.Combine(encodingHash, ((int)obj.ChecksumAlgorithm).GetHashCode())));
        }
    }
}
