// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal sealed class FileSystem : IFileSystem
{
    public IEnumerable<string> GetFiles(string workspaceDirectory, string searchPattern, SearchOption searchOption)
        => Directory.GetFiles(workspaceDirectory, searchPattern, searchOption);

    public IEnumerable<string> GetDirectories(string workspaceDirectory)
        => Directory.GetDirectories(workspaceDirectory);

    public bool FileExists(string filePath)
        => File.Exists(filePath);

    public string ReadFile(string filePath)
        => File.ReadAllText(filePath);

    public Stream OpenReadStream(string filePath)
        => new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

    public void Move(string sourceFilePath, string destinationFilePath)
        => File.Move(sourceFilePath, destinationFilePath);
}
