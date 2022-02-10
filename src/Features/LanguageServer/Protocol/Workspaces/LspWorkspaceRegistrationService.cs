// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;
using Logger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal abstract class LspWorkspaceRegistrationService
{
    private readonly object _gate = new();
    private ImmutableArray<Workspace> _registrations = ImmutableArray.Create<Workspace>();

    public abstract string GetHostWorkspaceKind();

    public ImmutableArray<Workspace> GetAllRegistrations()
        => _registrations;

    public virtual void Register(Workspace workspace)
    {
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

        WorkspaceRegistered?.Invoke(this, new LspWorkspaceRegisteredEventArgs(workspace));
    }

    public event EventHandler<LspWorkspaceRegisteredEventArgs>? WorkspaceRegistered;
}

internal class LspWorkspaceRegisteredEventArgs : EventArgs
{
    public Workspace Workspace { get; }

    public LspWorkspaceRegisteredEventArgs(Workspace workspace)
    {
        Workspace = workspace;
    }
}
