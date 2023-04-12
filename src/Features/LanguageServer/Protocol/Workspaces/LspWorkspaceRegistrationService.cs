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
    private ImmutableArray<Workspace> _registrations = ImmutableArray.Create<Workspace>();

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

        Logger.Log(FunctionId.RegisterWorkspace, KeyValueLogMessage.Create(LogType.Trace, m =>
        {
            m["WorkspaceKind"] = workspace.Kind;
            m["WorkspaceCanOpenDocuments"] = workspace.CanOpenDocuments;
            m["WorkspaceCanChangeActiveContextDocument"] = workspace.CanChangeActiveContextDocument;
            m["WorkspacePartialSemanticsEnabled"] = workspace.PartialSemanticsEnabled;
        }));

        lock (_gate)
        {
            _registrations = _registrations.Add(workspace);
        }

        // Forward workspace change events for all registered LSP workspaces.
        workspace.WorkspaceChanged += OnLspWorkspaceChanged;
    }

    public void Deregister(Workspace? workspace)
    {
        if (workspace is null)
            return;

        workspace.WorkspaceChanged -= OnLspWorkspaceChanged;
        lock (_gate)
        {
            _registrations = _registrations.Remove(workspace);
        }
    }

    private void OnLspWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        LspSolutionChanged?.Invoke(this, e);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var workspace in _registrations)
            {
                workspace.WorkspaceChanged -= OnLspWorkspaceChanged;
            }

            _registrations = _registrations.Clear();
        }
    }

    /// <summary>
    /// Indicates whether the LSP solution has changed in a non-tracked document context.
    /// 
    /// <b>IMPORTANT:</b> Implementations of this event handler should do as little synchronous work as possible since this will block.
    /// </summary>
    public EventHandler<WorkspaceChangeEventArgs>? LspSolutionChanged;
}

internal class LspWorkspaceRegisteredEventArgs : EventArgs
{
    public Workspace Workspace { get; }

    public LspWorkspaceRegisteredEventArgs(Workspace workspace)
    {
        Workspace = workspace;
    }
}
