// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal sealed class TestFileSystem((string filePath, string contents)[]? files) : IFileSystem
{
    public List<(string source, string destination)> MovedFiles { get; } = new();

    public bool FileExists(string filePath)
        => files?.Any(f => FilePathNormalizingComparer.Instance.Equals(f.filePath, filePath)) ?? false;

    public string ReadFile(string filePath)
        => files.AssumeNotNull().Single(f => FilePathNormalizingComparer.Instance.Equals(f.filePath, filePath)).contents;

    public Stream OpenReadStream(string filePath)
        => new MemoryStream(Encoding.UTF8.GetBytes(ReadFile(filePath)));

    public IEnumerable<string> GetDirectories(string workspaceDirectory)
        => throw new NotImplementedException();

    public IEnumerable<string> GetFiles(string workspaceDirectory, string searchPattern, SearchOption searchOption)
        => throw new NotImplementedException();

    public void Move(string sourceFilePath, string destinationFilePath)
        => MovedFiles.Add((sourceFilePath, destinationFilePath));
}
