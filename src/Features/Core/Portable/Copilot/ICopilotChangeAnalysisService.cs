// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

namespace Microsoft.CodeAnalysis.Copilot;

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
                            callback(diagnostic);
                    },
                    args: (@this: this, newDocument, diagnosticKind),
                    cancellationToken).ConfigureAwait(false);

                var codeFixCollections = await ProducerConsumer<DiagnosticData>.RunAsync(
                    newSpans,
                    static async (span, callback, args, cancellationToken) =>
                    {
                        var (@this, newDocument) = args;
                        await foreach (var codeFixCollection in @this._codeFixService.StreamFixesAsync(
                            newDocument, span, callback, CancellationToken.None).ConfigureAwait(false))
                        {
                            foreach (var codeFix in codeFixCollection)
                                callback(codeFix);
                        }
                    },
                    args: (@this: this, newDocument),
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

            // Not yet implemented.  Flesh this out if we think there is value in looking at the rest of the document
            // (or just the regions around the copilot edits) to see how the actual edits impacted them.
            Task AnalyzeUnchangedRegionsAsync(DiagnosticKind diagnosticKind)
                => Task.CompletedTask;
        }
    }
}
