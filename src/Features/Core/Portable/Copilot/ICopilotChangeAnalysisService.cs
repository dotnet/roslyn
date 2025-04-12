// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.Copilot;

/// <param name="DiagnosticKind">What diagnostic kind this is analysis data for.</param>
/// <param name="DiagnosticComputationTime">How long it took to produce the diagnostics for this diagnostic kind.</param>
/// <param name="IdToCount">Mapping from <see cref="Diagnostic.Id"/> to the number of diagnostics produced for that id.</param>
/// <param name="CategoryToCount">Mapping from <see cref="Diagnostic.Category"/> to the number of diagnostics produced for that category.</param>
/// <param name="SeverityToCount">Mapping from <see cref="Diagnostic.Severity"/> to the number of diagnostics produced for that severity.</param>
[DataContract]
internal readonly record struct CopilotDiagnosticAnalysis(
    [property: DataMember(Order = 0)] DiagnosticKind DiagnosticKind,
    [property: DataMember(Order = 1)] TimeSpan DiagnosticComputationTime,
    [property: DataMember(Order = 2)] Dictionary<string, int> IdToCount,
    [property: DataMember(Order = 3)] Dictionary<string, int> CategoryToCount,
    [property: DataMember(Order = 4)] Dictionary<DiagnosticSeverity, int> SeverityToCount);

/// <param name="CodeFixComputationTime">Total time to compute code fixes for the changed regions.</param>
/// <param name="DiagnosticIdToCount">Mapping from diagnostic id to to how many diagnostics with that id had fixes.</param>
/// <param name="DiagnosticIdToApplicationTime">Mapping from diagnostic id to the total time taken to fix diagnostics with that id.</param>
/// <param name="DiagnosticIdToProviderName">Mapping from diagnostic id to the name of the provider that provided the fix.</param>
/// <param name="ProviderNameToApplicationTime">Mapping from provider name to the total time taken to fix diagnostics with that provider.</param>
[DataContract]
internal readonly record struct CopilotCodeFixAnalysis(
    [property: DataMember(Order = 0)] TimeSpan CodeFixComputationTime,
    [property: DataMember(Order = 1)] Dictionary<string, int> DiagnosticIdToCount,
    [property: DataMember(Order = 2)] Dictionary<string, TimeSpan> DiagnosticIdToApplicationTime,
    [property: DataMember(Order = 4)] Dictionary<string, HashSet<string>> DiagnosticIdToProviderName,
    [property: DataMember(Order = 5)] Dictionary<string, TimeSpan> ProviderNameToApplicationTime);

internal readonly record struct CopilotChangeAnalysis(
    TimeSpan TotalAnalysisTime,
    TimeSpan TotalDiagnosticComputationTime,
    TimeSpan TotalCodeFixComputationTime,
    TimeSpan TotalCodeFixApplicationTime,
    ImmutableArray<CopilotDiagnosticAnalysis> DiagnosticAnalyses,
    CopilotCodeFixAnalysis CodeFixAnalysis);

internal interface ICopilotChangeAnalysisService : IWorkspaceService
{
    /// <summary>
    /// Kicks of work to analyze a change that copilot suggested making to a document. <paramref name="document"/> is
    /// the state of the document prior to the edits, and <paramref name="changes"/> are the changes Copilot wants to
    /// make to it.  <paramref name="changes"/> must be sorted and normalized before calling this.
    /// </summary>
    Task AnalyzeChangeAsync(Document document, ImmutableArray<TextChange> changes, CancellationToken cancellationToken);
}

[ExportWorkspaceServiceFactory(typeof(ICopilotChangeAnalysisService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultCopilotChangeAnalysisServiceFactory(
    ICodeFixService codeFixService,
    IDiagnosticAnalyzerService diagnosticAnalyzerService) : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new DefaultCopilotChangeAnalysisService(codeFixService, diagnosticAnalyzerService, workspaceServices);

    private sealed class DefaultCopilotChangeAnalysisService(
        ICodeFixService codeFixService,
        IDiagnosticAnalyzerService diagnosticAnalyzerService,
        HostWorkspaceServices workspaceServices) : ICopilotChangeAnalysisService
    {
        private readonly ICodeFixService _codeFixService = codeFixService;
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService = diagnosticAnalyzerService;
        private readonly HostWorkspaceServices _workspaceServices = workspaceServices;

        public async Task AnalyzeChangeAsync(
            Document document,
            ImmutableArray<TextChange> changes,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(!changes.IsSorted(static (c1, c2) => c1.Span.Start - c2.Span.Start), "'changes' was not sorted.");
            Contract.ThrowIfTrue(new NormalizedTextSpanCollection(changes.Select(c => c.Span)).Count != changes.Length, "'changes' was not normalized.");
            Contract.ThrowIfTrue(document.Project.Solution.Workspace != _workspaceServices.Workspace);

            var client = await RemoteHostClient.TryGetClientAsync(
                _workspaceServices.Workspace, cancellationToken).ConfigureAwait(false);

            if (client != null)
            {
                await client.TryInvokeAsync<IRemoteCopilotChangeAnalysisService>(
                    // Don't need to sync the entire solution over.  Just the cone of projects this document it contained within.
                    document.Project,
                    (service, checksum, cancellationToken) => service.AnalyzeChangeAsync(checksum, document.Id, changes, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await AnalyzeChangeInCurrentProcessAsync(document, changes, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task AnalyzeChangeInCurrentProcessAsync(
            Document document,
            ImmutableArray<TextChange> changes,
            CancellationToken cancellationToken)
        {
            var oldText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = oldText.WithChanges(changes);

            var newDocument = document.WithText(newText);

            var totalDelta = 0;
            using var _ = ArrayBuilder<TextSpan>.GetInstance(out var newSpans);

            foreach (var change in changes)
            {
                var newSpan = new TextSpan(change.Span.Start + totalDelta, change.Span.Length);
                newSpans.Add(newSpan);
                totalDelta += change.NewText!.Length - change.Span.Length;
            }

            await Task.WhenAll(
                AnalyzeChangeAsync(DiagnosticKind.CompilerSyntax),
                AnalyzeChangeAsync(DiagnosticKind.CompilerSemantic),
                AnalyzeChangeAsync(DiagnosticKind.AnalyzerSyntax),
                AnalyzeChangeAsync(DiagnosticKind.AnalyzerSemantic)).ConfigureAwait(false);
            return;

            Task AnalyzeChangeAsync(DiagnosticKind diagnosticKind)
                => Task.WhenAll(AnalyzeChangedRegionsAsync(diagnosticKind), AnalyzeUnchangedRegionsAsync(diagnosticKind));

            async Task AnalyzeChangedRegionsAsync(DiagnosticKind diagnosticKind)
            {
                var diagnostics = await ProducerConsumer<DiagnosticData>.RunParallelAsync(
                    newSpans,
                    static async (span, callback, args, cancellationToken) =>
                    {
                        var (@this, newDocument, diagnosticKind) = args;
                        var diagnostics = await @this._diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(
                            newDocument, span, diagnosticKind, cancellationToken).ConfigureAwait(false);
                        foreach (var diagnostic in diagnostics)
                        {
                            // Ignore supressed diagnostics.  These are things the user has said they do not care about
                            // and would then have no interest in being auto fixed.
                            if (!diagnostic.IsSuppressed)
                                callback(diagnostic);
                        }
                    },
                    args: (@this: this, newDocument, diagnosticKind),
                    cancellationToken).ConfigureAwait(false);

                var codeFixCollections = await ProducerConsumer<CodeFixCollection>.RunParallelAsync(
                    newSpans,
                    static async (span, callback, args, cancellationToken) =>
                    {
                        var (@this, newDocument) = args;
                        await foreach (var codeFixCollection in @this._codeFixService.StreamFixesAsync(
                            newDocument, span, cancellationToken).ConfigureAwait(false))
                        {
                            // Ignore the suppress/configure codefixes that are almost always present.
                            // We would not ever want to apply those to a copilot change.
                            if (codeFixCollection.Provider is not IConfigurationFixProvider &&
                                codeFixCollection.Fixes is [var codeFix, ..])
                            {
                                callback(codeFixCollection);
                            }
                        }
                    },
                    args: (@this: this, newDocument),
                    cancellationToken).ConfigureAwait(false);

                var results = await ProducerConsumer<VoidResult>.RunParallelAsync(
                    codeFixCollections,
                    static async (codeFixCollection, callback, args, cancellationToken) =>
                    {
                        var (@this, solution) = args;
                        var firstAction = GetFirstAction(codeFixCollection.Fixes[0]);
                        var result = await firstAction.GetPreviewOperationsAsync(solution, cancellationToken).ConfigureAwait(false);
                    },
                    args: (@this: this, document.Project.Solution),
                    cancellationToken).ConfigureAwait(false);

                Logger.Log(FunctionId.Copilot_AnalyzeChange, KeyValueLogMessage.Create(LogType.Trace, static (message, args) =>
                {
                    var (diagnosticKind, diagnostics) = args;
                    message["DiagnosticKind"] = diagnosticKind.ToString();

                    foreach (var group in diagnostics.GroupBy(d => d.Id))
                        message[$"Id_{group.Key}"] = group.Count();

                    foreach (var group in diagnostics.GroupBy(d => d.Category))
                        message[$"{diagnosticKind}_{group.Key}"] = group.Count();

                    foreach (var group in diagnostics.GroupBy(d => d.Severity))
                        message[$"{diagnosticKind}_{group.Key}"] = group.Count();
                }, (diagnosticKind, diagnostics)));
            }

            static CodeAction GetFirstAction(CodeFix codeFix)
            {
                var action = codeFix.Action;
                while (action is { NestedCodeActions: [var nestedAction, ..] })
                    action = nestedAction;

                return action;
            }

            // Not yet implemented.  Flesh this out if we think there is value in looking at the rest of the document
            // (or just the regions around the copilot edits) to see how the actual edits impacted them.
            Task AnalyzeUnchangedRegionsAsync(DiagnosticKind diagnosticKind)
                => Task.CompletedTask;
        }
    }
}
