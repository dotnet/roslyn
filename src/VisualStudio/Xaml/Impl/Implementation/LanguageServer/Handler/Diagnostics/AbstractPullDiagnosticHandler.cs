// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Diagnostics;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Handler.Diagnostics;

/// <summary>
/// Root type for both document and workspace diagnostic pull requests.
/// </summary>
internal abstract class AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport> : ILspServiceRequestHandler<TDiagnosticsParams, TReport[]?>
    where TReport : VSInternalDiagnosticReport
{
    private readonly IXamlPullDiagnosticService _xamlDiagnosticService;

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    /// <summary>
    /// Gets the progress object to stream results to.
    /// </summary>
    protected abstract IProgress<TReport[]>? GetProgress(TDiagnosticsParams diagnosticsParams);

    /// <summary>
    /// Retrieve the previous results we reported.
    /// </summary>
    protected abstract VSInternalDiagnosticParams[]? GetPreviousResults(TDiagnosticsParams diagnosticsParams);

    /// <summary>
    /// Returns all the documents that should be processed.
    /// </summary>
    protected abstract ImmutableArray<Document> GetDocuments(RequestContext context);

    /// <summary>
    /// Creates the <see cref="VSInternalDiagnosticReport"/> instance we'll report back to clients to let them know our
    /// progress. 
    /// </summary>
    protected abstract TReport CreateReport(TextDocumentIdentifier? identifier, VSDiagnostic[]? diagnostics, string? resultId);

    protected AbstractPullDiagnosticHandler(IXamlPullDiagnosticService xamlDiagnosticService)
    {
        _xamlDiagnosticService = xamlDiagnosticService;
    }

    public async Task<TReport[]?> HandleRequestAsync(TDiagnosticsParams diagnosticsParams, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        using var progress = BufferedProgress.Create(GetProgress(diagnosticsParams));

        // Get the set of results the request said were previously reported.
        var previousResults = GetPreviousResults(diagnosticsParams);

        var documentToPreviousResultId = new Dictionary<Document, string?>();
        if (previousResults != null)
        {
            // Go through the previousResults and check if we need to remove diagnostic information for any documents
            foreach (var previousResult in previousResults)
            {
                if (previousResult.TextDocument != null)
                {
                    var document = await context.Solution.GetDocumentAsync(previousResult.TextDocument, cancellationToken).ConfigureAwait(false);
                    if (document == null)
                    {
                        // We can no longer get this document, return null for both diagnostics and resultId
                        progress.Report(CreateReport(previousResult.TextDocument, diagnostics: null, resultId: null));
                    }
                    else
                    {
                        // Cache the document to previousResultId mapping so we can easily retrieve the resultId later.
                        documentToPreviousResultId[document] = previousResult.PreviousResultId;
                    }
                }
            }
        }

        // Go through the documents that we need to process and call XamlPullDiagnosticService to get the diagnostic report
        foreach (var document in GetDocuments(context))
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var documentId = ProtocolConversions.DocumentToTextDocumentIdentifier(document);

            // If we can get a previousId of the document, use it, 
            // otherwise use null as the previousId to pass into the XamlPullDiagnosticService
            var previousResultId = documentToPreviousResultId.TryGetValue(document, out var id) ? id : null;

            // Call XamlPullDiagnosticService to get the diagnostic report for this document.
            // We will compute what to report inside XamlPullDiagnosticService, for example, whether we should keep using the previousId or use a new resultId,
            // and the handler here just return the result get from XamlPullDiagnosticService.
            var diagnosticReport = await _xamlDiagnosticService.GetDiagnosticReportAsync(document, previousResultId, cancellationToken).ConfigureAwait(false);
            progress.Report(CreateReport(
                        documentId,
                        ConvertToVSDiagnostics(diagnosticReport.Diagnostics, document, text),
                        diagnosticReport.ResultId));
        }

        return progress.GetFlattenedValues();
    }

    /// <summary>
    /// Convert XamlDiagnostics to VSDiagnostics
    /// </summary>
    private static VSDiagnostic[]? ConvertToVSDiagnostics(ImmutableArray<XamlDiagnostic>? xamlDiagnostics, Document document, SourceText text)
    {
        if (xamlDiagnostics == null)
        {
            return null;
        }

        var project = document.Project;
        return [.. xamlDiagnostics.Value.Select(d => new VSDiagnostic()
        {
            Code = d.Code,
            Message = d.Message ?? string.Empty,
            ExpandedMessage = d.ExtendedMessage,
            Severity = ConvertDiagnosticSeverity(d.Severity),
            Range = ProtocolConversions.TextSpanToRange(new TextSpan(d.Offset, d.Length), text),
            Tags = ConvertTags(d),
            Source = d.Tool,
            CodeDescription = ProtocolConversions.HelpLinkToCodeDescription(d.GetHelpLinkUri()),
            Projects =
            [
                new VSDiagnosticProjectInformation
                {
                    ProjectIdentifier = project.Id.Id.ToString(),
                    ProjectName = project.Name,
                },
            ],
        })];
    }

    private static LSP.DiagnosticSeverity ConvertDiagnosticSeverity(XamlDiagnosticSeverity severity)
        => severity switch
        {
            // Hidden is translated in ConvertTags to pass along appropriate _ms tags
            // that will hide the item in a client that knows about those tags.
            XamlDiagnosticSeverity.Hidden => LSP.DiagnosticSeverity.Hint,
            XamlDiagnosticSeverity.HintedSuggestion => LSP.DiagnosticSeverity.Hint,
            XamlDiagnosticSeverity.Message => LSP.DiagnosticSeverity.Information,
            XamlDiagnosticSeverity.Warning => LSP.DiagnosticSeverity.Warning,
            XamlDiagnosticSeverity.Error => LSP.DiagnosticSeverity.Error,
            _ => throw ExceptionUtilities.UnexpectedValue(severity),
        };

    /// <summary>
    /// If you make change in this method, please also update the corresponding file in
    /// src\\LanguageServer\Protocol\Extensions\ProtocolConversions.Diagnostics.cs
    /// </summary>
    private static DiagnosticTag[] ConvertTags(XamlDiagnostic diagnostic)
    {
        using var _ = ArrayBuilder<DiagnosticTag>.GetInstance(out var result);

        result.Add(VSDiagnosticTags.IntellisenseError);

        if (diagnostic.Severity == XamlDiagnosticSeverity.Hidden)
        {
            result.Add(VSDiagnosticTags.HiddenInEditor);
            result.Add(VSDiagnosticTags.HiddenInErrorList);
            result.Add(VSDiagnosticTags.SuppressEditorToolTip);
        }
        else if (diagnostic.Severity == XamlDiagnosticSeverity.HintedSuggestion)
        {
            result.Add(VSDiagnosticTags.HiddenInErrorList);
        }
        else
        {
            result.Add(VSDiagnosticTags.VisibleInErrorList);
        }

        if (diagnostic.CustomTags?.Contains(WellKnownDiagnosticTags.Unnecessary) == true)
            result.Add(DiagnosticTag.Unnecessary);

        return result.ToArray();
    }
}
