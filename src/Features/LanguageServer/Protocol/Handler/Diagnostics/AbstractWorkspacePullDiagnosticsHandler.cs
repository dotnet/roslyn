// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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
    protected readonly IDiagnosticSourceManager _diagnosticSourceManager;

    /// <summary>
    /// Flag that represents whether the LSP view of the world has changed.
    /// It is totally fine for this to somewhat over-report changes
    /// as it is an optimization used to delay closing workspace diagnostic requests
    /// until something has changed.
    /// </summary>
    private int _lspChanged = 0;

    protected AbstractWorkspacePullDiagnosticsHandler(
        LspWorkspaceManager workspaceManager,
        LspWorkspaceRegistrationService registrationService,
        IDiagnosticAnalyzerService diagnosticAnalyzerService,
        IDiagnosticSourceManager diagnosticSourceManager,
        IDiagnosticsRefresher diagnosticRefresher,
        IGlobalOptionService globalOptions) : base(diagnosticAnalyzerService, diagnosticRefresher, globalOptions)
    {
        _diagnosticSourceManager = diagnosticSourceManager;
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

    protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(TDiagnosticsParams diagnosticsParams, string requestDiagnosticCategory, RequestContext context, CancellationToken cancellationToken)
    {
        return _diagnosticSourceManager.CreateDiagnosticSourcesAsync(context, requestDiagnosticCategory, false, cancellationToken);
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
        Interlocked.Exchange(ref _lspChanged, 1);
    }

    protected override async Task WaitForChangesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        // Spin waiting until our LSP change flag has been set.  When the flag is set (meaning LSP has changed),
        // we reset the flag to false and exit out of the loop allowing the request to close.
        // The client will automatically trigger a new request as soon as we close it, bringing us up to date on diagnostics.
        while (Interlocked.CompareExchange(ref _lspChanged, value: 0, comparand: 1) == 0)
        {
            // There have been no changes between now and when the last request finished - we will hold the connection open while we poll for changes.
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
        }

        context.TraceInformation("Closing workspace/diagnostics request");
        // We've hit a change, so we close the current request to allow the client to open a new one.
        return;
    }

    internal abstract TestAccessor GetTestAccessor();

    internal readonly struct TestAccessor(AbstractWorkspacePullDiagnosticsHandler<TDiagnosticsParams, TReport, TReturn> handler)
    {
        public void TriggerConnectionClose() => handler.UpdateLspChanged();
    }
}
