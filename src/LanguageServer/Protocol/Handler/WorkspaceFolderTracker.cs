// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal sealed class WorkspaceFolderTracker : IWorkspaceFolderTracker
{
    private readonly object _gate = new();
    private ImmutableArray<string> _workspaceFolderPaths;

    public void Initialize(WorkspaceFolder[]? workspaceFolders)
    {
        lock (_gate)
        {
            Contract.ThrowIfFalse(_workspaceFolderPaths.IsDefault);

            if (workspaceFolders is not [_, ..])
            {
                _workspaceFolderPaths = [];
                return;
            }

            var builder = ImmutableArray.CreateBuilder<string>(workspaceFolders.Length);
            foreach (var workspaceFolder in workspaceFolders)
            {
                if (workspaceFolder.DocumentUri.ParsedUri is null)
                    continue;

                builder.Add(NormalizePath(workspaceFolder.DocumentUri.GetDocumentFilePathFromUri()));
            }

            _workspaceFolderPaths = builder.ToImmutable();
        }
    }

    public ImmutableArray<string> GetRequiredWorkspaceFolderPaths()
    {
        lock (_gate)
        {
            Contract.ThrowIfTrue(_workspaceFolderPaths.IsDefault, $"{nameof(_workspaceFolderPaths)} was not initialized. Was this accessed before the Initialize request ran?");
            return _workspaceFolderPaths;
        }
    }

    public void Update(ImmutableArray<string> addedFolders, ImmutableArray<string> removedFolders)
    {
        lock (_gate)
        {
            Contract.ThrowIfTrue(_workspaceFolderPaths.IsDefault, $"{nameof(_workspaceFolderPaths)} was not initialized. Was this called before the Initialize request ran?");

            var builder = _workspaceFolderPaths.ToBuilder();
            foreach (var removedFolder in removedFolders)
                builder.Remove(NormalizePath(removedFolder), StringComparer.OrdinalIgnoreCase);

            foreach (var addedFolder in addedFolders)
            {
                var normalizedFolder = NormalizePath(addedFolder);
                if (builder.IndexOf(normalizedFolder, 0, builder.Count, StringComparer.OrdinalIgnoreCase) < 0)
                    builder.Add(normalizedFolder);
            }

            _workspaceFolderPaths = builder.ToImmutable();
        }
    }

    private static string NormalizePath(string path)
        => Path.GetFullPath(path);
}
