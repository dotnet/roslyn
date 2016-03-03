// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        public override Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
        {
            var diagnostics = await GetDiagnosticsAsync(project.Solution, project.Id, null, includeSuppressedDiagnostics: true, cancellationToken: cancellationToken).ConfigureAwait(false);

            RaiseEvents(project, diagnostics);
        }

        public override Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyTask;
        }

        public override void RemoveDocument(DocumentId documentId)
        {
            Owner.RaiseDiagnosticsUpdated(this, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                ValueTuple.Create(this, documentId), Workspace, null, null, null));
        }

        public override void RemoveProject(ProjectId projectId)
        {
            Owner.RaiseDiagnosticsUpdated(this, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                ValueTuple.Create(this, projectId), Workspace, null, null, null));
        }
    }
}
