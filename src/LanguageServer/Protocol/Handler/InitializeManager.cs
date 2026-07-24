// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal sealed class InitializeManager : IInitializeManager
{
    public InitializeManager()
    {
    }

    private InitializeParams? _initializeParams;
    private ImmutableArray<string> _workspaceFolderPathsOpt;

    public event EventHandler<WorkspaceFoldersChangedEventArgs>? WorkspaceFoldersChanged;

    public ClientCapabilities GetClientCapabilities()
    {
        if (_initializeParams?.Capabilities is null)
        {
            throw new InvalidOperationException($"Tried to get required {nameof(ClientCapabilities)} before it was set");
        }

        return _initializeParams.Capabilities;
    }

    public void SetInitializeParams(InitializeParams initializeParams)
    {
        Contract.ThrowIfFalse(_initializeParams == null);
        _initializeParams = initializeParams;
        _workspaceFolderPathsOpt = initializeParams.WorkspaceFolders is [_, ..] workspaceFolders ? GetFolderPaths(workspaceFolders) : [];

        static ImmutableArray<string> GetFolderPaths(WorkspaceFolder[] workspaceFolders)
        {
            var builder = ArrayBuilder<string>.GetInstance(workspaceFolders.Length);
            foreach (var workspaceFolder in workspaceFolders)
            {
                if (workspaceFolder.DocumentUri.ParsedUri is not { } parsedUri)
                    continue;

                var workspaceFolderPath = workspaceFolder.DocumentUri.GetDocumentFilePathFromUri();
                builder.Add(workspaceFolderPath);
            }

            return builder.ToImmutableAndFree();
        }
    }

    public InitializeParams? TryGetInitializeParams()
    {
        return _initializeParams;
    }

    public ImmutableArray<string> GetRequiredWorkspaceFolderPaths()
    {
        Contract.ThrowIfTrue(_workspaceFolderPathsOpt.IsDefault, $"{nameof(_workspaceFolderPathsOpt)} was not initialized. Was this accessed before the OnInitialized event ran?");
        return _workspaceFolderPathsOpt;
    }

    public ClientCapabilities? TryGetClientCapabilities()
    {
        return _initializeParams?.Capabilities;
    }

    public void UpdateWorkspaceFolders(ImmutableArray<string> addedFolders, ImmutableArray<string> removedFolders)
    {
        Contract.ThrowIfTrue(_workspaceFolderPathsOpt.IsDefault, $"{nameof(_workspaceFolderPathsOpt)} was not initialized. Was this called before OnInitialized?");

        var builder = _workspaceFolderPathsOpt.ToBuilder();

        // Remove old folders
        foreach (var removedFolder in removedFolders)
        {
            builder.Remove(removedFolder);
        }

        // Add new folders
        foreach (var addedFolder in addedFolders)
        {
            if (!builder.Contains(addedFolder))
            {
                builder.Add(addedFolder);
            }
        }

        _workspaceFolderPathsOpt = builder.ToImmutable();

        // Notify subscribers of the workspace folder changes
        WorkspaceFoldersChanged?.Invoke(this, new WorkspaceFoldersChangedEventArgs(addedFolders, removedFolders));
    }
}
