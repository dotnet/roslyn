// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

[ExportDiagnosticSourceProvider, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DocumentDiagnosticSourceProvider(
    [Import] IGlobalOptionService globalOptions,
    [Import] IDiagnosticAnalyzerService diagnosticAnalyzerService)
    : IDiagnosticSourceProvider
{
    private static readonly ImmutableArray<string> sourceNames =
    [
        PullDiagnosticCategories.Task,
        PullDiagnosticCategories.DocumentCompilerSyntax,
        PullDiagnosticCategories.DocumentCompilerSemantic,
        PullDiagnosticCategories.DocumentAnalyzerSyntax,
        PullDiagnosticCategories.DocumentAnalyzerSemantic
    ];

    public bool IsDocument => true;
    public ImmutableArray<string> SourceNames => sourceNames;

    public ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, string sourceName, CancellationToken cancellationToken)
    {
        if (sourceName == PullDiagnosticCategories.Task)
            return new(GetDiagnosticSources(diagnosticAnalyzerService, diagnosticKind: default, nonLocalDocumentDiagnostics: false, taskList: true, context, globalOptions));

        if (sourceName == PullDiagnosticCategories.EditAndContinue)
        {
            if (GetEditAndContinueDiagnosticSource(context) is IDiagnosticSource source)
            {
                return new([source]);
            }

            return new([]);
        }

        var diagnosticKind = sourceName switch
        {
            PullDiagnosticCategories.DocumentCompilerSyntax => DiagnosticKind.CompilerSyntax,
            PullDiagnosticCategories.DocumentCompilerSemantic => DiagnosticKind.CompilerSemantic,
            PullDiagnosticCategories.DocumentAnalyzerSyntax => DiagnosticKind.AnalyzerSyntax,
            PullDiagnosticCategories.DocumentAnalyzerSemantic => DiagnosticKind.AnalyzerSemantic,
            //// if this request doesn't have a category at all (legacy behavior, assume they're asking about everything).
            //null => DiagnosticKind.All, // !!!VS Code does not request any diag kind!!!
            //                            // if it's a category we don't recognize, return nothing.
            _ => (DiagnosticKind?)null,
        };

        if (diagnosticKind is null)
            return new([]);

        return new(GetDiagnosticSources(diagnosticAnalyzerService, diagnosticKind.Value, nonLocalDocumentDiagnostics: false, taskList: false, context, globalOptions));
    }

    internal static IDiagnosticSource? GetEditAndContinueDiagnosticSource(RequestContext context)
        => context.GetTrackedDocument<Document>() is { } document ? EditAndContinueDiagnosticSource.CreateOpenDocumentSource(document) : null;

    internal static ImmutableArray<IDiagnosticSource> GetDiagnosticSources(
            IDiagnosticAnalyzerService diagnosticAnalyzerService, DiagnosticKind diagnosticKind, bool nonLocalDocumentDiagnostics, bool taskList, RequestContext context, IGlobalOptionService globalOptions)
    {
        // For the single document case, that is the only doc we want to process.
        //
        // Note: context.Document may be null in the case where the client is asking about a document that we have
        // since removed from the workspace.  In this case, we don't really have anything to process.
        // GetPreviousResults will be used to properly realize this and notify the client that the doc is gone.
        //
        // Only consider open documents here (and only closed ones in the WorkspacePullDiagnosticHandler).  Each
        // handler treats those as separate worlds that they are responsible for.
        var textDocument = context.TextDocument;
        if (textDocument is null)
        {
            context.TraceInformation("Ignoring diagnostics request because no text document was provided");
            return [];
        }

        var document = textDocument as Document;
        if (taskList && document is null)
        {
            context.TraceInformation("Ignoring task list diagnostics request because no document was provided");
            return [];
        }

        if (!context.IsTracking(textDocument.GetURI()))
        {
            context.TraceWarning($"Ignoring diagnostics request for untracked document: {textDocument.GetURI()}");
            return [];
        }

        if (nonLocalDocumentDiagnostics)
            return GetNonLocalDiagnosticSources();

        return taskList
            ? [new TaskListDiagnosticSource(document!, globalOptions)]
            : [new DocumentDiagnosticSource(diagnosticAnalyzerService, diagnosticKind, textDocument)/*, Xaml source might go here; ???why doc while it should we workspace???*/];

        ImmutableArray<IDiagnosticSource> GetNonLocalDiagnosticSources()
        {
            Debug.Assert(!taskList);

            // This code path is currently only invoked from the public LSP handler, which always uses 'DiagnosticKind.All'
            Debug.Assert(diagnosticKind == DiagnosticKind.All);

            // Non-local document diagnostics are reported only when full solution analysis is enabled for analyzer execution.
            if (globalOptions.GetBackgroundAnalysisScope(textDocument.Project.Language) != BackgroundAnalysisScope.FullSolution)
                return [];

            return [new NonLocalDocumentDiagnosticSource(textDocument, diagnosticAnalyzerService, ShouldIncludeAnalyzer)];

            // NOTE: Compiler does not report any non-local diagnostics, so we bail out for compiler analyzer.
            bool ShouldIncludeAnalyzer(DiagnosticAnalyzer analyzer) => !analyzer.IsCompilerAnalyzer();
        }
    }
}
