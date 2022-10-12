// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed class WorkspaceDocumentDiagnosticSource : AbstractDocumentDiagnosticSource<TextDocument>
{
    private readonly IGlobalOptionService _globalOptionService;

    protected override bool IncludeTaskListItems { get; }
    protected override bool IncludeStandardDiagnostics { get; }

    public WorkspaceDocumentDiagnosticSource(
        TextDocument document,
        bool includeTaskListItems,
        bool includeStandardDiagnostics,
        IGlobalOptionService globalOptionService) : base(document)
    {
        Contract.ThrowIfFalse(includeTaskListItems || includeStandardDiagnostics,
            $"At least one of includeTaskListItems={includeTaskListItems} or includeStandardDiagnostics={includeStandardDiagnostics} must be true.");
        IncludeTaskListItems = includeTaskListItems;
        IncludeStandardDiagnostics = includeStandardDiagnostics;
        _globalOptionService = globalOptionService;
    }

    protected override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsWorkerAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        if (Document is SourceGeneratedDocument sourceGeneratedDocument)
        {
            // Unfortunately GetDiagnosticsForIdsAsync returns nothing for source generated documents.
            var documentDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(sourceGeneratedDocument, range: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            return documentDiagnostics;
        }
        else
        {
            if (Document.FilePath?.Contains("Test.txt") == true)
            {
                //Debugger.Launch();
            }
            // We call GetDiagnosticsForIdsAsync as we want to ensure we get the full set of diagnostics for this document
            // including those reported as a compilation end diagnostic.  These are not included in document pull (uses GetDiagnosticsForSpan) due to cost.
            // However we can include them as a part of workspace pull when FSA is on.
            var documentDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForIdsAsync(Document.Project.Solution, Document.Project.Id, Document.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (Document.FilePath?.Contains("Test.txt") == true && documentDiagnostics.IsEmpty)
            {
                context.TraceInformation("What analyzers? " + string.Join(",", Document.Project.Solution.AnalyzerReferences.SelectMany(s => s.GetAnalyzersForAllLanguages()).Select(a => a.GetType().Name)));
                context.TraceInformation("diagnostic mode: " + _globalOptionService.GetDiagnosticMode(InternalDiagnosticsOptions.NormalDiagnosticMode));
                context.TraceInformation("c# background: " + _globalOptionService.GetOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp)));
                context.TraceInformation("vb background: " + _globalOptionService.GetOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic)));
                context.TraceInformation("ts background: " + _globalOptionService.GetOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, InternalLanguageNames.TypeScript)));
                //Debugger.Launch();
            }

            return documentDiagnostics;
        }
    }
}
