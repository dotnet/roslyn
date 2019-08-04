// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    [Export(typeof(IPreviewFactoryService)), Shared]
    internal class PreviewFactoryService : ForegroundThreadAffinitizedObject, IPreviewFactoryService
    {
        private const double DefaultZoomLevel = 0.75;
        private readonly ITextViewRoleSet _previewRoleSet;

        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly IContentTypeRegistryService _contentTypeRegistryService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly ITextDifferencingSelectorService _differenceSelectorService;
        private readonly IDifferenceBufferFactoryService _differenceBufferService;
        private readonly IWpfDifferenceViewerFactoryService _differenceViewerService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PreviewFactoryService(
            IThreadingContext threadingContext,
            ITextBufferFactoryService textBufferFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextDifferencingSelectorService differenceSelectorService,
            IDifferenceBufferFactoryService differenceBufferService,
            IWpfDifferenceViewerFactoryService differenceViewerService)
            : base(threadingContext)
        {
            Contract.ThrowIfFalse(ThreadingContext.HasMainThread);

            _textBufferFactoryService = textBufferFactoryService;
            _contentTypeRegistryService = contentTypeRegistryService;
            _projectionBufferFactoryService = projectionBufferFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _differenceSelectorService = differenceSelectorService;
            _differenceBufferService = differenceBufferService;
            _differenceViewerService = differenceViewerService;

            _previewRoleSet = textEditorFactoryService.CreateTextViewRoleSet(
                TextViewRoles.PreviewRole, PredefinedTextViewRoles.Analyzable);
        }

        public SolutionPreviewResult GetSolutionPreviews(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
        {
            return GetSolutionPreviews(oldSolution, newSolution, DefaultZoomLevel, cancellationToken);
        }

        public SolutionPreviewResult GetSolutionPreviews(Solution oldSolution, Solution newSolution, double zoomLevel, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Note: The order in which previews are added to the below list is significant.
            // Preview for a changed document is preferred over preview for changed references and so on.
            var previewItems = new List<SolutionPreviewItem>();
            SolutionChangeSummary changeSummary = null;
            if (newSolution != null)
            {
                var solutionChanges = newSolution.GetChanges(oldSolution);

                foreach (var projectChanges in solutionChanges.GetProjectChanges())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var projectId = projectChanges.ProjectId;
                    var oldProject = projectChanges.OldProject;
                    var newProject = projectChanges.NewProject;

                    foreach (var documentId in projectChanges.GetChangedDocuments())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(documentId.ProjectId, documentId, c =>
                            CreateChangedDocumentPreviewViewAsync(oldSolution.GetDocument(documentId), newSolution.GetDocument(documentId), zoomLevel, c)));
                    }

                    foreach (var documentId in projectChanges.GetAddedDocuments())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(documentId.ProjectId, documentId, c =>
                            CreateAddedDocumentPreviewViewAsync(newSolution.GetDocument(documentId), zoomLevel, c)));
                    }

                    foreach (var documentId in projectChanges.GetRemovedDocuments())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(oldProject.Id, documentId, c =>
                            CreateRemovedDocumentPreviewViewAsync(oldSolution.GetDocument(documentId), zoomLevel, c)));
                    }

                    foreach (var documentId in projectChanges.GetChangedAdditionalDocuments())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(documentId.ProjectId, documentId, c =>
                            CreateChangedAdditionalDocumentPreviewViewAsync(oldSolution.GetAdditionalDocument(documentId), newSolution.GetAdditionalDocument(documentId), zoomLevel, c)));
                    }

                    foreach (var documentId in projectChanges.GetAddedAdditionalDocuments())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(documentId.ProjectId, documentId, c =>
                            CreateAddedAdditionalDocumentPreviewViewAsync(newSolution.GetAdditionalDocument(documentId), zoomLevel, c)));
                    }

                    foreach (var documentId in projectChanges.GetRemovedAdditionalDocuments())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(oldProject.Id, documentId, c =>
                            CreateRemovedAdditionalDocumentPreviewViewAsync(oldSolution.GetAdditionalDocument(documentId), zoomLevel, c)));
                    }

                    foreach (var documentId in projectChanges.GetChangedAnalyzerConfigDocuments())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(documentId.ProjectId, documentId, c =>
                            CreateChangedAnalyzerConfigDocumentPreviewViewAsync(oldSolution.GetAnalyzerConfigDocument(documentId), newSolution.GetAnalyzerConfigDocument(documentId), zoomLevel, c)));
                    }

                    foreach (var documentId in projectChanges.GetAddedAnalyzerConfigDocuments())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(documentId.ProjectId, documentId, c =>
                            CreateAddedAnalyzerConfigDocumentPreviewViewAsync(newSolution.GetAnalyzerConfigDocument(documentId), zoomLevel, c)));
                    }

                    foreach (var documentId in projectChanges.GetRemovedAnalyzerConfigDocuments())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(oldProject.Id, documentId, c =>
                            CreateRemovedAnalyzerConfigDocumentPreviewViewAsync(oldSolution.GetAnalyzerConfigDocument(documentId), zoomLevel, c)));
                    }

                    foreach (var metadataReference in projectChanges.GetAddedMetadataReferences())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(oldProject.Id, null,
                            string.Format(EditorFeaturesResources.Adding_reference_0_to_1, metadataReference.Display, oldProject.Name)));
                    }

                    foreach (var metadataReference in projectChanges.GetRemovedMetadataReferences())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(oldProject.Id, null,
                            string.Format(EditorFeaturesResources.Removing_reference_0_from_1, metadataReference.Display, oldProject.Name)));
                    }

                    foreach (var projectReference in projectChanges.GetAddedProjectReferences())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(oldProject.Id, null,
                            string.Format(EditorFeaturesResources.Adding_reference_0_to_1, newSolution.GetProject(projectReference.ProjectId).Name, oldProject.Name)));
                    }

                    foreach (var projectReference in projectChanges.GetRemovedProjectReferences())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(oldProject.Id, null,
                            string.Format(EditorFeaturesResources.Removing_reference_0_from_1, oldSolution.GetProject(projectReference.ProjectId).Name, oldProject.Name)));
                    }

                    foreach (var analyzer in projectChanges.GetAddedAnalyzerReferences())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(oldProject.Id, null,
                            string.Format(EditorFeaturesResources.Adding_analyzer_reference_0_to_1, analyzer.Display, oldProject.Name)));
                    }

                    foreach (var analyzer in projectChanges.GetRemovedAnalyzerReferences())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        previewItems.Add(new SolutionPreviewItem(oldProject.Id, null,
                            string.Format(EditorFeaturesResources.Removing_analyzer_reference_0_from_1, analyzer.Display, oldProject.Name)));
                    }
                }

                foreach (var project in solutionChanges.GetAddedProjects())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    previewItems.Add(new SolutionPreviewItem(project.Id, null,
                        string.Format(EditorFeaturesResources.Adding_project_0, project.Name)));
                }

                foreach (var project in solutionChanges.GetRemovedProjects())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    previewItems.Add(new SolutionPreviewItem(project.Id, null,
                        string.Format(EditorFeaturesResources.Removing_project_0, project.Name)));
                }

                foreach (var projectChanges in solutionChanges.GetProjectChanges().Where(ProjectReferencesChanged))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    previewItems.Add(new SolutionPreviewItem(projectChanges.OldProject.Id, null,
                        string.Format(EditorFeaturesResources.Changing_project_references_for_0, projectChanges.OldProject.Name)));
                }

                changeSummary = new SolutionChangeSummary(oldSolution, newSolution, solutionChanges);
            }

            return new SolutionPreviewResult(ThreadingContext, previewItems, changeSummary);
        }

        private bool ProjectReferencesChanged(ProjectChanges projectChanges)
        {
            var oldProjectReferences = projectChanges.OldProject.ProjectReferences.ToDictionary(r => r.ProjectId);
            var newProjectReferences = projectChanges.NewProject.ProjectReferences.ToDictionary(r => r.ProjectId);

            // These are the set of project reference that remained in the project. We don't care 
            // about project references that were added or removed.  Those will already be reported.
            var preservedProjectIds = oldProjectReferences.Keys.Intersect(newProjectReferences.Keys);

            foreach (var projectId in preservedProjectIds)
            {
                var oldProjectReference = oldProjectReferences[projectId];
                var newProjectReference = newProjectReferences[projectId];

                if (!oldProjectReference.Equals(newProjectReference))
                {
                    return true;
                }
            }

            return false;
        }

        public Task<object> CreateAddedDocumentPreviewViewAsync(Document document, CancellationToken cancellationToken)
        {
            return CreateAddedDocumentPreviewViewAsync(document, DefaultZoomLevel, cancellationToken);
        }

        private Task<object> CreateAddedDocumentPreviewViewCoreAsync(ITextBuffer newBuffer, PreviewWorkspace workspace, TextDocument document, double zoomLevel, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var firstLine = string.Format(EditorFeaturesResources.Adding_0_to_1_with_content_colon,
                document.Name, document.Project.Name);

            var originalBuffer = _projectionBufferFactoryService.CreatePreviewProjectionBuffer(
                sourceSpans: new List<object> { firstLine, "\r\n" }, registryService: _contentTypeRegistryService);

            var span = new SnapshotSpan(newBuffer.CurrentSnapshot, Span.FromBounds(0, newBuffer.CurrentSnapshot.Length))
                .CreateTrackingSpan(SpanTrackingMode.EdgeExclusive);
            var changedBuffer = _projectionBufferFactoryService.CreatePreviewProjectionBuffer(
                sourceSpans: new List<object> { firstLine, "\r\n", span }, registryService: _contentTypeRegistryService);

            return CreateNewDifferenceViewerAsync(null, workspace, originalBuffer, changedBuffer, zoomLevel, cancellationToken);
        }

        private Task<object> CreateAddedTextDocumentPreviewViewAsync(
            TextDocument document,
            double zoomLevel,
            Func<TextDocument, CancellationToken, ITextBuffer> createBuffer,
            Action<Workspace, DocumentId> openTextDocument,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var newBuffer = createBuffer(document, cancellationToken);

            // Create PreviewWorkspace around the buffer to be displayed in the diff preview
            // so that all IDE services (colorizer, squiggles etc.) light up in this buffer.
            var rightWorkspace = new PreviewWorkspace(
                document.Project.Solution.WithTextDocumentText(document.Id, newBuffer.AsTextContainer().CurrentText));
            openTextDocument(rightWorkspace, document.Id);

            return CreateAddedDocumentPreviewViewCoreAsync(newBuffer, rightWorkspace, document, zoomLevel, cancellationToken);
        }

        public Task<object> CreateAddedDocumentPreviewViewAsync(Document document, double zoomLevel, CancellationToken cancellationToken)
        {
            return CreateAddedTextDocumentPreviewViewAsync(
                document, zoomLevel,
                createBuffer: (textDocument, cancellationToken) => CreateNewBuffer((Document)textDocument, cancellationToken),
                openTextDocument: (workspace, documentId) => workspace.OpenDocument(documentId),
                cancellationToken);
        }

        public Task<object> CreateAddedAdditionalDocumentPreviewViewAsync(TextDocument document, double zoomLevel, CancellationToken cancellationToken)
        {
            return CreateAddedTextDocumentPreviewViewAsync(
                document, zoomLevel,
                createBuffer: CreateNewPlainTextBuffer,
                openTextDocument: (workspace, documentId) => workspace.OpenAdditionalDocument(documentId),
                cancellationToken);
        }

        public Task<object> CreateAddedAnalyzerConfigDocumentPreviewViewAsync(TextDocument document, double zoomLevel, CancellationToken cancellationToken)
        {
            return CreateAddedTextDocumentPreviewViewAsync(
                document, zoomLevel,
                createBuffer: CreateNewPlainTextBuffer,
                openTextDocument: (workspace, documentId) => workspace.OpenAnalyzerConfigDocument(documentId),
                cancellationToken);
        }

        public Task<object> CreateRemovedDocumentPreviewViewAsync(Document document, CancellationToken cancellationToken)
        {
            return CreateRemovedDocumentPreviewViewAsync(document, DefaultZoomLevel, cancellationToken);
        }

        private Task<object> CreateRemovedDocumentPreviewViewCoreAsync(ITextBuffer oldBuffer, PreviewWorkspace workspace, TextDocument document, double zoomLevel, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var firstLine = string.Format(EditorFeaturesResources.Removing_0_from_1_with_content_colon,
                document.Name, document.Project.Name);

            var span = new SnapshotSpan(oldBuffer.CurrentSnapshot, Span.FromBounds(0, oldBuffer.CurrentSnapshot.Length))
                .CreateTrackingSpan(SpanTrackingMode.EdgeExclusive);
            var originalBuffer = _projectionBufferFactoryService.CreatePreviewProjectionBuffer(
                sourceSpans: new List<object> { firstLine, "\r\n", span }, registryService: _contentTypeRegistryService);

            var changedBuffer = _projectionBufferFactoryService.CreatePreviewProjectionBuffer(
                sourceSpans: new List<object> { firstLine, "\r\n" }, registryService: _contentTypeRegistryService);

            return CreateNewDifferenceViewerAsync(workspace, null, originalBuffer, changedBuffer, zoomLevel, cancellationToken);
        }

        private Task<object> CreateRemovedTextDocumentPreviewViewAsync(
            TextDocument document,
            double zoomLevel,
            Func<TextDocument, CancellationToken, ITextBuffer> createBuffer,
            Func<Solution, DocumentId, Solution> removeTextDocument,
            Func<Solution, DocumentId, string, SourceText, Solution> addTextDocument,
            Action<Workspace, DocumentId> openTextDocument,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Note: We don't use the original buffer that is associated with oldDocument
            // (and possibly open in the editor) for oldBuffer below. This is because oldBuffer
            // will be used inside a projection buffer inside our inline diff preview below
            // and platform's implementation currently has a bug where projection buffers
            // are being leaked. This leak means that if we use the original buffer that is
            // currently visible in the editor here, the projection buffer span calculation
            // would be triggered every time user changes some code in this buffer (even though
            // the diff view would long have been dismissed by the time user edits the code)
            // resulting in crashes. Instead we create a new buffer from the same content.
            // TODO: We could use ITextBufferCloneService instead here to clone the original buffer.
            var oldBuffer = createBuffer(document, cancellationToken);

            // Create PreviewWorkspace around the buffer to be displayed in the diff preview
            // so that all IDE services (colorizer, squiggles etc.) light up in this buffer.
            var leftDocumentId = DocumentId.CreateNewId(document.Project.Id);
            var solutionWithRemovedDocument = removeTextDocument(document.Project.Solution, document.Id);
            var leftSolution = addTextDocument(solutionWithRemovedDocument, leftDocumentId, document.Name, oldBuffer.AsTextContainer().CurrentText);
            var leftDocument = leftSolution.GetTextDocument(leftDocumentId);
            var leftWorkspace = new PreviewWorkspace(leftSolution);
            openTextDocument(leftWorkspace, leftDocument.Id);

            return CreateRemovedDocumentPreviewViewCoreAsync(oldBuffer, leftWorkspace, leftDocument, zoomLevel, cancellationToken);
        }

        public Task<object> CreateRemovedDocumentPreviewViewAsync(Document document, double zoomLevel, CancellationToken cancellationToken)
        {
            return CreateRemovedTextDocumentPreviewViewAsync(
                document, zoomLevel,
                createBuffer: (textDocument, cancellationToken) => CreateNewBuffer((Document)textDocument, cancellationToken),
                removeTextDocument: (solution, documentId) => solution.RemoveDocument(documentId),
                addTextDocument: (solution, documentId, name, text) => solution.AddDocument(documentId, name, text),
                openTextDocument: (workspace, documentId) => workspace.OpenDocument(documentId),
                cancellationToken);
        }

        public Task<object> CreateRemovedAdditionalDocumentPreviewViewAsync(TextDocument document, double zoomLevel, CancellationToken cancellationToken)
        {
            return CreateRemovedTextDocumentPreviewViewAsync(
                document, zoomLevel,
                createBuffer: CreateNewPlainTextBuffer,
                removeTextDocument: (solution, documentId) => solution.RemoveAdditionalDocument(documentId),
                addTextDocument: (solution, documentId, name, text) => solution.AddAdditionalDocument(documentId, name, text),
                openTextDocument: (workspace, documentId) => workspace.OpenAdditionalDocument(documentId),
                cancellationToken);
        }

        public Task<object> CreateRemovedAnalyzerConfigDocumentPreviewViewAsync(TextDocument document, double zoomLevel, CancellationToken cancellationToken)
        {
            return CreateRemovedTextDocumentPreviewViewAsync(
                document, zoomLevel,
                createBuffer: CreateNewPlainTextBuffer,
                removeTextDocument: (solution, documentId) => solution.RemoveAnalyzerConfigDocument(documentId),
                addTextDocument: (solution, documentId, name, text) => solution.AddAnalyzerConfigDocument(documentId, name, text),
                openTextDocument: (workspace, documentId) => workspace.OpenAnalyzerConfigDocument(documentId),
                cancellationToken);
        }

        public Task<object> CreateChangedDocumentPreviewViewAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
        {
            return CreateChangedDocumentPreviewViewAsync(oldDocument, newDocument, DefaultZoomLevel, cancellationToken);
        }

        public Task<object> CreateChangedDocumentPreviewViewAsync(Document oldDocument, Document newDocument, double zoomLevel, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Note: We don't use the original buffer that is associated with oldDocument
            // (and currently open in the editor) for oldBuffer below. This is because oldBuffer
            // will be used inside a projection buffer inside our inline diff preview below
            // and platform's implementation currently has a bug where projection buffers
            // are being leaked. This leak means that if we use the original buffer that is
            // currently visible in the editor here, the projection buffer span calculation
            // would be triggered every time user changes some code in this buffer (even though
            // the diff view would long have been dismissed by the time user edits the code)
            // resulting in crashes. Instead we create a new buffer from the same content.
            // TODO: We could use ITextBufferCloneService instead here to clone the original buffer.
            var oldBuffer = CreateNewBuffer(oldDocument, cancellationToken);
            var newBuffer = CreateNewBuffer(newDocument, cancellationToken);

            // Convert the diffs to be line based.  
            // Compute the diffs between the old text and the new.
            var diffResult = ComputeEditDifferences(oldDocument, newDocument, cancellationToken);

            // Need to show the spans in the right that are different.
            // We also need to show the spans that are in conflict.
            var originalSpans = GetOriginalSpans(diffResult, cancellationToken);
            var changedSpans = GetChangedSpans(diffResult, cancellationToken);
            var description = default(string);
            var allSpans = default(NormalizedSpanCollection);

            if (newDocument.SupportsSyntaxTree)
            {
                var newRoot = newDocument.GetSyntaxRootSynchronously(cancellationToken);
                var conflictNodes = newRoot.GetAnnotatedNodesAndTokens(ConflictAnnotation.Kind);
                var conflictSpans = conflictNodes.Select(n => n.Span.ToSpan()).ToList();
                var conflictDescriptions = conflictNodes.SelectMany(n => n.GetAnnotations(ConflictAnnotation.Kind))
                                                        .Select(a => $"❌ {ConflictAnnotation.GetDescription(a)}")
                                                        .Distinct();

                var warningNodes = newRoot.GetAnnotatedNodesAndTokens(WarningAnnotation.Kind);
                var warningSpans = warningNodes.Select(n => n.Span.ToSpan()).ToList();
                var warningDescriptions = warningNodes.SelectMany(n => n.GetAnnotations(WarningAnnotation.Kind))
                                                        .Select(a => $"⚠ {WarningAnnotation.GetDescription(a)}")
                                                        .Distinct();

                var suppressDiagnosticsNodes = newRoot.GetAnnotatedNodesAndTokens(SuppressDiagnosticsAnnotation.Kind);
                var suppressDiagnosticsSpans = suppressDiagnosticsNodes.Select(n => n.Span.ToSpan()).ToList();
                AttachAnnotationsToBuffer(newBuffer, conflictSpans, warningSpans, suppressDiagnosticsSpans);

                description = conflictSpans.Count == 0 && warningSpans.Count == 0
                    ? null
                    : string.Join(Environment.NewLine, conflictDescriptions.Concat(warningDescriptions));
                allSpans = new NormalizedSpanCollection(conflictSpans.Concat(warningSpans).Concat(changedSpans));
            }
            else
            {
                allSpans = new NormalizedSpanCollection(changedSpans);
            }

            var originalLineSpans = CreateLineSpans(oldBuffer.CurrentSnapshot, originalSpans, cancellationToken);
            var changedLineSpans = CreateLineSpans(newBuffer.CurrentSnapshot, allSpans, cancellationToken);
            if (!originalLineSpans.Any())
            {
                // This means that we have no differences (likely because of conflicts).
                // In such cases, use the same spans for the left (old) buffer as the right (new) buffer.
                originalLineSpans = changedLineSpans;
            }

            // Create PreviewWorkspaces around the buffers to be displayed on the left and right
            // so that all IDE services (colorizer, squiggles etc.) light up in these buffers.
            var leftDocument = oldDocument.Project
                .RemoveDocument(oldDocument.Id)
                .AddDocument(oldDocument.Name, oldBuffer.AsTextContainer().CurrentText, oldDocument.Folders, oldDocument.FilePath);
            var leftWorkspace = new PreviewWorkspace(leftDocument.Project.Solution);
            leftWorkspace.OpenDocument(leftDocument.Id);

            var rightWorkspace = new PreviewWorkspace(
                newDocument.WithText(newBuffer.AsTextContainer().CurrentText).Project.Solution);
            rightWorkspace.OpenDocument(newDocument.Id);

            return CreateChangedDocumentViewAsync(
                oldBuffer, newBuffer, description, originalLineSpans, changedLineSpans,
                leftWorkspace, rightWorkspace, zoomLevel, cancellationToken);
        }

        // NOTE: We are only sharing this code between additional documents and analyzer config documents,
        // which are essentially plain text documents. Regular source documents need special handling
        // and hence have a different implementation.
        private Task<object> CreateChangedAdditionalOrAnalyzerConfigDocumentPreviewViewAsync(
            TextDocument oldDocument,
            TextDocument newDocument,
            double zoomLevel,
            Func<Solution, DocumentId, Solution> removeTextDocument,
            Func<Solution, DocumentId, string, SourceText, Solution> addTextDocument,
            Func<Solution, DocumentId, SourceText, PreservationMode, Solution> withDocumentText,
            Action<Workspace, DocumentId> openTextDocument,
            CancellationToken cancellationToken)
        {
            Debug.Assert(oldDocument.Kind == TextDocumentKind.AdditionalDocument || oldDocument.Kind == TextDocumentKind.AnalyzerConfigDocument);

            cancellationToken.ThrowIfCancellationRequested();

            // Note: We don't use the original buffer that is associated with oldDocument
            // (and currently open in the editor) for oldBuffer below. This is because oldBuffer
            // will be used inside a projection buffer inside our inline diff preview below
            // and platform's implementation currently has a bug where projection buffers
            // are being leaked. This leak means that if we use the original buffer that is
            // currently visible in the editor here, the projection buffer span calculation
            // would be triggered every time user changes some code in this buffer (even though
            // the diff view would long have been dismissed by the time user edits the code)
            // resulting in crashes. Instead we create a new buffer from the same content.
            // TODO: We could use ITextBufferCloneService instead here to clone the original buffer.
            var oldBuffer = CreateNewPlainTextBuffer(oldDocument, cancellationToken);
            var newBuffer = CreateNewPlainTextBuffer(newDocument, cancellationToken);

            // Convert the diffs to be line based.  
            // Compute the diffs between the old text and the new.
            var diffResult = ComputeEditDifferences(oldDocument, newDocument, cancellationToken);

            // Need to show the spans in the right that are different.
            var originalSpans = GetOriginalSpans(diffResult, cancellationToken);
            var changedSpans = GetChangedSpans(diffResult, cancellationToken);

            string description = null;
            var originalLineSpans = CreateLineSpans(oldBuffer.CurrentSnapshot, originalSpans, cancellationToken);
            var changedLineSpans = CreateLineSpans(newBuffer.CurrentSnapshot, changedSpans, cancellationToken);

            // TODO: Why aren't we attaching conflict / warning annotations here like we do for regular documents above?

            // Create PreviewWorkspaces around the buffers to be displayed on the left and right
            // so that all IDE services (colorizer, squiggles etc.) light up in these buffers.
            var leftDocumentId = DocumentId.CreateNewId(oldDocument.Project.Id);
            var solutionWithRemovedDocument = removeTextDocument(oldDocument.Project.Solution, oldDocument.Id);
            var leftSolution = addTextDocument(solutionWithRemovedDocument, leftDocumentId, oldDocument.Name, oldBuffer.AsTextContainer().CurrentText);
            var leftWorkspace = new PreviewWorkspace(leftSolution);
            openTextDocument(leftWorkspace, leftDocumentId);

            var rightWorkSpace = new PreviewWorkspace(
                withDocumentText(oldDocument.Project.Solution, oldDocument.Id, newBuffer.AsTextContainer().CurrentText, PreservationMode.PreserveValue));
            openTextDocument(rightWorkSpace, newDocument.Id);

            return CreateChangedDocumentViewAsync(
                oldBuffer, newBuffer, description, originalLineSpans, changedLineSpans,
                leftWorkspace, rightWorkSpace, zoomLevel, cancellationToken);
        }

        public Task<object> CreateChangedAdditionalDocumentPreviewViewAsync(TextDocument oldDocument, TextDocument newDocument, double zoomLevel, CancellationToken cancellationToken)
        {
            return CreateChangedAdditionalOrAnalyzerConfigDocumentPreviewViewAsync(
                oldDocument, newDocument, zoomLevel,
                removeTextDocument: (solution, documentId) => solution.RemoveAdditionalDocument(documentId),
                addTextDocument: (solution, documentId, name, text) => solution.AddAdditionalDocument(documentId, name, text),
                withDocumentText: (solution, documentId, newText, mode) => solution.WithAdditionalDocumentText(documentId, newText, mode),
                openTextDocument: (workspace, documentId) => workspace.OpenAdditionalDocument(documentId),
                cancellationToken);
        }

        public Task<object> CreateChangedAnalyzerConfigDocumentPreviewViewAsync(TextDocument oldDocument, TextDocument newDocument, double zoomLevel, CancellationToken cancellationToken)
        {
            return CreateChangedAdditionalOrAnalyzerConfigDocumentPreviewViewAsync(
                oldDocument, newDocument, zoomLevel,
                removeTextDocument: (solution, documentId) => solution.RemoveAnalyzerConfigDocument(documentId),
                addTextDocument: (solution, documentId, name, text) => solution.AddAnalyzerConfigDocument(documentId, name, text),
                withDocumentText: (solution, documentId, newText, mode) => solution.WithAnalyzerConfigDocumentText(documentId, newText, mode),
                openTextDocument: (workspace, documentId) => workspace.OpenAnalyzerConfigDocument(documentId),
                cancellationToken);
        }

        private Task<object> CreateChangedDocumentViewAsync(ITextBuffer oldBuffer, ITextBuffer newBuffer, string description,
            List<LineSpan> originalSpans, List<LineSpan> changedSpans, PreviewWorkspace leftWorkspace, PreviewWorkspace rightWorkspace,
            double zoomLevel, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!(originalSpans.Any() && changedSpans.Any()))
            {
                // Both line spans must be non-empty. Otherwise, below projection buffer factory API call will throw.
                // So if either is empty (signaling that there are no changes to preview in the document), then we bail out.
                // This can happen in cases where the user has already applied the fix and light bulb has already been dismissed,
                // but platform hasn't cancelled the preview operation yet. Since the light bulb has already been dismissed at
                // this point, the preview that we return will never be displayed to the user. So returning null here is harmless.
                return SpecializedTasks.Default<object>();
            }

            var originalBuffer = _projectionBufferFactoryService.CreateProjectionBufferWithoutIndentation(
                _contentTypeRegistryService,
                _editorOptionsFactoryService.GlobalOptions,
                oldBuffer.CurrentSnapshot,
                "...",
                description,
                originalSpans.ToArray());

            var changedBuffer = _projectionBufferFactoryService.CreateProjectionBufferWithoutIndentation(
                _contentTypeRegistryService,
                _editorOptionsFactoryService.GlobalOptions,
                newBuffer.CurrentSnapshot,
                "...",
                description,
                changedSpans.ToArray());

            return CreateNewDifferenceViewerAsync(leftWorkspace, rightWorkspace, originalBuffer, changedBuffer, zoomLevel, cancellationToken);
        }

        private static void AttachAnnotationsToBuffer(
            ITextBuffer newBuffer, IEnumerable<Span> conflictSpans, IEnumerable<Span> warningSpans, IEnumerable<Span> suppressDiagnosticsSpans)
        {
            // Attach the spans to the buffer.
            newBuffer.Properties.AddProperty(PredefinedPreviewTaggerKeys.ConflictSpansKey, new NormalizedSnapshotSpanCollection(newBuffer.CurrentSnapshot, conflictSpans));
            newBuffer.Properties.AddProperty(PredefinedPreviewTaggerKeys.WarningSpansKey, new NormalizedSnapshotSpanCollection(newBuffer.CurrentSnapshot, warningSpans));
            newBuffer.Properties.AddProperty(PredefinedPreviewTaggerKeys.SuppressDiagnosticsSpansKey, new NormalizedSnapshotSpanCollection(newBuffer.CurrentSnapshot, suppressDiagnosticsSpans));
        }

        private ITextBuffer CreateNewBuffer(Document document, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // is it okay to create buffer from threads other than UI thread?
            var contentTypeService = document.GetLanguageService<IContentTypeLanguageService>();
            var contentType = contentTypeService.GetDefaultContentType();

            return _textBufferFactoryService.CreateTextBuffer(
                document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken).ToString(), contentType);
        }

        private ITextBuffer CreateNewPlainTextBuffer(TextDocument document, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var contentType = _textBufferFactoryService.TextContentType;

            return _textBufferFactoryService.CreateTextBuffer(
                document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken).ToString(), contentType);
        }

        private async Task<object> CreateNewDifferenceViewerAsync(
            PreviewWorkspace leftWorkspace, PreviewWorkspace rightWorkspace,
            IProjectionBuffer originalBuffer, IProjectionBuffer changedBuffer,
            double zoomLevel, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // leftWorkspace can be null if the change is adding a document.
            // rightWorkspace can be null if the change is removing a document.
            // However both leftWorkspace and rightWorkspace can't be null at the same time.
            Contract.ThrowIfTrue((leftWorkspace == null) && (rightWorkspace == null));

            var diffBuffer = _differenceBufferService.CreateDifferenceBuffer(
                originalBuffer, changedBuffer,
                new StringDifferenceOptions(), disableEditing: true);

            var diffViewer = _differenceViewerService.CreateDifferenceView(diffBuffer, _previewRoleSet);

            diffViewer.Closed += (s, e) =>
            {
                // Workaround Editor bug.  The editor has an issue where they sometimes crash when 
                // trying to apply changes to projection buffer.  So, when the user actually invokes
                // a SuggestedAction we may then edit a text buffer, which the editor will then 
                // try to propagate through the projections we have here over that buffer.  To ensure
                // that that doesn't happen, we clear out the projections first so that this crash
                // won't happen.
                originalBuffer.DeleteSpans(0, originalBuffer.CurrentSnapshot.SpanCount);
                changedBuffer.DeleteSpans(0, changedBuffer.CurrentSnapshot.SpanCount);

                leftWorkspace?.Dispose();
                leftWorkspace = null;

                rightWorkspace?.Dispose();
                rightWorkspace = null;
            };

            const string DiffOverviewMarginName = "deltadifferenceViewerOverview";
            if (leftWorkspace == null)
            {
                diffViewer.ViewMode = DifferenceViewMode.RightViewOnly;
                diffViewer.RightView.ZoomLevel *= zoomLevel;
                diffViewer.RightHost.GetTextViewMargin(DiffOverviewMarginName).VisualElement.Visibility = Visibility.Collapsed;
            }
            else if (rightWorkspace == null)
            {
                diffViewer.ViewMode = DifferenceViewMode.LeftViewOnly;
                diffViewer.LeftView.ZoomLevel *= zoomLevel;
                diffViewer.LeftHost.GetTextViewMargin(DiffOverviewMarginName).VisualElement.Visibility = Visibility.Collapsed;
            }
            else
            {
                diffViewer.ViewMode = DifferenceViewMode.Inline;
                diffViewer.InlineView.ZoomLevel *= zoomLevel;
                diffViewer.InlineHost.GetTextViewMargin(DiffOverviewMarginName).VisualElement.Visibility = Visibility.Collapsed;
            }

            // Disable focus / tab stop for the diff viewer.
            diffViewer.RightView.VisualElement.Focusable = false;
            diffViewer.LeftView.VisualElement.Focusable = false;
            diffViewer.InlineView.VisualElement.Focusable = false;

            // This code path must be invoked on UI thread.
            AssertIsForeground();

            // We use ConfigureAwait(true) to stay on the UI thread.
            await diffViewer.SizeToFitAsync(ThreadingContext).ConfigureAwait(true);

            leftWorkspace?.EnableDiagnostic();
            rightWorkspace?.EnableDiagnostic();

            return new DifferenceViewerPreview(diffViewer);
        }

        private List<LineSpan> CreateLineSpans(ITextSnapshot textSnapshot, NormalizedSpanCollection allSpans, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = new List<LineSpan>();

            foreach (var span in allSpans)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var lineSpan = GetLineSpan(textSnapshot, span);
                MergeLineSpans(result, lineSpan);
            }

            return result;
        }

        // Find the lines that surround the span of the difference.  Try to expand the span to
        // include both the previous and next lines so that we can show more context to the
        // user.
        private LineSpan GetLineSpan(
            ITextSnapshot snapshot,
            Span span)
        {
            var startLine = snapshot.GetLineNumberFromPosition(span.Start);
            var endLine = snapshot.GetLineNumberFromPosition(span.End);

            if (startLine > 0)
            {
                startLine--;
            }

            if (endLine < snapshot.LineCount)
            {
                endLine++;
            }

            return LineSpan.FromBounds(startLine, endLine);
        }

        // Adds a line span to the spans we've been collecting.  If the line span overlaps or
        // abuts a previous span then the two are merged.
        private static void MergeLineSpans(List<LineSpan> lineSpans, LineSpan nextLineSpan)
        {
            if (lineSpans.Count > 0)
            {
                var lastLineSpan = lineSpans.Last();

                // We merge them if there's no more than one line between the two.  Otherwise
                // we'd show "..." between two spans where we could just show the actual code. 
                if (nextLineSpan.Start >= lastLineSpan.Start && nextLineSpan.Start <= (lastLineSpan.End + 1))
                {
                    nextLineSpan = LineSpan.FromBounds(lastLineSpan.Start, nextLineSpan.End);
                    lineSpans.RemoveAt(lineSpans.Count - 1);
                }
            }

            lineSpans.Add(nextLineSpan);
        }

        private IHierarchicalDifferenceCollection ComputeEditDifferences(TextDocument oldDocument, TextDocument newDocument, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get the text that's actually in the editor.
            var oldText = oldDocument.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var newText = newDocument.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            // Defer to the editor to figure out what changes the client made.
            var diffService = _differenceSelectorService.GetTextDifferencingService(
                oldDocument.Project.LanguageServices.GetService<IContentTypeLanguageService>().GetDefaultContentType());

            diffService ??= _differenceSelectorService.DefaultTextDifferencingService;
            return diffService.DiffStrings(oldText.ToString(), newText.ToString(), new StringDifferenceOptions()
            {
                DifferenceType = StringDifferenceTypes.Word | StringDifferenceTypes.Line,
            });
        }

        private NormalizedSpanCollection GetOriginalSpans(IHierarchicalDifferenceCollection diffResult, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lineSpans = new List<Span>();

            foreach (var difference in diffResult)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var mappedSpan = diffResult.LeftDecomposition.GetSpanInOriginal(difference.Left);
                lineSpans.Add(mappedSpan);
            }

            return new NormalizedSpanCollection(lineSpans);
        }

        private NormalizedSpanCollection GetChangedSpans(IHierarchicalDifferenceCollection diffResult, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lineSpans = new List<Span>();

            foreach (var difference in diffResult)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var mappedSpan = diffResult.RightDecomposition.GetSpanInOriginal(difference.Right);
                lineSpans.Add(mappedSpan);
            }

            return new NormalizedSpanCollection(lineSpans);
        }
    }
}
