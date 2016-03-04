// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    // TODO: implement correct events and etc.
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        private readonly int _correlationId;

        public DiagnosticIncrementalAnalyzer(
            DiagnosticAnalyzerService owner,
            int correlationId,
            Workspace workspace,
            HostAnalyzerManager hostAnalyzerManager,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
            : base(owner, workspace, hostAnalyzerManager, hostDiagnosticUpdateSource)
        {
            _correlationId = correlationId;
        }

        private static bool AnalysisEnabled(Document document)
        {
            // change it to check active file (or visible files), not open files if active file tracking is enabled.
            // otherwise, use open file.
            return document.IsOpen();
        }

        private bool FullAnalysisEnabled(Workspace workspace, string language)
        {
            return workspace.Options.GetOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, language) &&
                   workspace.Options.GetOption(RuntimeOptions.FullSolutionAnalysis);
        }

        private async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsAsync(Project project, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            if (project == null)
            {
                return ImmutableArray<DiagnosticData>.Empty;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var analyzers = HostAnalyzerManager.CreateDiagnosticAnalyzers(project);

            var compilationWithAnalyzer = compilation.WithAnalyzers(analyzers, project.AnalyzerOptions, cancellationToken);

            // REVIEW: this API is a bit strange. 
            //         if getting diagnostic is cancelled, it has to create new compilation and do everything from scratch again?
            var dxs = GetDiagnosticData(project, await compilationWithAnalyzer.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false)).ToImmutableArrayOrEmpty();

            return dxs;
        }

        private IEnumerable<DiagnosticData> GetDiagnosticData(Project project, ImmutableArray<Diagnostic> diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Location == Location.None)
                {
                    yield return DiagnosticData.Create(project, diagnostic);
                    continue;
                }

                var document = project.GetDocument(diagnostic.Location.SourceTree);
                if (document == null)
                {
                    continue;
                }

                yield return DiagnosticData.Create(document, diagnostic);
            }
        }

        private void RaiseEvents(Project project, ImmutableArray<DiagnosticData> diagnostics)
        {
            var groups = diagnostics.GroupBy(d => d.DocumentId);

            var solution = project.Solution;
            var workspace = solution.Workspace;

            foreach (var kv in groups)
            {
                if (kv.Key == null)
                {
                    Owner.RaiseDiagnosticsUpdated(
                        this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                            ValueTuple.Create(this, project.Id), workspace, solution, project.Id, null, kv.ToImmutableArrayOrEmpty()));
                    continue;
                }

                Owner.RaiseDiagnosticsUpdated(
                    this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        ValueTuple.Create(this, kv.Key), workspace, solution, project.Id, kv.Key, kv.ToImmutableArrayOrEmpty()));
            }
        }

        public override bool ContainsDiagnostics(Workspace workspace, ProjectId projectId)
        {
            // for now, it always return false;
            return false;
        }
    }
}
