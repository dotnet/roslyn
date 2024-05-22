// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

[Method(VSInternalMethods.WorkspacePullDiagnosticName)]
internal sealed partial class WorkspacePullDiagnosticHandler(
    LspWorkspaceManager workspaceManager,
    LspWorkspaceRegistrationService registrationService,
    IDiagnosticAnalyzerService analyzerService,
    IDiagnosticSourceManager diagnosticSourceManager,
    IDiagnosticsRefresher diagnosticsRefresher,
    IGlobalOptionService globalOptions)
    : AbstractWorkspacePullDiagnosticsHandler<VSInternalWorkspaceDiagnosticsParams, VSInternalWorkspaceDiagnosticReport[], VSInternalWorkspaceDiagnosticReport[]>(
        workspaceManager, registrationService, analyzerService, diagnosticSourceManager, diagnosticsRefresher, globalOptions)
{
    protected override string? GetRequestDiagnosticCategory(VSInternalWorkspaceDiagnosticsParams diagnosticsParams)
        => diagnosticsParams.QueryingDiagnosticKind?.Value;

    protected override VSInternalWorkspaceDiagnosticReport[] CreateReport(TextDocumentIdentifier identifier, Roslyn.LanguageServer.Protocol.Diagnostic[]? diagnostics, string? resultId)
    => [
        new VSInternalWorkspaceDiagnosticReport
        {
            TextDocument = identifier,
            Diagnostics = diagnostics,
            ResultId = resultId,
            // Mark these diagnostics as having come from us.  They will be superseded by any diagnostics for the
            // same file produced by the DocumentPullDiagnosticHandler.
            Identifier = WorkspaceDiagnosticIdentifier,
        }
    ];

    protected override VSInternalWorkspaceDiagnosticReport[] CreateRemovedReport(TextDocumentIdentifier identifier)
        => CreateReport(identifier, diagnostics: null, resultId: null);

    protected override bool TryCreateUnchangedReport(TextDocumentIdentifier identifier, string resultId, [NotNullWhen(true)] out VSInternalWorkspaceDiagnosticReport[]? report)
    {
        // Skip reporting 'unchanged' document reports for workspace pull diagnostics.  There are often a ton of
        // these and we can save a lot of memory not serializing/deserializing all of this.
        report = null;
        return false;
    }

    protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(VSInternalWorkspaceDiagnosticsParams diagnosticsParams)
        => diagnosticsParams.PreviousResults?.Where(d => d.PreviousResultId != null).Select(d => new PreviousPullResult(d.PreviousResultId!, d.TextDocument!)).ToImmutableArray();

    protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData, bool isLiveSource)
    {
        // All workspace diagnostics are potential duplicates given that they can be overridden by the diagnostics
        // produced by document diagnostics.
        return ConvertTags(diagnosticData, isLiveSource, potentialDuplicate: true);
    }

    protected override VSInternalWorkspaceDiagnosticReport[]? CreateReturn(BufferedProgress<VSInternalWorkspaceDiagnosticReport[]> progress)
    {
        return progress.GetFlattenedValues();
    }

    internal override TestAccessor GetTestAccessor() => new(this);
}
