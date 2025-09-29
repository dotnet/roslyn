// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;
using Logger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal abstract class LspWorkspaceRegistrationService : IDisposable
{
    private readonly object _gate = new();

    // These arrays are kept in sync, with _workspaceChangedDisposers[i] representing
    // a disposer for a WorkspaceChanged event on the workspace at _registrations[i]
    private ImmutableArray<Workspace> _registrations = [];
    private ImmutableArray<WorkspaceEventRegistration> _workspaceChangedDisposers = [];

    public ImmutableArray<Workspace> GetAllRegistrations()
    {
        lock (_gate)
        {
            return _registrations;
        }
    }

    public virtual void Register(Workspace? workspace)
    {
        if (workspace is null)
            return;

        Logger.Log(FunctionId.RegisterWorkspace, KeyValueLogMessage.Create(LogType.Trace, static (m, workspace) =>
        {
            m["WorkspaceKind"] = workspace.Kind;
            m["WorkspaceCanOpenDocuments"] = workspace.CanOpenDocuments;
            m["WorkspaceCanChangeActiveContextDocument"] = workspace.CanChangeActiveContextDocument;
            m["WorkspacePartialSemanticsEnabled"] = workspace.PartialSemanticsEnabled;
        }, workspace));

        var workspaceChangedDisposer = workspace.RegisterWorkspaceChangedHandler(OnLspWorkspaceChanged);

        lock (_gate)
        {
            _registrations = _registrations.Add(workspace);
            _workspaceChangedDisposers = _workspaceChangedDisposers.Add(workspaceChangedDisposer);
        }
    }

    public void Deregister(Workspace? workspace)
    {
        if (workspace is null)
            return;

        WorkspaceEventRegistration? disposer = null;
        lock (_gate)
        {
            var index = _registrations.IndexOf(workspace);

            // Handle the case where we were registered with a null workspace, but deregistered
            // with a non-null workspace
            if (index >= 0)
            {
                _registrations = _registrations.RemoveAt(index);

                disposer = _workspaceChangedDisposers[index];
                _workspaceChangedDisposers = _workspaceChangedDisposers.RemoveAt(index);
            }
        }

        disposer?.Dispose();
    }

    private void OnLspWorkspaceChanged(WorkspaceChangeEventArgs e)
    {
        LspSolutionChanged?.Invoke(this, e);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var disposer in _workspaceChangedDisposers)
            {
                disposer.Dispose();
            }

            _registrations = [];
            _workspaceChangedDisposers = [];
        }
    }

    /// <summary>
    /// Indicates whether the LSP solution has changed in a non-tracked document context. May be raised on any thread.
    /// </summary>
    public EventHandler<WorkspaceChangeEventArgs>? LspSolutionChanged;
}
