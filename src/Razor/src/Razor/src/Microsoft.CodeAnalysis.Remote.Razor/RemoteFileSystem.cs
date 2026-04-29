// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Remote.Razor;

[Export(typeof(IFileSystem)), Shared]
internal class RemoteFileSystem : IFileSystem
{
    private IFileSystem _fileSystem = new FileSystem();

    public bool FileExists(string filePath)
        => _fileSystem.FileExists(filePath);

    public string ReadFile(string filePath)
        => _fileSystem.ReadFile(filePath);

    public Stream OpenReadStream(string filePath)
        => _fileSystem.OpenReadStream(filePath);

    public IEnumerable<string> GetDirectories(string workspaceDirectory)
        => _fileSystem.GetDirectories(workspaceDirectory);

    public IEnumerable<string> GetFiles(string workspaceDirectory, string searchPattern, SearchOption searchOption)
        => _fileSystem.GetFiles(workspaceDirectory, searchPattern, searchOption);

    public void Move(string sourceFilePath, string destinationFilePath)
        => _fileSystem.Move(sourceFilePath, destinationFilePath);

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(RemoteFileSystem instance)
    {
        public void SetFileSystem(IFileSystem fileSystem)
        {
            instance._fileSystem = fileSystem;
        }
    }
}
