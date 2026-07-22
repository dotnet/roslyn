// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal sealed class WorkspaceFoldersChangedEventArgs(ImmutableArray<string> addedFolders, ImmutableArray<string> removedFolders) : EventArgs
{
    public ImmutableArray<string> AddedFolders { get; } = addedFolders;
    public ImmutableArray<string> RemovedFolders { get; } = removedFolders;
}

internal interface IInitializeManager : ILspService
{
    ClientCapabilities GetClientCapabilities();

    ClientCapabilities? TryGetClientCapabilities();

    InitializeParams? TryGetInitializeParams();

    /// <summary>Expected to be non-default after the Initialize event.</summary>
    ImmutableArray<string> GetRequiredWorkspaceFolderPaths();

    void SetInitializeParams(InitializeParams initializeParams);

    /// <summary>
    /// Updates the set of workspace folders after initialization.
    /// Called in response to workspace/didChangeWorkspaceFolders notifications.
    /// </summary>
    void UpdateWorkspaceFolders(ImmutableArray<string> addedFolders, ImmutableArray<string> removedFolders);

    /// <summary>
    /// Raised when workspace folders are added or removed.
    /// </summary>
    event EventHandler<WorkspaceFoldersChangedEventArgs>? WorkspaceFoldersChanged;
}
