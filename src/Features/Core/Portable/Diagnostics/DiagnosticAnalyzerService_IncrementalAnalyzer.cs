// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [ExportIncrementalAnalyzerProvider(
        highPriorityForActiveFile: true, name: WellKnownSolutionCrawlerAnalyzers.Diagnostic,
        workspaceKinds: new string[] { WorkspaceKind.Host, WorkspaceKind.Interactive, WorkspaceKind.AnyCodeRoslynWorkspace })]
    internal partial class DiagnosticAnalyzerService : IIncrementalAnalyzerProvider
    {
        private readonly ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer> _map;
        private readonly ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer>.CreateValueCallback _createIncrementalAnalyzer;

        private DiagnosticAnalyzerService()
        {
            _map = new ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer>();
            _createIncrementalAnalyzer = CreateIncrementalAnalyzerCallback;
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            if (!workspace.Options.GetOption(ServiceComponentOnOffOptions.DiagnosticProvider))
            {
                return null;
            }

            return GetOrCreateIncrementalAnalyzer(workspace);
        }

        private DiagnosticIncrementalAnalyzer GetOrCreateIncrementalAnalyzer(Workspace workspace)
        {
            return _map.GetValue(workspace, _createIncrementalAnalyzer);
        }

        public void ShutdownAnalyzerFrom(Workspace workspace)
        {
            // this should be only called once analyzer associated with the workspace is done.
            // this will let diagnostic service to remove data associated with the workspace from this source
            var asyncToken = Listener.BeginAsyncOperation(nameof(ShutdownAnalyzerFrom));
            _eventQueue.ScheduleTask(() =>
            {
                // workspace such as previewWorkspace, which can come and go on the fly, will use this to remove data saved. 
                // but since this doesn't raise diagnostic removed events, if there are others
                // who hold onto diagnostics reported, it is their responsibility to clean them up when workspace
                // goes away
                _registrationService.Shutdown(this, workspace);
            }).CompletesAsyncOperation(asyncToken);
        }

        private DiagnosticIncrementalAnalyzer CreateIncrementalAnalyzerCallback(Workspace workspace)
        {
            // subscribe to active context changed event for new workspace
            workspace.DocumentActiveContextChanged += OnDocumentActiveContextChanged;

            return new DiagnosticIncrementalAnalyzer(this, LogAggregator.GetNextId(), workspace, _hostAnalyzerManager, _hostDiagnosticUpdateSource);
        }

        private void OnDocumentActiveContextChanged(object sender, DocumentActiveContextChangedEventArgs e)
        {
            Reanalyze(e.Solution.Workspace, documentIds: SpecializedCollections.SingletonEnumerable(e.NewActiveContextDocumentId), highPriority: true);
        }
    }
}
