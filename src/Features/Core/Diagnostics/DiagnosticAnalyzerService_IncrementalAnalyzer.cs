// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [ExportIncrementalAnalyzerProvider(highPriorityForActiveFile: true, workspaceKinds: new string[] { WorkspaceKind.Host, WorkspaceKind.Interactive })]
    internal partial class DiagnosticAnalyzerService : IIncrementalAnalyzerProvider
    {
        private readonly ConditionalWeakTable<Workspace, BaseDiagnosticIncrementalAnalyzer> _map;
        private readonly ConditionalWeakTable<Workspace, BaseDiagnosticIncrementalAnalyzer>.CreateValueCallback _createIncrementalAnalyzer;

        private DiagnosticAnalyzerService()
        {
            _map = new ConditionalWeakTable<Workspace, BaseDiagnosticIncrementalAnalyzer>();
            _createIncrementalAnalyzer = CreateIncrementalAnalyzerCallback;
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            var optionService = workspace.Services.GetService<IOptionService>();

            if (!optionService.GetOption(ServiceComponentOnOffOptions.DiagnosticProvider))
            {
                return null;
            }

            return GetOrCreateIncrementalAnalyzer(workspace);
        }

        private BaseDiagnosticIncrementalAnalyzer GetOrCreateIncrementalAnalyzer(Workspace workspace)
        {
            return _map.GetValue(workspace, _createIncrementalAnalyzer);
        }

        private BaseDiagnosticIncrementalAnalyzer CreateIncrementalAnalyzerCallback(Workspace workspace)
        {
            // subscribe to active context changed event for new workspace
            workspace.DocumentActiveContextChanged += OnDocumentActiveContextChanged;
            return new IncrementalAnalyzerDelegatee(this, workspace, _hostAnalyzerManager, _hostDiagnosticUpdateSource);
        }

        private void OnDocumentActiveContextChanged(object sender, DocumentEventArgs e)
        {
            Reanalyze(e.Document.Project.Solution.Workspace, documentIds: SpecializedCollections.SingletonEnumerable(e.Document.Id));
        }

        // internal for testing
        internal class IncrementalAnalyzerDelegatee : BaseDiagnosticIncrementalAnalyzer
        {
            private readonly HostAnalyzerManager _hostAnalyzerManager;
            private readonly DiagnosticAnalyzerService _owner;

            // v1 diagnostic engine
            private readonly EngineV1.DiagnosticIncrementalAnalyzer _engineV1;

            // v2 diagnostic engine - for now v1
            private readonly EngineV2.DiagnosticIncrementalAnalyzer _engineV2;

            public IncrementalAnalyzerDelegatee(DiagnosticAnalyzerService owner, Workspace workspace, HostAnalyzerManager hostAnalyzerManager, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
                : base(workspace, hostDiagnosticUpdateSource)
            {
                _hostAnalyzerManager = hostAnalyzerManager;
                _owner = owner;

                var v1CorrelationId = LogAggregator.GetNextId();
                _engineV1 = new EngineV1.DiagnosticIncrementalAnalyzer(_owner, v1CorrelationId, workspace, _hostAnalyzerManager, hostDiagnosticUpdateSource);

                var v2CorrelationId = LogAggregator.GetNextId();
                _engineV2 = new EngineV2.DiagnosticIncrementalAnalyzer(_owner, v2CorrelationId, workspace, _hostAnalyzerManager, hostDiagnosticUpdateSource);
            }

            #region IIncrementalAnalyzer
            public override Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
            {
                return Analyzer.AnalyzeSyntaxAsync(document, cancellationToken);
            }

            public override Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
            {
                return Analyzer.AnalyzeDocumentAsync(document, bodyOpt, cancellationToken);
            }

            public override Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
            {
                return Analyzer.AnalyzeProjectAsync(project, semanticsChanged, cancellationToken);
            }

            public override Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            {
                return Analyzer.DocumentOpenAsync(document, cancellationToken);
            }

            public override Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                return Analyzer.DocumentResetAsync(document, cancellationToken);
            }

            public override Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                return Analyzer.NewSolutionSnapshotAsync(solution, cancellationToken);
            }

            public override void RemoveDocument(DocumentId documentId)
            {
                Analyzer.RemoveDocument(documentId);
            }

            public override void RemoveProject(ProjectId projectId)
            {
                Analyzer.RemoveProject(projectId);
            }

            public override bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                return e.Option.Feature == SimplificationOptions.PerLanguageFeatureName ||
                       e.Option.Feature == SimplificationOptions.NonPerLanguageFeatureName ||
                       e.Option == ServiceFeatureOnOffOptions.ClosedFileDiagnostic ||
                       e.Option == InternalDiagnosticsOptions.UseDiagnosticEngineV2 ||
                       Analyzer.NeedsReanalysisOnOptionChanged(sender, e);
            }
            #endregion

            #region delegating methods from diagnostic analyzer service to each implementation of the engine
            public override Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Analyzer.GetCachedDiagnosticsAsync(solution, projectId, documentId, cancellationToken);
            }

            public override Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken)
            {
                return Analyzer.GetSpecificCachedDiagnosticsAsync(solution, id, cancellationToken);
            }

            public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Analyzer.GetDiagnosticsAsync(solution, projectId, documentId, cancellationToken);
            }

            public override Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, CancellationToken cancellationToken)
            {
                return Analyzer.GetSpecificDiagnosticsAsync(solution, id, cancellationToken);
            }

            public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, ImmutableHashSet<string> diagnosticIds = null, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Analyzer.GetDiagnosticsForIdsAsync(solution, projectId, documentId, diagnosticIds, cancellationToken);
            }

            public override Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, ImmutableHashSet<string> diagnosticIds = null, CancellationToken cancellationToken = default(CancellationToken))
            {
                return Analyzer.GetProjectDiagnosticsForIdsAsync(solution, projectId, diagnosticIds, cancellationToken);
            }

            public override Task<bool> TryAppendDiagnosticsForSpanAsync(Document document, TextSpan range, List<DiagnosticData> diagnostics, CancellationToken cancellationToken)
            {
                return Analyzer.TryAppendDiagnosticsForSpanAsync(document, range, diagnostics, cancellationToken);
            }

            public override Task<IEnumerable<DiagnosticData>> GetDiagnosticsForSpanAsync(Document document, TextSpan range, CancellationToken cancellationToken)
            {
                return Analyzer.GetDiagnosticsForSpanAsync(document, range, cancellationToken);
            }
            #endregion

            public void TurnOff(bool useV2)
            {
                var turnedOffAnalyzer = GetAnalyzer(!useV2);

                foreach (var project in Workspace.CurrentSolution.Projects)
                {
                    foreach (var document in project.Documents)
                    {
                        turnedOffAnalyzer.RemoveDocument(document.Id);
                    }

                    turnedOffAnalyzer.RemoveProject(project.Id);
                }
            }

            // internal for testing
            internal BaseDiagnosticIncrementalAnalyzer Analyzer
            {
                get
                {
                    var option = Workspace.Options.GetOption(InternalDiagnosticsOptions.UseDiagnosticEngineV2);
                    return GetAnalyzer(option);
                }
            }

            private BaseDiagnosticIncrementalAnalyzer GetAnalyzer(bool useV2)
            {
                return useV2 ? (BaseDiagnosticIncrementalAnalyzer)_engineV2 : _engineV1;
            }
        }
    }
}
