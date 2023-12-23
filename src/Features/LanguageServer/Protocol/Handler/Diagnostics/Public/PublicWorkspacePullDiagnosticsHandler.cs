// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;

// A document diagnostic partial report is defined as having the first literal send = WorkspaceDiagnosticReport followed
// by n WorkspaceDiagnosticReportPartialResult literals.
// See https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspace_diagnostic
using WorkspaceDiagnosticPartialReport = SumType<WorkspaceDiagnosticReport, WorkspaceDiagnosticReportPartialResult>;

[Method(Methods.WorkspaceDiagnosticName)]
internal sealed class PublicWorkspacePullDiagnosticsHandler(
    LspWorkspaceManager workspaceManager,
    LspWorkspaceRegistrationService registrationService,
    IDiagnosticAnalyzerService analyzerService,
    IDiagnosticsRefresher diagnosticRefresher,
    IGlobalOptionService globalOptions)
    : AbstractWorkspacePullDiagnosticsHandler<WorkspaceDiagnosticParams, WorkspaceDiagnosticPartialReport, WorkspaceDiagnosticReport?>(
        workspaceManager, registrationService, analyzerService, diagnosticRefresher, globalOptions), IDisposable
{

    /// <summary>
    /// Public API doesn't support categories (yet).
    /// </summary>
    protected override string? GetDiagnosticCategory(WorkspaceDiagnosticParams diagnosticsParams)
        => null;

    protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData, bool isLiveSource)
        => ConvertTags(diagnosticData, isLiveSource, potentialDuplicate: false);

    protected override WorkspaceDiagnosticPartialReport CreateReport(TextDocumentIdentifier identifier, Roslyn.LanguageServer.Protocol.Diagnostic[] diagnostics, string resultId)
        => new(new WorkspaceDiagnosticReport
        {
            Items =
            [
                new WorkspaceFullDocumentDiagnosticReport
                {
                    Uri = identifier.Uri,
                    Items = diagnostics,
                    // The documents provided by workspace reports are never open, so we return null.
                    Version = null,
                    ResultId = resultId
                }
            ]
        });

    protected override WorkspaceDiagnosticPartialReport CreateRemovedReport(TextDocumentIdentifier identifier)
        => new(new WorkspaceDiagnosticReport
        {
            Items =
            [
                new WorkspaceFullDocumentDiagnosticReport
                {
                    Uri = identifier.Uri,
                    Items = Array.Empty<Roslyn.LanguageServer.Protocol.Diagnostic>(),
                    // The documents provided by workspace reports are never open, so we return null.
                    Version = null,
                    ResultId = null,
                }
            ]
        });

    protected override WorkspaceDiagnosticPartialReport CreateUnchangedReport(TextDocumentIdentifier identifier, string resultId)
        => new(new WorkspaceDiagnosticReport
        {
            Items =
            [
                new WorkspaceUnchangedDocumentDiagnosticReport
                {
                    Uri = identifier.Uri,
                    // The documents provided by workspace reports are never open, so we return null.
                    Version = null,
                    ResultId = resultId,
                }
            ]
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

    internal override TestAccessor GetTestAccessor() => new(this);
}
