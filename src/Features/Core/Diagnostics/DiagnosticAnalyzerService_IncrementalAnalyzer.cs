// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [ExportIncrementalAnalyzerProvider(highPriorityForActiveFile: true, workspaceKinds: new string[] { WorkspaceKind.Host, WorkspaceKind.Interactive })]
    internal partial class DiagnosticAnalyzerService : IIncrementalAnalyzerProvider
    {
        private readonly ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer> map;
        private readonly ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer>.CreateValueCallback createIncrementalAnalyzer;

        private DiagnosticAnalyzerService()
        {
            this.map = new ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer>();
            this.createIncrementalAnalyzer = CreateIncrementalAnalyzerCallback;
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            var optionService = workspace.Services.GetService<IOptionService>();

            if (!optionService.GetOption(ServiceComponentOnOffOptions.DiagnosticProvider))
            {
                return null;
            }

            return GetOrCreateIncrementalAnalyzerCore(workspace);
        }

        private DiagnosticIncrementalAnalyzer GetOrCreateIncrementalAnalyzerCore(Workspace workspace)
        {
            return this.map.GetValue(workspace, this.createIncrementalAnalyzer);
        }

        private DiagnosticIncrementalAnalyzer CreateIncrementalAnalyzerCallback(Workspace workspace)
        {
            Contract.ThrowIfTrue(this.workspaceAnalyzers.IsDefault);

            // subscribe to active context changed event for new workspace
            workspace.DocumentActiveContextChanged += OnDocumentActiveContextChanged;

            var correlationId = LogAggregator.GetNextId();
            return new DiagnosticIncrementalAnalyzer(this, correlationId, workspace, this.workspaceAnalyzers);
        }

        private void OnDocumentActiveContextChanged(object sender, DocumentEventArgs e)
        {
            Reanalyze(e.Document.Project.Solution.Workspace, documentIds: SpecializedCollections.SingletonEnumerable(e.Document.Id));
        }
    }
}
