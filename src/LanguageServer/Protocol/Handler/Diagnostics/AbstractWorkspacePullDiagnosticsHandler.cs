// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Threading;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractWorkspacePullDiagnosticsHandler<TDiagnosticsParams, TReport, TReturn>
    : AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn>, IDisposable
    where TDiagnosticsParams : IPartialResultParams<TReport>
{
    private readonly LspWorkspaceRegistrationService _workspaceRegistrationService;
    private readonly LspWorkspaceManager _workspaceManager;
    private readonly IDiagnosticsRefresher _diagnosticsRefresher;
    protected readonly IDiagnosticSourceManager DiagnosticSourceManager;

    /// <summary>
    /// Stores the LSP changed state on a per category basis.  This ensures that requests for different categories
    /// are 'walled off' from each other and only reset state for their own category.
    /// </summary>
    private readonly ConcurrentDictionary<string, ReleaseAllAutoResetEvent> _categoryToLspChanged = [];

    protected AbstractWorkspacePullDiagnosticsHandler(
        LspWorkspaceManager workspaceManager,
        LspWorkspaceRegistrationService registrationService,
        IDiagnosticSourceManager diagnosticSourceManager,
        IDiagnosticsRefresher diagnosticRefresher,
        IGlobalOptionService globalOptions)
        : base(diagnosticRefresher, globalOptions)
    {
        DiagnosticSourceManager = diagnosticSourceManager;
        _workspaceManager = workspaceManager;
        _workspaceRegistrationService = registrationService;
        _diagnosticsRefresher = diagnosticRefresher;

        _workspaceRegistrationService.LspSolutionChanged += OnLspSolutionChanged;
        _workspaceManager.LspTextChanged += OnLspTextChanged;
        _diagnosticsRefresher.WorkspaceRefreshRequested += OnWorkspaceRefreshRequested;
    }

    public void Dispose()
    {
        _diagnosticsRefresher.WorkspaceRefreshRequested -= OnWorkspaceRefreshRequested;
        _workspaceManager.LspTextChanged -= OnLspTextChanged;
        _workspaceRegistrationService.LspSolutionChanged -= OnLspSolutionChanged;
    }

    protected override async ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(TDiagnosticsParams diagnosticsParams, string? requestDiagnosticCategory, RequestContext context, CancellationToken cancellationToken)
    {
        if (context.ServerKind == WellKnownLspServerKinds.RazorLspServer)
        {
            // If we're being called from razor, we do not support WorkspaceDiagnostics at all.  For razor, workspace
            // diagnostics will be handled by razor itself, which will operate by calling into Roslyn and asking for
            // document-diagnostics instead.
            return [];
        }

        return await DiagnosticSourceManager.CreateWorkspaceDiagnosticSourcesAsync(context, requestDiagnosticCategory, cancellationToken).ConfigureAwait(false);
    }

    private void OnLspSolutionChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        UpdateLspChanged();
    }

    private void OnLspTextChanged(object? sender, EventArgs e)
    {
        UpdateLspChanged();
    }

    private void OnWorkspaceRefreshRequested()
    {
        UpdateLspChanged();
    }

    private void UpdateLspChanged()
    {
        // Loop through our map of source -> has changed and mark them as all having changed.
        foreach (var categoryResetEvent in _categoryToLspChanged.Values)
            categoryResetEvent.Set();
    }

    protected override async Task WaitForChangesAsync(string? category, RequestContext context, CancellationToken cancellationToken)
    {
        // A null category counts a separate category and should track changes independently of other categories, so we'll add an empty entry in our map for it.
        category ??= string.Empty;

        // Wait until the workspace changes again (or was changed while we were in the middle of processing).
        // We'll use a variant of an AutoResetEvent so in the case we were to have multiple requests for the same category,
        // they're all released. That's not expected to happen, but it ensures better behavior in the case of a misbehaving client.
        var resetEvent = _categoryToLspChanged.GetOrAdd(category, static _ => new ReleaseAllAutoResetEvent(initialState: true));
        await resetEvent.WaitAsync().WithCancellation(cancellationToken).ConfigureAwait(false);

        // We've hit a change, so we close the current request to allow the client to open a new one.
        context.TraceDebug($"Closing workspace/diagnostics request for {category}");
    }

    internal abstract TestAccessor GetTestAccessor();

    internal readonly struct TestAccessor(AbstractWorkspacePullDiagnosticsHandler<TDiagnosticsParams, TReport, TReturn> handler)
    {
        public void TriggerConnectionClose() => handler.UpdateLspChanged();
    }

    /// <summary>
    /// An <see cref="AutoResetEvent"/> with two differences: it supports async waiting, and in the case Set() releases a waiter, it releases all waiters rather than just one.
    /// 
    /// The semantics of this type are thus: there is internally a "set" state. When the event is <see cref="Set()"/>, the next waiter to call <see cref="WaitAsync()"/> will be let through, and the
    /// event resets to false. A call to <see cref="Set()"/> while there is already waiters will release all the waiters, and since it already let a waiter through, the state is untouched.
    /// </summary>
    private sealed class ReleaseAllAutoResetEvent
    {
        private readonly object _gate = new object();
        private readonly List<TaskCompletionSource<object?>> _waiters = new();

        /// <summary>
        /// True if <see cref="Set()"/> has been called, indicating the next waiter should be let through.
        /// </summary>
        private bool _state;

        public ReleaseAllAutoResetEvent(bool initialState)
        {
            _state = initialState;
        }

        public Task WaitAsync()
        {
            lock (_gate)
            {
                if (_state)
                {
                    // Since _state was true, we let the next waiter through. Any waiter after that must wait for the next Set().
                    Contract.ThrowIfTrue(_waiters.Count > 0);
                    _state = false;
                    return Task.CompletedTask;
                }
                else
                {
                    // Passing RunContinuationsAsynchronously ensures we can call SetResult() on this without any risk of the things running inside the lock.
                    var waiter = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _waiters.Add(waiter);
                    return waiter.Task;
                }
            }
        }

        public void Set()
        {
            lock (_gate)
            {
                if (_waiters.Count > 0)
                {
                    // We had some waiters waiting, so _state should be false. We'll let all the waiters through.
                    Contract.ThrowIfTrue(_state);

                    foreach (var waiter in _waiters)
                        waiter.SetResult(null);

                    _waiters.Clear();
                }
                else
                {
                    // There are no waiters, so we'll let the next waiter through when they call WaitAsync().
                    _state = true;
                }
            }
        }
    }
}
