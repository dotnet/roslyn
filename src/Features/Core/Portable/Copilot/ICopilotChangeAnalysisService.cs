// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Copilot;

internal interface ICopilotChangeAnalysisService : IWorkspaceService
{
    /// <summary>
    /// Kicks of work to analyze a change that copilot suggested making to a document. <paramref name="document"/> is
    /// the state of the document prior to the edits, and <paramref name="changes"/> are the changes Copilot wants to
    /// make to it.  <paramref name="changes"/> must be sorted and normalized before calling this.
    /// </summary>
    Task<CopilotChangeAnalysis> AnalyzeChangeAsync(
        Document document, ImmutableArray<TextChange> changes, CancellationToken cancellationToken);
}

[ExportWorkspaceService(typeof(ICopilotChangeAnalysisService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultCopilotChangeAnalysisService(
    ICodeFixService codeFixService) : ICopilotChangeAnalysisService
{
    private const string RoslynPrefix = "Microsoft.CodeAnalysis.";

    private readonly ICodeFixService _codeFixService = codeFixService;

    public async Task<CopilotChangeAnalysis> AnalyzeChangeAsync(
        Document document,
        ImmutableArray<TextChange> changes,
        CancellationToken cancellationToken)
    {
        if (!document.SupportsSemanticModel)
            return default;

        Contract.ThrowIfTrue(!changes.IsSorted(static (c1, c2) => c1.Span.Start - c2.Span.Start), "'changes' was not sorted.");
        Contract.ThrowIfTrue(new NormalizedTextSpanCollection(changes.Select(c => c.Span)).Count != changes.Length, "'changes' was not normalized.");

        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);

        if (client != null)
        {
            var value = await client.TryInvokeAsync<IRemoteCopilotChangeAnalysisService, CopilotChangeAnalysis>(
                // Don't need to sync the entire solution over.  Just the cone of projects this document it contained within.
                document.Project,
                (service, checksum, cancellationToken) => service.AnalyzeChangeAsync(
                    checksum, document.Id, changes, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            return value.HasValue ? value.Value : default;
        }
        else
        {
            return await AnalyzeChangeInCurrentProcessAsync(
                document, changes, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<CopilotChangeAnalysis> AnalyzeChangeInCurrentProcessAsync(
        Document document,
        ImmutableArray<TextChange> changes,
        CancellationToken cancellationToken)
    {
        // Keep track of how long our analysis takes entirely.
        var totalAnalysisTimeStopWatch = SharedStopwatch.StartNew();

        var forkingTimeStopWatch = SharedStopwatch.StartNew();

        // Fork the starting document with the changes copilot wants to make.  Keep track of where the edited spans
        // move to in the forked doucment, as that is what we will want to analyze.
        var oldText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var newText = oldText.WithChanges(changes);

        var newDocument = document.WithText(newText);

        // Get the semantic model and keep it alive so none of the work we do causes it to be dropped.
        var semanticModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var forkingTime = forkingTimeStopWatch.Elapsed;

        var totalDelta = 0;
        using var _ = ArrayBuilder<TextSpan>.GetInstance(out var newSpans);

        foreach (var change in changes)
        {
            var newTextLength = change.NewText!.Length;

            newSpans.Add(new TextSpan(change.Span.Start + totalDelta, newTextLength));
            totalDelta += newTextLength - change.Span.Length;
        }

        // First, determine the diagnostics produced in the edits that copilot makes.  Done non-concurrently with
        // ComputeCodeFixAnalysisAsync as we want good data on just how long it takes to even compute the varying
        // types of diagnostics, without contending with the code fix analysis.
        var totalDiagnosticComputationTimeStopWatch = SharedStopwatch.StartNew();
        var diagnosticAnalyses = await ComputeAllDiagnosticAnalysesAsync(
            newDocument, newSpans, cancellationToken).ConfigureAwait(false);
        var totalDiagnosticComputationTime = totalDiagnosticComputationTimeStopWatch.Elapsed;

        // After computing diagnostics, do another analysis pass to see if we would have been able to fixup any of
        // the code copilot produced.
        var codeFixAnalysis = await ComputeCodeFixAnalysisAsync(
            newDocument, newSpans, cancellationToken).ConfigureAwait(false);

        var totalAnalysisTime = totalAnalysisTimeStopWatch.Elapsed;

        var sourceGeneratedDocuments = await newDocument.Project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);

        var projectDocumentCount = newDocument.Project.DocumentIds.Count;
        var projectSourceGeneratedDocumentCount = sourceGeneratedDocuments.Count();
        var projectConeCount = 1 + document.Project.Solution
            .GetProjectDependencyGraph()
            .GetProjectsThatThisProjectTransitivelyDependsOn(document.Project.Id).Count;

        GC.KeepAlive(semanticModel);

        return new CopilotChangeAnalysis(
            Succeeded: true,
            OldDocumentLength: oldText.Length,
            NewDocumentLength: newText.Length,
            TextChangeDelta: totalDelta,
            ProjectDocumentCount: projectDocumentCount,
            ProjectSourceGeneratedDocumentCount: projectSourceGeneratedDocumentCount,
            ProjectConeCount: projectConeCount,
            TotalAnalysisTime: totalAnalysisTime,
            ForkingTime: forkingTime,
            TotalDiagnosticComputationTime: totalDiagnosticComputationTime,
            diagnosticAnalyses,
            codeFixAnalysis);
    }

    private static CodeAction GetFirstAction(CodeFix codeFix)
    {
        var action = codeFix.Action;
        while (action is { NestedCodeActions: [var nestedAction, ..] })
            action = nestedAction;

        return action;
    }

    private static void IncrementCount<TKey>(Dictionary<TKey, int> map, TKey key) where TKey : notnull
    {
        map.TryGetValue(key, out var idCount);
        map[key] = idCount + 1;
    }

    private static void IncrementElapsedTime<TKey>(Dictionary<TKey, TimeSpan> map, TKey key, TimeSpan elapsed) where TKey : notnull
    {
        map.TryGetValue(key, out var currentElapsed);
        map[key] = currentElapsed + elapsed;
    }

    private static Task<ImmutableArray<CopilotDiagnosticAnalysis>> ComputeAllDiagnosticAnalysesAsync(
        Document newDocument,
        ArrayBuilder<TextSpan> newSpans,
        CancellationToken cancellationToken)
    {
        // Compute the data in parallel for each diagnostic kind.
        return ProducerConsumer<CopilotDiagnosticAnalysis>.RunParallelAsync(
            [DiagnosticKind.CompilerSyntax, DiagnosticKind.CompilerSemantic, DiagnosticKind.AnalyzerSyntax, DiagnosticKind.AnalyzerSemantic],
            static async (diagnosticKind, callback, args, cancellationToken) =>
            {
                var (newDocument, newSpans) = args;

                var computationTime = SharedStopwatch.StartNew();

                // Compute the diagnostics.
                var diagnostics = await ComputeDiagnosticsAsync(
                    newDocument, newSpans, diagnosticKind, cancellationToken).ConfigureAwait(false);

                // Collect the data to report as telemetry.
                var idToCount = new Dictionary<string, int>();
                var categoryToCount = new Dictionary<string, int>();
                var severityToCount = new Dictionary<DiagnosticSeverity, int>();

                foreach (var diagnostic in diagnostics)
                {
                    IncrementCount(idToCount, diagnostic.Id);
                    IncrementCount(categoryToCount, diagnostic.Category);
                    IncrementCount(severityToCount, diagnostic.Severity);
                }

                callback(new CopilotDiagnosticAnalysis(
                    diagnosticKind,
                    computationTime.Elapsed,
                    idToCount,
                    categoryToCount,
                    severityToCount));
            },
            args: (newDocument, newSpans),
            cancellationToken);

        static Task<ImmutableArray<DiagnosticData>> ComputeDiagnosticsAsync(
           Document newDocument,
           ArrayBuilder<TextSpan> newSpans,
           DiagnosticKind diagnosticKind,
           CancellationToken cancellationToken)
        {
            // Get diagnostics in parallel for all edited spans, for the desired diagnostic kind.
            return ProducerConsumer<DiagnosticData>.RunParallelAsync(
                newSpans,
                static async (span, callback, args, cancellationToken) =>
                {
                    var (newDocument, diagnosticKind) = args;
                    var diagnosticAnalyzerService = newDocument.Project.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                    var diagnostics = await diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(
                        newDocument, span, diagnosticKind, cancellationToken).ConfigureAwait(false);
                    foreach (var diagnostic in diagnostics)
                    {
                        // Ignore supressed and hidden diagnostics.  These are things the user has said they do not
                        // care about and would then have no interest in being auto fixed.
                        if (IsVisibleDiagnostic(diagnostic.IsSuppressed, diagnostic.Severity))
                            callback(diagnostic);
                    }
                },
                args: (newDocument, diagnosticKind),
                cancellationToken);
        }
    }

    private static bool IsVisibleDiagnostic(bool isSuppressed, DiagnosticSeverity severity)
        => !isSuppressed && severity != DiagnosticSeverity.Hidden;

    private async Task<CopilotCodeFixAnalysis> ComputeCodeFixAnalysisAsync(
        Document newDocument,
        ArrayBuilder<TextSpan> newSpans,
        CancellationToken cancellationToken)
    {
        // Determine how long it would be to even compute code fixes for these changed regions.
        var totalComputationStopWatch = SharedStopwatch.StartNew();
        var codeFixCollections = await ComputeCodeFixCollectionsAsync().ConfigureAwait(false);
        var totalComputationTime = totalComputationStopWatch.Elapsed;

        var diagnosticIdToCount = new Dictionary<string, int>();
        var diagnosticIdToApplicationTime = new Dictionary<string, TimeSpan>();
        var diagnosticIdToProviderName = new Dictionary<string, HashSet<string>>();
        var providerNameToApplicationTime = new Dictionary<string, TimeSpan>();

        var totalApplicationTimeStopWatch = SharedStopwatch.StartNew();
        await ProducerConsumer<(CodeFixCollection collection, TimeSpan elapsedTime)>.RunParallelAsync(
            codeFixCollections,
            produceItems: static async (codeFixCollection, callback, args, cancellationToken) =>
            {
                var (@this, solution, _, _, _, _) = args;
                var firstAction = GetFirstAction(codeFixCollection.Fixes[0]);

                var applicationTimeStopWatch = SharedStopwatch.StartNew();
                var result = await firstAction.GetPreviewOperationsAsync(solution, cancellationToken).ConfigureAwait(false);
                callback((codeFixCollection, applicationTimeStopWatch.Elapsed));
            },
            consumeItems: static async (values, args, cancellationToken) =>
            {
                var (@this, solution, diagnosticIdToCount, diagnosticIdToApplicationTime, diagnosticIdToProviderName, providerNameToApplicationTime) = args;
                await foreach (var (codeFixCollection, applicationTime) in values)
                {
                    var diagnosticId = codeFixCollection.FirstDiagnostic.Id;
                    var providerName = codeFixCollection.Provider.GetType().FullName![RoslynPrefix.Length..];

                    IncrementCount(diagnosticIdToCount, diagnosticId);
                    IncrementElapsedTime(diagnosticIdToApplicationTime, diagnosticId, applicationTime);
                    diagnosticIdToProviderName.MultiAdd(diagnosticId, providerName);
                    IncrementElapsedTime(providerNameToApplicationTime, providerName, applicationTime);
                }
            },
            args: (@this: this, newDocument.Project.Solution, diagnosticIdToCount, diagnosticIdToApplicationTime, diagnosticIdToProviderName, providerNameToApplicationTime),
            cancellationToken).ConfigureAwait(false);
        var totalApplicationTime = totalApplicationTimeStopWatch.Elapsed;

        return new CopilotCodeFixAnalysis(
            totalComputationTime,
            totalApplicationTime,
            diagnosticIdToCount,
            diagnosticIdToApplicationTime,
            diagnosticIdToProviderName,
            providerNameToApplicationTime);

        Task<ImmutableArray<CodeFixCollection>> ComputeCodeFixCollectionsAsync()
        {
            return ProducerConsumer<CodeFixCollection>.RunParallelAsync(
                newSpans,
                static async (span, callback, args, cancellationToken) =>
                {
                    var intervalTree = new TextSpanMutableIntervalTree();

                    var (@this, newDocument) = args;
                    await foreach (var codeFixCollection in @this._codeFixService.StreamFixesAsync(
                        newDocument, span, cancellationToken).ConfigureAwait(false))
                    {
                        // Ignore the suppress/configure codefixes that are almost always present.
                        // We would not ever want to apply those to a copilot change.
                        if (codeFixCollection is
                            {
                                Provider: not IConfigurationFixProvider,
                                Fixes: [var codeFix, ..],
                            } &&
                            IsVisibleDiagnostic(codeFix.PrimaryDiagnostic.IsSuppressed, codeFix.PrimaryDiagnostic.Severity) &&
                            (codeFixCollection.Provider.GetType().Namespace ?? "").StartsWith(RoslynPrefix))
                        {
                            // The first for a particular span is the one we would apply.  Ignore others that fix the same span.
                            if (intervalTree.HasIntervalThatOverlapsWith(codeFixCollection.TextSpan))
                                continue;

                            intervalTree.AddIntervalInPlace(codeFixCollection.TextSpan);
                            callback(codeFixCollection);
                        }
                    }
                },
                args: (@this: this, newDocument),
                cancellationToken);
        }
    }
}
    }
}
#endif
