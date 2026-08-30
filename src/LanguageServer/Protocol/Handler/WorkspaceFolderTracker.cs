// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal sealed class WorkspaceFolderTracker : IWorkspaceFolderTracker
{
    // Mutations are serialized by the request queue, but non-mutating requests may read the current folders concurrently.
    private readonly object _gate = new();
    private ImmutableHashSet<string> _workspaceFolderPaths = ImmutableHashSet.Create(PathUtilities.Comparer);

    public event Action<ImmutableHashSet<string>>? WorkspaceFoldersChanged;

    public void Update(WorkspaceFolder[]? addedFolders, WorkspaceFolder[]? removedFolders)
    {
        ImmutableHashSet<string> updatedWorkspaceFolderPaths;
        lock (_gate)
        {
            updatedWorkspaceFolderPaths = _workspaceFolderPaths;
            if (removedFolders is not null)
            {
                foreach (var workspaceFolder in removedFolders)
                {
                    if (GetNormalizedFilePath(workspaceFolder) is not { } normalizedPath)
                        continue;

                    updatedWorkspaceFolderPaths = updatedWorkspaceFolderPaths.Remove(normalizedPath);
                }
            }

            if (addedFolders is not null)
            {
                foreach (var workspaceFolder in addedFolders)
                {
                    if (GetNormalizedFilePath(workspaceFolder) is not { } normalizedPath)
                        continue;

                    updatedWorkspaceFolderPaths = updatedWorkspaceFolderPaths.Add(normalizedPath);
                }
            }

            if (updatedWorkspaceFolderPaths.SetEquals(_workspaceFolderPaths))
                return;

            _workspaceFolderPaths = updatedWorkspaceFolderPaths;
        }

        WorkspaceFoldersChanged?.Invoke(updatedWorkspaceFolderPaths);
    }

    public ImmutableHashSet<string> GetRequiredWorkspaceFolderPaths()
    {
        lock (_gate)
        {
            return _workspaceFolderPaths;
        }
    }

    private static string? GetNormalizedFilePath(WorkspaceFolder workspaceFolder)
        => workspaceFolder.DocumentUri.ParsedDocumentUri?.IsFile == true
            ? NormalizePath(workspaceFolder.DocumentUri.GetDocumentFilePathFromUri())
            : null;

    private static string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var rootLength = Path.GetPathRoot(fullPath)?.Length ?? 0;
        return fullPath.Length > rootLength && PathUtilities.IsDirectorySeparator(fullPath[^1])
            ? fullPath[..^1]
            : fullPath;
    }
}
