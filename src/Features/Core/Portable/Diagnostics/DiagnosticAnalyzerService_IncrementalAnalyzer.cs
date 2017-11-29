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
            // DiagnosticAnalyzerService implements IDiagnosticUpdateSource and register itself to IDiagnosticService
            // and IDiagnosticService will hold onto some data from this source to do book keeping.
            // this will tell IDiagnosticService to drop any information related to the workspace from this
            // otherwise, IDiagnosticService can hold onto some information which causes memory leaks.
            //
            // to preserve order of events, we use event queue to serialize events. we use explicit queue for it
            // rather than using UI thread and its message queue to archieve same thing implicitly.
            var asyncToken = Listener.BeginAsyncOperation(nameof(ShutdownAnalyzerFrom));
            _eventQueue.ScheduleTask(() =>
            {
                // workspace such as previewWorkspace, which can come and go on the fly, will use this to remove data saved
                // in IDiagnosticService. but since diagnostic changed events are something anyone can listen to, there might
                // be other listener who hold onto its event args reported by DiagnosticChanged event. for those, its
                // the listener's responsibility to clean thsoe up when workspace goes away.
                _registrationService.Shutdown(this, workspace);
            }).CompletesAsyncOperation(asyncToken);
        }

        private DiagnosticIncrementalAnalyzer CreateIncrementalAnalyzerCallback(Workspace workspace)
        {
            return new DiagnosticIncrementalAnalyzer(this, LogAggregator.GetNextId(), workspace, _hostAnalyzerManager, _hostDiagnosticUpdateSource);
        }
    }
}
