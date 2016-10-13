// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes.AddImport;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.AddImport
{
    [ExportWorkspaceService(typeof(IAddImportUndoService), ServiceLayer.Host), Shared]
    internal partial class VisualStudioAddImportUndoService : IAddImportUndoService
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IAddImportUndoService _defaultService = new DefaultAddImportUndoService();
        private readonly VisualStudioWorkspaceImpl _workspace;

        [ImportingConstructor]
        public VisualStudioAddImportUndoService(
            VisualStudioWorkspaceImpl workspace,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
        {
            _workspace = workspace;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
        }

        public bool TryAddMetadataReference(
            Workspace workspace, DocumentId contextDocumentId,
            ProjectId fromProjectId, PortableExecutableReference toReference,
            CancellationToken cancellationToken)
        {
            var undoManager = _editorAdaptersFactoryService.TryGetUndoManager(
                workspace, contextDocumentId, cancellationToken);
            return TryAddMetadataReferenceAndAddUndoAction(workspace, contextDocumentId, fromProjectId, 
                toReference.FilePath, undoManager, cancellationToken);
        }

        private bool TryAddMetadataReferenceAndAddUndoAction(
            Workspace workspace, DocumentId contextDocumentId, 
            ProjectId fromProjectId, string filePath,
            IOleUndoManager undoManager, 
            CancellationToken cancellationToken)
        {
            PortableExecutableReference peReference;
            try
            {
                peReference = MetadataReference.CreateFromFile(filePath);
            }
            catch (IOException)
            {
                return false;
            }

            var referenceAdded = _defaultService.TryAddMetadataReference(
                workspace, contextDocumentId, fromProjectId,
                peReference, cancellationToken);

            if (referenceAdded && workspace == _workspace)
            {
                undoManager?.Add(new RemoveMetadataReferenceUndoUnit(
                    this, contextDocumentId, fromProjectId, filePath));
            }

            return referenceAdded;
        }

        private bool TryRemoveMetadataReferenceAndAddUndoUnit(
            DocumentId contextDocumentId, ProjectId fromProjectId, 
            string filePath, IOleUndoManager undoManager)
        {
            var project = _workspace.CurrentSolution.GetProject(fromProjectId);
            if (project != null)
            {
                var existingReference = project.MetadataReferences
                    .OfType<PortableExecutableReference>()
                    .FirstOrDefault(m => StringComparer.OrdinalIgnoreCase.Equals(m.FilePath, filePath));

                if (existingReference != null)
                {
                    var newProject = project.RemoveMetadataReference(existingReference);
                    var removed = _workspace.TryApplyChanges(newProject.Solution);
                    if (removed)
                    {
                        undoManager.Add(new AddMetadataReferenceUndoUnit(
                            this, contextDocumentId, fromProjectId, filePath));
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryAddProjectReference(
            Workspace workspace, DocumentId contextDocumentId,
            ProjectId fromProjectId, ProjectId toProjectId, CancellationToken cancellationToken)
        {
            var undoManager = _editorAdaptersFactoryService.TryGetUndoManager(
                workspace, contextDocumentId, cancellationToken);
            return TryAddProjectReferenceAndAddUndoUnit(workspace, contextDocumentId,
                fromProjectId, toProjectId, undoManager, cancellationToken);
        }

        public bool TryAddProjectReferenceAndAddUndoUnit(
            Workspace workspace, DocumentId contextDocumentId, 
            ProjectId fromProjectId, ProjectId toProjectId, 
            IOleUndoManager undoManager, CancellationToken cancellationToken)
        {
            var referenceAdded = _defaultService.TryAddProjectReference(
                workspace, contextDocumentId, fromProjectId,
                toProjectId, cancellationToken);
            
            if (referenceAdded && workspace == _workspace)
            {
                var toProject = workspace.CurrentSolution.GetProject(toProjectId);
                undoManager?.Add(new RemoveProjectReferenceUndoUnit(
                    this, contextDocumentId, fromProjectId, toProjectId, toProject.Name));
            }

            return referenceAdded;
        }

        private bool TryRemoveProjectReferenceAndAddUndoUnit(
            DocumentId contextDocumentId, ProjectId fromProjectId,
            ProjectId toProjectId, IOleUndoManager undoManager)
        {
            var project = _workspace.CurrentSolution.GetProject(fromProjectId);
            var toProject = _workspace.CurrentSolution.GetProject(toProjectId);
            if (project != null && toProject != null)
            {
                var newProject = project.RemoveProjectReference(new ProjectReference(toProjectId));
                var removed = _workspace.TryApplyChanges(newProject.Solution);
                if (removed)
                {
                    undoManager.Add(new AddProjectReferenceUndoUnit(this, contextDocumentId, fromProjectId, toProjectId, toProject.Name));
                    return true;
                }
            }

            return false;
        }
    }
}