// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Remote.DebugUtil
{
    internal static class TestUtils
    {
        public static void RemoveChecksums(this Dictionary<Checksum, object> map, ChecksumWithChildren checksums)
        {
            map.Remove(checksums.Checksum);

            foreach (var child in checksums.Children)
            {
                if (child is Checksum checksum)
                {
                    map.Remove(checksum);
                }

                if (child is ChecksumCollection collection)
                {
                    foreach (var item in collection)
                    {
                        map.Remove(item);
                    }
                }
            }
        }
    }
}
