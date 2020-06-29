// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace BuildValidator
{
    class FileNameEqualityComparer : IEqualityComparer<FileInfo>
    {
        public readonly static FileNameEqualityComparer Instance = new FileNameEqualityComparer();

        private FileNameEqualityComparer()
        {
        }

        public bool Equals(FileInfo? x, FileInfo? y)
            => x?.Name == y?.Name;

        public int GetHashCode(FileInfo? file)
            => file?.Name.GetHashCode() ?? 0;
    }
}
