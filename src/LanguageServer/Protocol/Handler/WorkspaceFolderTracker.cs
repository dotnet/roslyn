// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicLspServiceFactory(typeof(WorkspaceFolderTracker)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WorkspaceFolderTrackerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new WorkspaceFolderTracker();
}

internal sealed class WorkspaceFolderTracker : IWorkspaceFolderTracker, IOnInitialize
{
    // The gate makes updates atomic; volatile allows lock-free reads of the latest immutable snapshot.
    private readonly object _gate = new();
    private volatile ImmutableHashSet<string> _workspaceFolderPaths = ImmutableHashSet.Create(PathUtilities.Comparer);

    public event EventHandler? WorkspaceFoldersChanged;

    public Task OnInitializeAsync(InitializeParams initializeParams, RequestContext context, CancellationToken cancellationToken)
    {
        Update(initializeParams.WorkspaceFolders, removedFolders: null);
        return Task.CompletedTask;
    }

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

        WorkspaceFoldersChanged?.Invoke(this, EventArgs.Empty);
    }

    public ImmutableHashSet<string> GetRequiredWorkspaceFolderPaths()
        => _workspaceFolderPaths;

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
