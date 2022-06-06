// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [ExportIncrementalAnalyzerProvider(
        highPriorityForActiveFile: true, name: WellKnownSolutionCrawlerAnalyzers.Diagnostic,
        workspaceKinds: new string[] { WorkspaceKind.Host, WorkspaceKind.Interactive })]
    internal partial class DiagnosticAnalyzerService : IIncrementalAnalyzerProvider
    {
        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            if (GlobalOptions.IsPullDiagnostics(InternalDiagnosticsOptions.NormalDiagnosticMode))
            {
                // We rely on LSP to query us for diagnostics when things have changed and poll us for changes that might
                // have happened to the project or closed files outside of VS.
                // However, we still need to create the analyzer so that the map contains the analyzer to run when pull diagnostics asks.
                _ = _map.GetValue(workspace, _createIncrementalAnalyzer);

                return NoOpIncrementalAnalyzer.Instance;
            }

            return _map.GetValue(workspace, _createIncrementalAnalyzer);
        }

        public void ShutdownAnalyzerFrom(Workspace workspace)
        {
            // this should be only called once analyzer associated with the workspace is done.
            if (_map.TryGetValue(workspace, out var analyzer))
            {
                analyzer.Shutdown();
            }
        }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        private DiagnosticIncrementalAnalyzer CreateIncrementalAnalyzerCallback(Workspace workspace)
        {
            // subscribe to active context changed event for new workspace
            workspace.DocumentActiveContextChanged += OnDocumentActiveContextChanged;

            return new DiagnosticIncrementalAnalyzer(this, LogAggregator.GetNextId(), workspace, AnalyzerInfoCache);
        }

        private void OnDocumentActiveContextChanged(object? sender, DocumentActiveContextChangedEventArgs e)
            => Reanalyze(e.Solution.Workspace, documentIds: SpecializedCollections.SingletonEnumerable(e.NewActiveContextDocumentId), highPriority: true);
    }

    internal class NoOpIncrementalAnalyzer : IIncrementalAnalyzer
    {
        public static NoOpIncrementalAnalyzer Instance = new();

        public int Priority => 5;

        public Task ActiveDocumentSwitchedAsync(TextDocument document, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task AnalyzeNonSourceDocumentAsync(TextDocument textDocument, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void LogAnalyzerCountSummary()
        {
        }

        public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
        {
            return false;
        }

        public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NonSourceDocumentCloseAsync(TextDocument textDocument, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NonSourceDocumentOpenAsync(TextDocument textDocument, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task NonSourceDocumentResetAsync(TextDocument textDocument, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Shutdown()
        {
        }
    }
}
