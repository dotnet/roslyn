// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;

// A document diagnostic partial report is defined as having the first literal send = WorkspaceDiagnosticReport followed
// by n WorkspaceDiagnosticReportPartialResult literals.
// See https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspace_diagnostic
using WorkspaceDiagnosticPartialReport = SumType<WorkspaceDiagnosticReport, WorkspaceDiagnosticReportPartialResult>;

[Method(Methods.WorkspaceDiagnosticName)]
internal sealed class PublicWorkspacePullDiagnosticsHandler : AbstractPullDiagnosticHandler<WorkspaceDiagnosticParams, WorkspaceDiagnosticPartialReport, WorkspaceDiagnosticReport?>, IDisposable
{
    private readonly LspWorkspaceRegistrationService _workspaceRegistrationService;
    private readonly LspWorkspaceManager _workspaceManager;

    /// <summary>
    /// Flag that represents whether the LSP view of the world has changed.
    /// It is totally fine for this to somewhat over-report changes
    /// as it is an optimization used to delay closing workspace diagnostic requests
    /// until something has changed.
    /// </summary>
    private int _lspChanged = 0;

    public PublicWorkspacePullDiagnosticsHandler(
        LspWorkspaceManager workspaceManager,
        LspWorkspaceRegistrationService registrationService,
        IDiagnosticAnalyzerService analyzerService,
        IDiagnosticsRefresher diagnosticRefresher,
        IGlobalOptionService globalOptions)
        : base(analyzerService, diagnosticRefresher, globalOptions)
    {
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

    /// <summary>
    /// Public API doesn't support categories (yet).
    /// </summary>
    protected override string? GetDiagnosticCategory(WorkspaceDiagnosticParams diagnosticsParams)
        => null;

    protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData)
    {
        return ConvertTags(diagnosticData, potentialDuplicate: false);
    }

    protected override WorkspaceDiagnosticPartialReport CreateReport(TextDocumentIdentifier identifier, VisualStudio.LanguageServer.Protocol.Diagnostic[] diagnostics, string resultId)
        => new WorkspaceDiagnosticPartialReport(new WorkspaceDiagnosticReport
        {
            Items = new SumType<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>[]
            {
                new WorkspaceFullDocumentDiagnosticReport
                {
                    Uri = identifier.Uri,
                    Items = diagnostics,
                    // The documents provided by workspace reports are never open, so we return null.
                    Version = null,
                    ResultId = resultId
                }
            }
        });

    protected override WorkspaceDiagnosticPartialReport CreateRemovedReport(TextDocumentIdentifier identifier)
        => new WorkspaceDiagnosticPartialReport(new WorkspaceDiagnosticReport
        {
            Items = new SumType<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>[]
            {
                new WorkspaceFullDocumentDiagnosticReport
                {
                    Uri = identifier.Uri,
                    Items = Array.Empty<VisualStudio.LanguageServer.Protocol.Diagnostic>(),
                    // The documents provided by workspace reports are never open, so we return null.
                    Version = null,
                    ResultId = null,
                }
            }
        });

    protected override WorkspaceDiagnosticPartialReport CreateUnchangedReport(TextDocumentIdentifier identifier, string resultId)
        => new WorkspaceDiagnosticPartialReport(new WorkspaceDiagnosticReport
        {
            Items = new SumType<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>[]
            {
                new WorkspaceUnchangedDocumentDiagnosticReport
                {
                    Uri = identifier.Uri,
                    // The documents provided by workspace reports are never open, so we return null.
                    Version = null,
                    ResultId = resultId,
                }
            }
        });

    protected override WorkspaceDiagnosticReport? CreateReturn(BufferedProgress<WorkspaceDiagnosticPartialReport> progress)
    {
        var progressValues = progress.GetValues();
        return new WorkspaceDiagnosticReport
        {
            Items = progressValues != null
            ? progressValues.SelectMany(report => report.Match(r => r.Items, partial => partial.Items)).ToArray()
            : Array.Empty<SumType<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>>(),
        };
    }

    protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(
        WorkspaceDiagnosticParams diagnosticParams, RequestContext context, CancellationToken cancellationToken)
    {
        // Task list items are not reported through the public LSP diagnostic API.
        return WorkspacePullDiagnosticHandler.GetDiagnosticSourcesAsync(context, GlobalOptions, cancellationToken);
    }

    protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(WorkspaceDiagnosticParams diagnosticsParams)
    {
        return diagnosticsParams.PreviousResultId.Select(id => new PreviousPullResult
        {
            PreviousResultId = id.Value,
            TextDocument = new TextDocumentIdentifier
            {
                Uri = id.Uri
            }
        }).ToImmutableArray();
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

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor
    {
        private readonly PublicWorkspacePullDiagnosticsHandler _handler;

        public TestAccessor(PublicWorkspacePullDiagnosticsHandler handler)
        {
            _handler = handler;
        }

        public void TriggerConnectionClose() => _handler.UpdateLspChanged();
    }
}
