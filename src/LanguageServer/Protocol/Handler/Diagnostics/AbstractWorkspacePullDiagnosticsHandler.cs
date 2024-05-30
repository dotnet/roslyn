// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractWorkspacePullDiagnosticsHandler<TDiagnosticsParams, TReport, TReturn>
    : AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn>, IDisposable
    where TDiagnosticsParams : IPartialResultParams<TReport>
{
    private readonly LspWorkspaceRegistrationService _workspaceRegistrationService;
    private readonly LspWorkspaceManager _workspaceManager;
    protected readonly IDiagnosticSourceManager DiagnosticSourceManager;

    /// <summary>
    /// Gate to guard access to <see cref="_categoryToLspChanged"/>
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// Stores the LSP changed state on a per category basis.  This ensures that requests for different categories
    /// are 'walled off' from each other and only reset state for their own category.
    /// </summary>
    private readonly Dictionary<string, bool> _categoryToLspChanged = new();

    protected AbstractWorkspacePullDiagnosticsHandler(
        LspWorkspaceManager workspaceManager,
        LspWorkspaceRegistrationService registrationService,
        IDiagnosticAnalyzerService diagnosticAnalyzerService,
        IDiagnosticSourceManager diagnosticSourceManager,
        IDiagnosticsRefresher diagnosticRefresher,
        IGlobalOptionService globalOptions) : base(diagnosticAnalyzerService, diagnosticRefresher, globalOptions)
    {
        DiagnosticSourceManager = diagnosticSourceManager;
        _workspaceManager = workspaceManager;
        _workspaceRegistrationService = registrationService;

        _workspaceRegistrationService.LspSolutionChanged += OnLspSolutionChanged;
        _workspaceManager.LspTextChanged += OnLspTextChanged;
    }

    public void Dispose()
    {
        _workspaceManager.LspTextChanged -= OnLspTextChanged;
        _workspaceRegistrationService.LspSolutionChanged -= OnLspSolutionChanged;
    }

    protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(TDiagnosticsParams diagnosticsParams, string? requestDiagnosticCategory, RequestContext context, CancellationToken cancellationToken)
    {
        if (context.ServerKind == WellKnownLspServerKinds.RazorLspServer)
        {
            // If we're being called from razor, we do not support WorkspaceDiagnostics at all.  For razor, workspace
            // diagnostics will be handled by razor itself, which will operate by calling into Roslyn and asking for
            // document-diagnostics instead.
            return new([]);
        }

        return DiagnosticSourceManager.CreateWorkspaceDiagnosticSourcesAsync(context, requestDiagnosticCategory, cancellationToken);
    }

    private void OnLspSolutionChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        UpdateLspChanged();
    }

    private void OnLspTextChanged(object? sender, EventArgs e)
    {
        UpdateLspChanged();
    }

    private void UpdateLspChanged()
    {
        lock (_gate)
        {
            // Loop through our map of source -> has changed and mark them as all having changed.
            foreach (var category in _categoryToLspChanged.Keys.ToImmutableArray())
            {
                _categoryToLspChanged[category] = true;
            }
        }
    }

    protected override async Task WaitForChangesAsync(string? category, RequestContext context, CancellationToken cancellationToken)
    {
        // A null category counts a separate category and should track changes independently of other categories, so we'll add an empty entry in our map for it.
        category ??= string.Empty;

        // Spin waiting until our LSP change flag has been set.  When the flag is set (meaning LSP has changed),
        // we reset the flag to false and exit out of the loop allowing the request to close.
        // The client will automatically trigger a new request as soon as we close it, bringing us up to date on diagnostics.
        while (!HasChanged())
        {
            // There have been no changes between now and when the last request finished - we will hold the connection open while we poll for changes.
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
        }

        // We've hit a change, so we close the current request to allow the client to open a new one.
        context.TraceInformation("Closing workspace/diagnostics request");
        return;

        bool HasChanged()
        {
            lock (_gate)
            {
                // Get the LSP changed value of this category.  If it doesn't exist we add it with a value of 'changed' since this is the first
                // request for the category and we don't know if it has changed since the request started.
                var changed = _categoryToLspChanged.GetOrAdd(category, true);
                if (changed)
                {
                    // We've observed a change, so we reset the flag to false for this source and return true.
                    _categoryToLspChanged[category] = false;
                }

                return changed;
            }
        }
    }

    internal abstract TestAccessor GetTestAccessor();

    internal readonly struct TestAccessor(AbstractWorkspacePullDiagnosticsHandler<TDiagnosticsParams, TReport, TReturn> handler)
    {
        public void TriggerConnectionClose() => handler.UpdateLspChanged();
    }
}
