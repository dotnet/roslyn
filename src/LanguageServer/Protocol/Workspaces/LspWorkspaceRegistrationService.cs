// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Per-LSP-server view of the workspaces registered with the process-wide
/// <see cref="LspWorkspaceRegistrationEventListener"/>. Created via
/// <see cref="LspWorkspaceRegistrationServiceFactory"/> so each LSP server
/// gets its own instance with its own subscriptions and lifetime.
/// </summary>
internal sealed class LspWorkspaceRegistrationService : ILspService, IDisposable
{
    private readonly LspWorkspaceRegistrationEventListener _eventListener;
    private readonly object _gate = new();

    // These arrays are kept in sync, with _workspaceChangedDisposers[i]
    // representing a disposer for a WorkspaceChanged event handler on the
    // workspace at _registrations[i].
    private ImmutableArray<Workspace> _registrations = [];
    private ImmutableArray<WorkspaceEventRegistration> _workspaceChangedDisposers = [];
    private bool _disposed;

    public LspWorkspaceRegistrationService(LspWorkspaceRegistrationEventListener eventListener)
    {
        _eventListener = eventListener;

        // Atomically capture the current set of workspaces and subscribe for
        // future register/deregister notifications. Any workspace returned in
        // the snapshot will not also be reported via OnWorkspaceRegistered.
        var initial = _eventListener.SubscribeAndGetRegisteredWorkspaces(OnWorkspaceRegistered, OnWorkspaceDeregistered);
        foreach (var workspace in initial)
            OnWorkspaceRegistered(workspace);
    }

    public ImmutableArray<Workspace> GetAllRegistrations()
    {
        lock (_gate)
        {
            return _registrations;
        }
    }

    private void OnWorkspaceRegistered(Workspace workspace)
    {
        lock (_gate)
        {
            if (_disposed || _registrations.Contains(workspace))
                return;

            var disposer = workspace.RegisterWorkspaceChangedHandler(OnLspWorkspaceChanged);
            _registrations = _registrations.Add(workspace);
            _workspaceChangedDisposers = _workspaceChangedDisposers.Add(disposer);
        }
    }

    private void OnWorkspaceDeregistered(Workspace workspace)
    {
        WorkspaceEventRegistration? disposer = null;
        lock (_gate)
        {
            var index = _registrations.IndexOf(workspace);
            if (index < 0)
                return;

            disposer = _workspaceChangedDisposers[index];
            _registrations = _registrations.RemoveAt(index);
            _workspaceChangedDisposers = _workspaceChangedDisposers.RemoveAt(index);
        }

        disposer.Dispose();
    }

    private void OnLspWorkspaceChanged(WorkspaceChangeEventArgs e)
    {
        LspSolutionChanged?.Invoke(this, e);
    }

    public void Dispose()
    {
        // Unsubscribe from the listener first so any StartListening/StopListening
        // cycle the listener begins after this call returns will not include
        // our handlers. Notifications already in flight (where the listener
        // captured the handler snapshot before our Unsubscribe took effect)
        // are short-circuited by the _disposed check below.
        _eventListener.Unsubscribe(OnWorkspaceRegistered, OnWorkspaceDeregistered);

        ImmutableArray<WorkspaceEventRegistration> disposers;
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            disposers = _workspaceChangedDisposers;
            _registrations = [];
            _workspaceChangedDisposers = [];
        }

        foreach (var disposer in disposers)
            disposer.Dispose();
    }

    /// <summary>
    /// Indicates whether the LSP solution has changed in a non-tracked document context. May be raised on any thread.
    /// </summary>
    public EventHandler<WorkspaceChangeEventArgs>? LspSolutionChanged;
}
