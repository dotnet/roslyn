// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal sealed class TestStrongNameFileSystem : StrongNameFileSystem
    {
        internal Func<string, byte[]> ReadAllBytesFunc { get; set; }
        internal Func<string, FileMode, FileAccess, FileShare, FileStream> CreateFileStreamFunc { get; set; }

        internal TestStrongNameFileSystem(string? signingTempPath)
            : base(signingTempPath)
        {
            ReadAllBytesFunc = base.ReadAllBytes;
            CreateFileStreamFunc = base.CreateFileStream;
        }

        internal override byte[] ReadAllBytes(string fullPath) => ReadAllBytesFunc(fullPath);
        internal override FileStream CreateFileStream(string filePath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare) =>
            CreateFileStreamFunc(filePath, fileMode, fileAccess, fileShare);
    }
}
