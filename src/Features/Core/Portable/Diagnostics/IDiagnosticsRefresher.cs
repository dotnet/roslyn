// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Used to send request for diagnostic pull to the client.
/// </summary>
internal interface IDiagnosticsRefresher
{
    event Action? WorkspaceRefreshRequested;

    /// <summary>
    /// Requests workspace diagnostics refresh.
    /// Any component that maintains state whose change may affect reported diagnostics should call <see cref="RequestWorkspaceRefresh"/> whenever that state changes.
    /// Any component that reports diagnostics based on the value of a global option should also call <see cref="RequestWorkspaceRefresh"/> whenever the option value changes.
    /// </summary>
    void RequestWorkspaceRefresh();

    /// <summary>
    /// Current version of global state that may affect diagnostics. Incremented on every refresh.
    /// Used to determine whether any global state that might affect workspace diagnostics has changed.
    /// </summary>
    int GlobalStateVersion { get; }
}

[Export(typeof(IDiagnosticsRefresher)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultDiagnosticsRefresher() : IDiagnosticsRefresher
{
    /// <summary>
    /// Incremented every time a refresh is requested.
    /// </summary>
    private int _globalStateVersion;

    public event Action? WorkspaceRefreshRequested;

    public void RequestWorkspaceRefresh()
    {
        // bump version before sending the request to the client:
        Interlocked.Increment(ref _globalStateVersion);

        WorkspaceRefreshRequested?.Invoke();
    }

    public int GlobalStateVersion
        => _globalStateVersion;
}
