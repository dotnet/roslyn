// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal interface IFileSystem
{
    IEnumerable<string> GetFiles(string workspaceDirectory, string searchPattern, SearchOption searchOption);

    IEnumerable<string> GetDirectories(string workspaceDirectory);

    bool FileExists(string filePath);

    string ReadFile(string filePath);

    Stream OpenReadStream(string filePath);

    void Move(string sourceFilePath, string destinationFilePath);
}
