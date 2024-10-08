// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;

namespace Microsoft.CodeAnalysis.SymbolSearch;

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
