// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
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

    public void UpdateWorkspaceFolders(WorkspaceFoldersChangeEvent workspaceFoldersChangeEvent)
    {
        Contract.ThrowIfNull(_initializeParams, $"Tried to update {nameof(WorkspaceFolder)}s before initialize completed.");
        Contract.ThrowIfTrue(_workspaceFolderPathsOpt.IsDefault, $"{nameof(_workspaceFolderPathsOpt)} was not initialized.");

        using var _ = ArrayBuilder<string>.GetInstance(out var workspaceFolderPathsBuilder);
        workspaceFolderPathsBuilder.AddRange(_workspaceFolderPathsOpt);

        foreach (var removedWorkspaceFolder in workspaceFoldersChangeEvent.Removed)
        {
            if (TryGetFolderPath(removedWorkspaceFolder) is { } removedWorkspaceFolderPath)
            {
                workspaceFolderPathsBuilder.RemoveAll(path => string.Equals(path, removedWorkspaceFolderPath, StringComparison.OrdinalIgnoreCase));
            }
        }

        foreach (var addedWorkspaceFolder in workspaceFoldersChangeEvent.Added)
        {
            if (TryGetFolderPath(addedWorkspaceFolder) is { } addedWorkspaceFolderPath &&
                !workspaceFolderPathsBuilder.Any(path => string.Equals(path, addedWorkspaceFolderPath, StringComparison.OrdinalIgnoreCase)))
            {
                workspaceFolderPathsBuilder.Add(addedWorkspaceFolderPath);
            }
        }

        _workspaceFolderPathsOpt = workspaceFolderPathsBuilder.ToImmutable();

        static string? TryGetFolderPath(WorkspaceFolder workspaceFolder)
        {
            return workspaceFolder.DocumentUri.ParsedUri is null
                ? null
                : workspaceFolder.DocumentUri.GetDocumentFilePathFromUri();
        }
    }

    public ClientCapabilities? TryGetClientCapabilities()
    {
        return _initializeParams?.Capabilities;
    }
}
