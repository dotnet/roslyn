// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal partial class SymbolSearchUpdateEngine
    {
        private class IOService : IIOService
        {
            public void Create(DirectoryInfo directory) => directory.Create();

            public void Delete(FileInfo file) => file.Delete();

            public bool Exists(FileSystemInfo info) => info.Exists;

            public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

            public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors) =>
                File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);

            public void Move(string sourceFileName, string destinationFileName) =>
                File.Move(sourceFileName, destinationFileName);

            public void WriteAndFlushAllBytes(string path, byte[] bytes)
            {
                using var fileStream = new FileStream(path, FileMode.Create);
                fileStream.Write(bytes, 0, bytes.Length);
                fileStream.Flush(flushToDisk: true);
            }
        }
    }
}
