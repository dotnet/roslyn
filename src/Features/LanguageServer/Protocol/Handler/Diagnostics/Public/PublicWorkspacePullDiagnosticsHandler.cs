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

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;

// A document diagnostic partial report is defined as having the first literal send = WorkspaceDiagnosticReport followed
// by n WorkspaceDiagnosticReportPartialResult literals.
// See https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspace_diagnostic
using WorkspaceDiagnosticPartialReport = SumType<WorkspaceDiagnosticReport, WorkspaceDiagnosticReportPartialResult>;

[Method(Methods.WorkspaceDiagnosticName)]
internal class PublicWorkspacePullDiagnosticsHandler : AbstractPullDiagnosticHandler<WorkspaceDiagnosticParams, WorkspaceDiagnosticPartialReport, WorkspaceDiagnosticReport?>
{
    public PublicWorkspacePullDiagnosticsHandler(
        IDiagnosticAnalyzerService analyzerService,
        EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource,
        IGlobalOptionService globalOptions)
        : base(analyzerService, editAndContinueDiagnosticUpdateSource, globalOptions)
    {
    }

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

    protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
        => WorkspacePullDiagnosticHandler.GetDiagnosticSourcesAsync(context, GlobalOptions, cancellationToken);

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
}
