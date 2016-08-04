// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    internal class SourceTextComparer : IEqualityComparer<SourceText>
    {
        public static SourceTextComparer Instance = new SourceTextComparer();

        public bool Equals(SourceText x, SourceText y)
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

        public int GetHashCode(SourceText obj)
        {
            var checksum = obj.GetChecksum(useDefaultEncodingIfNull: true);
            var contentsHash = !checksum.IsDefault ? Hash.CombineValues(checksum) : 0;
            var encodingHash = obj.Encoding != null ? obj.Encoding.GetHashCode() : 0;

            return Hash.Combine(obj.Length,
                Hash.Combine(contentsHash,
                Hash.Combine(encodingHash, obj.ChecksumAlgorithm.GetHashCode())));
        }
    }
}
