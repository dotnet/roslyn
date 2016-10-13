// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal interface IAddImportUndoService : IWorkspaceService
    {
        bool TryAddMetadataReference(
            Workspace workspace, DocumentId contextDocumentId, 
            ProjectId fromProjectId, PortableExecutableReference toReference,
            CancellationToken cancellationToken);

        bool TryAddProjectReference(
            Workspace workspace, DocumentId contextDocumentId, 
            ProjectId fromProjectId, ProjectId toProjectId,
            CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(IAddImportUndoService)), Shared]
    internal class DefaultAddImportUndoService : IAddImportUndoService
    {
        public bool TryAddMetadataReference(
            Workspace workspace, DocumentId contextDocumentId,
            ProjectId fromProjectId, PortableExecutableReference reference,
            CancellationToken cancellationToken)
        {
            var project = workspace.CurrentSolution.GetProject(fromProjectId);
            var newProject = project.AddMetadataReference(reference);

            return workspace.TryApplyChanges(newProject.Solution);
        }

        public bool TryAddProjectReference(
            Workspace workspace, DocumentId contextDocumentId,
            ProjectId fromProjectId, ProjectId toProjectId,
            CancellationToken cancellationToken)
        {
            var fromProject = workspace.CurrentSolution.GetProject(fromProjectId);
            var newProject = fromProject.AddProjectReference(new ProjectReference(toProjectId));

            return workspace.TryApplyChanges(newProject.Solution);
        }
    }
}