// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    /// <summary>
    /// Used so we can mock out how the search service does IO for testing purposes.
    /// </summary>
    internal interface IIOService
    {
        void Create(DirectoryInfo directory);
        void Delete(FileInfo file);
        bool Exists(FileSystemInfo info);
        byte[] ReadAllBytes(string path);
        void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors);
        void Move(string sourceFileName, string destinationFileName);
        void WriteAndFlushAllBytes(string path, byte[] bytes);
    }
}
