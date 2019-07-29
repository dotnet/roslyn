// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal sealed class TestStrongNameFileSystem : StrongNameFileSystem
    {
        internal Func<string, byte[]> ReadAllBytesFunc { get; set; }
        internal Func<string, FileMode, FileAccess, FileShare, FileStream> CreateFileStreamFunc { get; set; }

        internal TestStrongNameFileSystem()
        {
            ReadAllBytesFunc = base.ReadAllBytes;
            CreateFileStreamFunc = base.CreateFileStream;
        }

        internal override byte[] ReadAllBytes(string fullPath) => ReadAllBytesFunc(fullPath);
        internal override FileStream CreateFileStream(string filePath, FileMode fileMode, FileAccess fileAccess, FileShare fileShare) =>
            CreateFileStreamFunc(filePath, fileMode, fileAccess, fileShare);
    }
}
