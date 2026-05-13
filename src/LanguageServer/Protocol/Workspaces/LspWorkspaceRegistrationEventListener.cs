// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Logger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Process-wide singleton that learns about workspaces from the MEF
/// <see cref="IEventListener"/> infrastructure and exposes them to
/// per-server <see cref="LspWorkspaceRegistrationService"/> instances.
/// </summary>
[ExportEventListener(
    WellKnownEventListeners.Workspace,
    WorkspaceKind.Host,
    WorkspaceKind.MiscellaneousFiles,
    WorkspaceKind.MetadataAsSource,
    WorkspaceKind.Interactive,
    WorkspaceKind.SemanticSearch), Shared]
[Export(typeof(LspWorkspaceRegistrationEventListener))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspWorkspaceRegistrationEventListener() : IEventListener
{
    private readonly object _gate = new();
    private ImmutableArray<Workspace> _registeredWorkspaces = [];
    private ImmutableArray<Action<Workspace>> _onRegisteredHandlers = [];
    private ImmutableArray<Action<Workspace>> _onDeregisteredHandlers = [];

    /// <summary>
    /// Returns the current set of workspaces tracked by this listener.
    /// </summary>
    public ImmutableArray<Workspace> GetRegisteredWorkspaces()
    {
        lock (_gate)
        {
            return _registeredWorkspaces;
        }
    }

    /// <summary>
    /// Atomically captures the current snapshot of registered workspaces and
    /// adds the provided handlers. Workspaces present in the returned snapshot
    /// will not also raise <paramref name="onRegistered"/>; future
    /// registrations will. Future deregistrations raise
    /// <paramref name="onDeregistered"/>.
    /// </summary>
    public ImmutableArray<Workspace> SubscribeAndGetRegisteredWorkspaces(
        Action<Workspace> onRegistered,
        Action<Workspace> onDeregistered)
    {
        lock (_gate)
        {
            _onRegisteredHandlers = _onRegisteredHandlers.Add(onRegistered);
            _onDeregisteredHandlers = _onDeregisteredHandlers.Add(onDeregistered);
            return _registeredWorkspaces;
        }
    }

    public void Unsubscribe(
        Action<Workspace> onRegistered,
        Action<Workspace> onDeregistered)
    {
        lock (_gate)
        {
            _onRegisteredHandlers = _onRegisteredHandlers.Remove(onRegistered);
            _onDeregisteredHandlers = _onDeregisteredHandlers.Remove(onDeregistered);
        }
    }

    public void StartListening(Workspace workspace)
    {
        Logger.Log(FunctionId.RegisterWorkspace, KeyValueLogMessage.Create(LogType.Trace, static (m, workspace) =>
        {
            m["WorkspaceKind"] = workspace.Kind;
            m["WorkspaceCanOpenDocuments"] = workspace.CanOpenDocuments;
            m["WorkspaceCanChangeActiveContextDocument"] = workspace.CanChangeActiveContextDocument;
            m["WorkspacePartialSemanticsEnabled"] = workspace.PartialSemanticsEnabled;
        }, workspace));

        ImmutableArray<Action<Workspace>> handlers;
        lock (_gate)
        {
            if (_registeredWorkspaces.Contains(workspace))
                return;

            _registeredWorkspaces = _registeredWorkspaces.Add(workspace);
            handlers = _onRegisteredHandlers;
        }

        foreach (var handler in handlers)
            handler(workspace);
    }

    public void StopListening(Workspace workspace)
    {
        ImmutableArray<Action<Workspace>> handlers;
        lock (_gate)
        {
            var index = _registeredWorkspaces.IndexOf(workspace);
            if (index < 0)
                return;

            _registeredWorkspaces = _registeredWorkspaces.RemoveAt(index);
            handlers = _onDeregisteredHandlers;
        }

        foreach (var handler in handlers)
            handler(workspace);
    }
}

