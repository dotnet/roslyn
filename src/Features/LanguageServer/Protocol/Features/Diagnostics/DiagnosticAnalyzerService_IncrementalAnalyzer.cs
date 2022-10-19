// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
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
            => _map.GetValue(workspace, _createIncrementalAnalyzer);

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
}
