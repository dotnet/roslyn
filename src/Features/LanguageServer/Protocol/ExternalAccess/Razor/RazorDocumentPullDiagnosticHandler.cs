// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

internal class RazorDocumentPullDiagnosticHandler(
    IDiagnosticAnalyzerService diagnosticAnalyzerService,
    IDiagnosticsRefresher diagnosticRefresher,
    IGlobalOptionService globalOptions,
    LspWorkspaceManager workspaceManager)
        : AbstractDocumentPullDiagnosticHandler<RazorDiagnosticsParams, VSInternalDiagnosticReport[], VSInternalDiagnosticReport[]>(diagnosticAnalyzerService, diagnosticRefresher, globalOptions)
        , ILspServiceRequestHandler<RazorDiagnosticsParams, VSInternalDiagnosticReport[]?>
{
    public const string RazorDiagnosticsName = "razor/diagnostics";
    private readonly LspWorkspaceManager _workspaceManager = workspaceManager;

    protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData, bool isLiveSource)
        => ConvertTags(diagnosticData, isLiveSource, potentialDuplicate: false);

    protected override VSInternalDiagnosticReport[] CreateReport(TextDocumentIdentifier identifier, VisualStudio.LanguageServer.Protocol.Diagnostic[]? diagnostics, string? resultId)
         => [
                new VSInternalDiagnosticReport
                {
                    Diagnostics = diagnostics,
                    ResultId = resultId,
                    Identifier = DocumentDiagnosticIdentifier,
                    // Mark these diagnostics as superseding any diagnostics for the same document from the
                    // WorkspacePullDiagnosticHandler. We are always getting completely accurate and up to date diagnostic
                    // values for a particular file, so our results should always be preferred over the workspace-pull
                    // values which are cached and may be out of date.
                    Supersedes = WorkspaceDiagnosticIdentifier,
                }
            ];

    protected override VSInternalDiagnosticReport[] CreateRemovedReport(TextDocumentIdentifier identifier)
        => CreateReport(identifier, diagnostics: null, resultId: null);

    protected override VSInternalDiagnosticReport[] CreateUnchangedReport(TextDocumentIdentifier identifier, string resultId)
        => CreateReport(identifier, diagnostics: null, resultId);

    protected override string? GetDiagnosticCategory(RazorDiagnosticsParams diagnosticsParams)
        => PullDiagnosticCategories.Task;

    protected override async ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(RazorDiagnosticsParams diagnosticsParams, RequestContext context, CancellationToken cancellationToken)
    {
        TextDocument? razorDocument = null;
        if (diagnosticsParams.RazorTextDocument is not null)
        {
            var (_, _, textDocument) = await _workspaceManager.GetLspDocumentInfoAsync(diagnosticsParams.RazorTextDocument, cancellationToken).ConfigureAwait(false);
            razorDocument = textDocument;
        }

        var csharpSources = DocumentPullDiagnosticHandler.GetDiagnosticSources(DiagnosticKind.All, nonLocalDocumentDiagnostics: false, taskList: false, context, GlobalOptions);
        var razorSource = razorDocument is null
            ? null
            : new NonLocalDocumentDiagnosticSource(razorDocument, static _ => true);

        if (razorSource is null)
        {
            return csharpSources;
        }

        return csharpSources.Add(razorSource);
    }

    protected override VSInternalDiagnosticReport[]? CreateReturn(BufferedProgress<VSInternalDiagnosticReport[]> progress)
    {
        return progress.GetFlattenedValues();
    }

    public override TextDocumentIdentifier? GetTextDocumentIdentifier(RazorDiagnosticsParams diagnosticsParams)
        => diagnosticsParams.TextDocument;

    protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(RazorDiagnosticsParams diagnosticsParams)
    {
        if (diagnosticsParams.PreviousResultId != null && diagnosticsParams.TextDocument != null)
        {
            return ImmutableArray.Create(new PreviousPullResult(diagnosticsParams.PreviousResultId, diagnosticsParams.TextDocument));
        }

        // The client didn't provide us with a previous result to look for, so we can't lookup anything.
        return null;
    }
}

[DataContract]
internal class RazorDiagnosticsParams : IPartialResultParams<VSInternalDiagnosticReport[]>
{
    [DataMember(Name = "partialResultToken", IsRequired = false)]
    public IProgress<VSInternalDiagnosticReport[]>? PartialResultToken { get; set; }

    [DataMember(Name = "previousResultId")]
    public string? PreviousResultId { get; set; }

    [DataMember(Name = "_razor_textDocument", IsRequired = true)]
    public TextDocumentIdentifier? RazorTextDocument { get; set; }

    [DataMember(Name = "_vs_textDocument", IsRequired = true)]
    public TextDocumentIdentifier? TextDocument { get; set; }
}
