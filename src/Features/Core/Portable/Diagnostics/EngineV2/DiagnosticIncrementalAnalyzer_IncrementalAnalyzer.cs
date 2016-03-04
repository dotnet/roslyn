// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    // TODO: make it to use cache
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        public async override Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            if (!AnalysisEnabled(document))
            {
                return;
            }

            // TODO: make active file state to cache compilationWithAnalyzer
            // REVIEW: this is really wierd that we need compilation for syntax diagnostics which basically defeat any reason
            //       we have syntax diagnostics.
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            // TODO: make it to use state manager
            var analyzers = HostAnalyzerManager.CreateDiagnosticAnalyzers(document.Project);

            // Create driver that holds onto compilation and associated analyzers
            // TODO: use CompilationWithAnalyzerOption instead of AnalyzerOption so that we can have exception filter and etc
            var analyzerDriver = compilation.WithAnalyzers(analyzers, document.Project.AnalyzerOptions, cancellationToken);

            foreach (var analyzer in analyzers)
            {
                // TODO: implement perf optimization not to run analyzers that are not needed.
                // REVIEW: more unnecessary allocations just to get diagnostics per analyzer
                var oneAnalyzers = ImmutableArray.Create(analyzer);

                // TODO: use cache for perf optimization
                var diagnostics = await analyzerDriver.GetAnalyzerSyntaxDiagnosticsAsync(tree, oneAnalyzers, cancellationToken).ConfigureAwait(false);

                // we only care about local diagnostics
                var diagnosticData = GetDiagnosticData(document.Project, diagnostics).Where(d => d.DocumentId == document.Id);

                // TODO: update using right arguments
                Owner.RaiseDiagnosticsUpdated(
                    this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        ValueTuple.Create(this, "Syntax", document.Id), document.Project.Solution.Workspace, document.Project.Solution, document.Project.Id, document.Id, diagnosticData.ToImmutableArrayOrEmpty()));
            }
        }

        public override async Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
        {
            if (!AnalysisEnabled(document))
            {
                return;
            }

            // TODO: make active file state to cache compilationWithAnalyzer
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // TODO: make it to use state manager
            var analyzers = HostAnalyzerManager.CreateDiagnosticAnalyzers(document.Project);

            // Create driver that holds onto compilation and associated analyzers
            // TODO: use CompilationWithAnalyzerOption instead of AnalyzerOption so that we can have exception filter and etc
            var analyzerDriver = compilation.WithAnalyzers(analyzers, document.Project.AnalyzerOptions, cancellationToken);

            var noSpanFilter = default(TextSpan?);
            foreach (var analyzer in analyzers)
            {
                // REVIEW: more unnecessary allocations just to get diagnostics per analyzer
                var oneAnalyzers = ImmutableArray.Create(analyzer);

                // TODO: use cache for perf optimization
                // REVIEW: I think we don't even need member tracking optimization
                var diagnostics = await analyzerDriver.GetAnalyzerSemanticDiagnosticsAsync(model, noSpanFilter, oneAnalyzers, cancellationToken).ConfigureAwait(false);

                var diagnosticData = GetDiagnosticData(document.Project, diagnostics).Where(d => d.DocumentId == document.Id);

                // TODO: update using right arguments
                Owner.RaiseDiagnosticsUpdated(
                    this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                        ValueTuple.Create(this, "Semantic", document.Id), document.Project.Solution.Workspace, document.Project.Solution, document.Project.Id, document.Id, diagnosticData.ToImmutableArrayOrEmpty()));
            }
        }

        public override Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
        {
            // TODO: at this event, if file being closed is active file (the one in ActiveFileState), we should put that data into
            //       ProjectState
            return SpecializedTasks.EmptyTask;
        }

        public override Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            // REVIEW: this should reset both active file and project state the document belong to.
            return SpecializedTasks.EmptyTask;
        }

        public override void RemoveDocument(DocumentId documentId)
        {
            // TODO: do proper eventing
            Owner.RaiseDiagnosticsUpdated(this, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                ValueTuple.Create(this, "Syntax", documentId), Workspace, null, null, null));

            Owner.RaiseDiagnosticsUpdated(this, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                ValueTuple.Create(this, "Semantic", documentId), Workspace, null, null, null));

            Owner.RaiseDiagnosticsUpdated(this, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                ValueTuple.Create(this, documentId), Workspace, null, null, null));
        }

        public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
        {
            if (!FullAnalysisEnabled(project.Solution.Workspace, project.Language))
            {
                // TODO: check whether there is existing state, if so, raise events to remove them all.
                return;
            }

            // TODO: make this to use cache.
            // TODO: use CompilerDiagnosticExecutor
            var diagnostics = await GetDiagnosticsAsync(project.Solution, project.Id, null, includeSuppressedDiagnostics: true, cancellationToken: cancellationToken).ConfigureAwait(false);

            // TODO: do proper event
            RaiseEvents(project, diagnostics);
        }

        public override void RemoveProject(ProjectId projectId)
        {
            // TODO: do proper event
            Owner.RaiseDiagnosticsUpdated(this, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                ValueTuple.Create(this, projectId), Workspace, null, null, null));
        }

        public override Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            // Review: I think we don't need to care about it
            return SpecializedTasks.EmptyTask;
        }

        public override Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
        {
            // we don't use this one.
            return SpecializedTasks.EmptyTask;
        }
    }
}
