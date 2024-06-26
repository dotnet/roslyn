// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Implementation.TextDiffing;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview;

[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal abstract class AbstractPreviewFactoryService<TDifferenceViewer>(
    IThreadingContext threadingContext,
    ITextBufferFactoryService textBufferFactoryService,
    ITextBufferCloneService textBufferCloneService,
    IContentTypeRegistryService contentTypeRegistryService,
    IProjectionBufferFactoryService projectionBufferFactoryService,
    EditorOptionsService editorOptionsService,
    ITextDifferencingSelectorService differenceSelectorService,
    IDifferenceBufferFactoryService differenceBufferService,
    ITextDocumentFactoryService textDocumentFactoryService,
    ITextViewRoleSet previewRoleSet) : IPreviewFactoryService
    where TDifferenceViewer : IDifferenceViewer
{
    private const double DefaultZoomLevel = 0.75;
    private readonly ITextViewRoleSet _previewRoleSet = previewRoleSet;
    private readonly ITextBufferFactoryService _textBufferFactoryService = textBufferFactoryService;
    private readonly ITextBufferCloneService _textBufferCloneService = textBufferCloneService;
    private readonly IContentTypeRegistryService _contentTypeRegistryService = contentTypeRegistryService;
    private readonly IProjectionBufferFactoryService _projectionBufferFactoryService = projectionBufferFactoryService;
    private readonly EditorOptionsService _editorOptionsService = editorOptionsService;
    private readonly ITextDifferencingSelectorService _differenceSelectorService = differenceSelectorService;
    private readonly IDifferenceBufferFactoryService _differenceBufferService = differenceBufferService;
    private readonly ITextDocumentFactoryService _textDocumentFactoryService = textDocumentFactoryService;

    private static readonly StringDifferenceOptions s_differenceOptions = new()
    {
        DifferenceType = StringDifferenceTypes.Word | StringDifferenceTypes.Line,
    };

    protected readonly IThreadingContext ThreadingContext = threadingContext;

    public SolutionPreviewResult? GetSolutionPreviews(Solution oldSolution, Solution? newSolution, CancellationToken cancellationToken)
        => GetSolutionPreviews(oldSolution, newSolution, DefaultZoomLevel, cancellationToken);

    public SolutionPreviewResult? GetSolutionPreviews(Solution oldSolution, Solution? newSolution, double zoomLevel, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Note: The order in which previews are added to the below list is significant.
        // Preview for a changed document is preferred over preview for changed references and so on.
        var previewItems = new List<SolutionPreviewItem>();
        SolutionChangeSummary? changeSummary = null;
        if (newSolution != null)
        {
            var solutionChanges = newSolution.GetChanges(oldSolution);
            var ignoreUnchangeableDocuments = oldSolution.Workspace.IgnoreUnchangeableDocumentsWhenApplyingChanges;

            foreach (var projectChanges in solutionChanges.GetProjectChanges())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var projectId = projectChanges.ProjectId;
                var oldProject = projectChanges.OldProject;
                var newProject = projectChanges.NewProject;

                // Exclude changes to unchangeable documents if they will be ignored when applied to workspace.
                foreach (var documentId in projectChanges.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true, ignoreUnchangeableDocuments))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    previewItems.Add(new SolutionPreviewItem(documentId.ProjectId, documentId, async c =>
                        await CreateChangedDocumentPreviewViewAsync(oldSolution.GetRequiredDocument(documentId), newSolution.GetRequiredDocument(documentId), zoomLevel, c).ConfigureAwaitRunInline()));
                }

                foreach (var documentId in projectChanges.GetAddedDocuments())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    previewItems.Add(new SolutionPreviewItem(documentId.ProjectId, documentId, async c =>
                        await CreateAddedDocumentPreviewViewAsync(newSolution.GetRequiredDocument(documentId), zoomLevel, c).ConfigureAwaitRunInline()));
                }

                foreach (var documentId in projectChanges.GetRemovedDocuments())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    previewItems.Add(new SolutionPreviewItem(oldProject.Id, documentId, async c =>
                        await CreateRemovedDocumentPreviewViewAsync(oldSolution.GetRequiredDocument(documentId), zoomLevel, c).ConfigureAwaitRunInline()));
                }

                foreach (var documentId in projectChanges.GetChangedAdditionalDocuments())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    previewItems.Add(new SolutionPreviewItem(documentId.ProjectId, documentId, async c =>
                        await CreateChangedAdditionalDocumentPreviewViewAsync(oldSolution.GetRequiredAdditionalDocument(documentId), newSolution.GetRequiredAdditionalDocument(documentId), zoomLevel, c).ConfigureAwaitRunInline()));
                }

                foreach (var documentId in projectChanges.GetAddedAdditionalDocuments())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    previewItems.Add(new SolutionPreviewItem(documentId.ProjectId, documentId, async c =>
                        await CreateAddedAdditionalDocumentPreviewViewAsync(newSolution.GetRequiredAdditionalDocument(documentId), zoomLevel, c).ConfigureAwaitRunInline()));
                }

                foreach (var documentId in projectChanges.GetRemovedAdditionalDocuments())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    previewItems.Add(new SolutionPreviewItem(oldProject.Id, documentId, async c =>
                        await CreateRemovedAdditionalDocumentPreviewViewAsync(oldSolution.GetRequiredAdditionalDocument(documentId), zoomLevel, c).ConfigureAwaitRunInline()));
                }

                foreach (var documentId in projectChanges.GetChangedAnalyzerConfigDocuments())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    previewItems.Add(new SolutionPreviewItem(documentId.ProjectId, documentId, async c =>
                        await CreateChangedAnalyzerConfigDocumentPreviewViewAsync(oldSolution.GetRequiredAnalyzerConfigDocument(documentId), newSolution.GetRequiredAnalyzerConfigDocument(documentId), zoomLevel, c).ConfigureAwaitRunInline()));
                }

                foreach (var documentId in projectChanges.GetAddedAnalyzerConfigDocuments())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    previewItems.Add(new SolutionPreviewItem(documentId.ProjectId, documentId, async c =>
                        await CreateAddedAnalyzerConfigDocumentPreviewViewAsync(newSolution.GetRequiredAnalyzerConfigDocument(documentId), zoomLevel, c).ConfigureAwaitRunInline()));
                }

                foreach (var documentId in projectChanges.GetRemovedAnalyzerConfigDocuments())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    previewItems.Add(new SolutionPreviewItem(oldProject.Id, documentId, async c =>
                        await CreateRemovedAnalyzerConfigDocumentPreviewViewAsync(oldSolution.GetRequiredAnalyzerConfigDocument(documentId), zoomLevel, c).ConfigureAwaitRunInline()));
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
                        string.Format(EditorFeaturesResources.Adding_reference_0_to_1, newSolution.GetRequiredProject(projectReference.ProjectId).Name, oldProject.Name)));
                }

                foreach (var projectReference in projectChanges.GetRemovedProjectReferences())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    previewItems.Add(new SolutionPreviewItem(oldProject.Id, null,
                        string.Format(EditorFeaturesResources.Removing_reference_0_from_1, oldSolution.GetRequiredProject(projectReference.ProjectId).Name, oldProject.Name)));
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

    public Task<IDifferenceViewerPreview<TDifferenceViewer>> CreateAddedDocumentPreviewViewAsync(Document document, CancellationToken cancellationToken)
        => CreateAddedDocumentPreviewViewAsync(document, DefaultZoomLevel, cancellationToken);

    private async ValueTask<IDifferenceViewerPreview<TDifferenceViewer>> CreateAddedDocumentPreviewViewCoreAsync(ITextBuffer newBuffer, ReferenceCountedDisposable<PreviewWorkspace> workspace, TextDocument document, double zoomLevel, CancellationToken cancellationToken)
    {
        // IProjectionBufferFactoryService is a Visual Studio API which is not documented as free-threaded
        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var firstLine = string.Format(EditorFeaturesResources.Adding_0_to_1_with_content_colon,
            document.Name, document.Project.Name);

        var originalBuffer = _projectionBufferFactoryService.CreatePreviewProjectionBuffer(
            sourceSpans: [firstLine, "\r\n"], registryService: _contentTypeRegistryService);

        var span = new SnapshotSpan(newBuffer.CurrentSnapshot, Span.FromBounds(0, newBuffer.CurrentSnapshot.Length))
            .CreateTrackingSpan(SpanTrackingMode.EdgeExclusive);
        var changedBuffer = _projectionBufferFactoryService.CreatePreviewProjectionBuffer(
            sourceSpans: [firstLine, "\r\n", span], registryService: _contentTypeRegistryService);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        return await CreateNewDifferenceViewerAsync(null, workspace, originalBuffer, changedBuffer, zoomLevel, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
    }

    private async Task<IDifferenceViewerPreview<TDifferenceViewer>> CreateAddedTextDocumentPreviewViewAsync<TDocument>(
        TDocument document,
        double zoomLevel,
        Func<TDocument, CancellationToken, ValueTask<ITextBuffer>> createBufferAsync,
        CancellationToken cancellationToken)
        where TDocument : TextDocument
    {
        // createBufferAsync must be called from the main thread
        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        var newBuffer = await createBufferAsync(document, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

        // Create PreviewWorkspace around the buffer to be displayed in the diff preview
        // so that all IDE services (colorizer, squiggles etc.) light up in this buffer.
        using var rightWorkspace = new ReferenceCountedDisposable<PreviewWorkspace>(new PreviewWorkspace(document.Project.Solution));
        rightWorkspace.Target.OpenDocument(document.Id, newBuffer.AsTextContainer());

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        return await CreateAddedDocumentPreviewViewCoreAsync(newBuffer, rightWorkspace, document, zoomLevel, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
    }

    public Task<IDifferenceViewerPreview<TDifferenceViewer>> CreateAddedDocumentPreviewViewAsync(Document document, double zoomLevel, CancellationToken cancellationToken)
    {
        return CreateAddedTextDocumentPreviewViewAsync(
            document, zoomLevel,
            createBufferAsync: (textDocument, cancellationToken) => CreateNewBufferAsync(textDocument, cancellationToken),
            cancellationToken);
    }

    public Task<IDifferenceViewerPreview<TDifferenceViewer>> CreateAddedAdditionalDocumentPreviewViewAsync(TextDocument document, double zoomLevel, CancellationToken cancellationToken)
    {
        return CreateAddedTextDocumentPreviewViewAsync(
            document, zoomLevel,
            createBufferAsync: CreateNewPlainTextBufferAsync,
            cancellationToken);
    }

    public Task<IDifferenceViewerPreview<TDifferenceViewer>> CreateAddedAnalyzerConfigDocumentPreviewViewAsync(TextDocument document, double zoomLevel, CancellationToken cancellationToken)
    {
        return CreateAddedTextDocumentPreviewViewAsync(
            document, zoomLevel,
            createBufferAsync: CreateNewPlainTextBufferAsync,
            cancellationToken);
    }

    public Task<IDifferenceViewerPreview<TDifferenceViewer>> CreateRemovedDocumentPreviewViewAsync(Document document, CancellationToken cancellationToken)
        => CreateRemovedDocumentPreviewViewAsync(document, DefaultZoomLevel, cancellationToken);

    private async ValueTask<IDifferenceViewerPreview<TDifferenceViewer>> CreateRemovedDocumentPreviewViewCoreAsync(ITextBuffer oldBuffer, ReferenceCountedDisposable<PreviewWorkspace> workspace, TextDocument document, double zoomLevel, CancellationToken cancellationToken)
    {
        // IProjectionBufferFactoryService is a Visual Studio API which is not documented as free-threaded
        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var firstLine = string.Format(EditorFeaturesResources.Removing_0_from_1_with_content_colon,
            document.Name, document.Project.Name);

        var span = new SnapshotSpan(oldBuffer.CurrentSnapshot, Span.FromBounds(0, oldBuffer.CurrentSnapshot.Length))
            .CreateTrackingSpan(SpanTrackingMode.EdgeExclusive);
        var originalBuffer = _projectionBufferFactoryService.CreatePreviewProjectionBuffer(
            sourceSpans: [firstLine, "\r\n", span], registryService: _contentTypeRegistryService);

        var changedBuffer = _projectionBufferFactoryService.CreatePreviewProjectionBuffer(
            sourceSpans: [firstLine, "\r\n"], registryService: _contentTypeRegistryService);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        return await CreateNewDifferenceViewerAsync(workspace, null, originalBuffer, changedBuffer, zoomLevel, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
    }

    private async Task<IDifferenceViewerPreview<TDifferenceViewer>> CreateRemovedTextDocumentPreviewViewAsync<TDocument>(
        TDocument document,
        double zoomLevel,
        Func<TDocument, CancellationToken, ValueTask<ITextBuffer>> createBufferAsync,
        CancellationToken cancellationToken)
        where TDocument : TextDocument
    {
        // createBufferAsync must be called from the main thread
        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        var oldBuffer = await createBufferAsync(document, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

        // Create PreviewWorkspace around the buffer to be displayed in the diff preview
        // so that all IDE services (colorizer, squiggles etc.) light up in this buffer.
        using var leftWorkspace = new ReferenceCountedDisposable<PreviewWorkspace>(new PreviewWorkspace(document.Project.Solution));
        leftWorkspace.Target.OpenDocument(document.Id, oldBuffer.AsTextContainer());

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        return await CreateRemovedDocumentPreviewViewCoreAsync(oldBuffer, leftWorkspace, document, zoomLevel, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
    }

    public Task<IDifferenceViewerPreview<TDifferenceViewer>> CreateRemovedDocumentPreviewViewAsync(Document document, double zoomLevel, CancellationToken cancellationToken)
    {
        return CreateRemovedTextDocumentPreviewViewAsync(
            document, zoomLevel,
            createBufferAsync: (textDocument, cancellationToken) => CreateNewBufferAsync(textDocument, cancellationToken),
            cancellationToken);
    }

    public Task<IDifferenceViewerPreview<TDifferenceViewer>> CreateRemovedAdditionalDocumentPreviewViewAsync(TextDocument document, double zoomLevel, CancellationToken cancellationToken)
    {
        return CreateRemovedTextDocumentPreviewViewAsync(
            document, zoomLevel,
            createBufferAsync: CreateNewPlainTextBufferAsync,
            cancellationToken);
    }

    public Task<IDifferenceViewerPreview<TDifferenceViewer>> CreateRemovedAnalyzerConfigDocumentPreviewViewAsync(TextDocument document, double zoomLevel, CancellationToken cancellationToken)
    {
        return CreateRemovedTextDocumentPreviewViewAsync(
            document, zoomLevel,
            createBufferAsync: CreateNewPlainTextBufferAsync,
            cancellationToken);
    }

    public Task<IDifferenceViewerPreview<TDifferenceViewer>?> CreateChangedDocumentPreviewViewAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
        => CreateChangedDocumentPreviewViewAsync(oldDocument, newDocument, DefaultZoomLevel, cancellationToken);

    public async Task<IDifferenceViewerPreview<TDifferenceViewer>?> CreateChangedDocumentPreviewViewAsync(Document oldDocument, Document newDocument, double zoomLevel, CancellationToken cancellationToken)
    {
        // CreateNewBufferAsync must be called from the main thread
        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        var oldBuffer = await CreateNewBufferAsync(oldDocument, cancellationToken);
        var newBuffer = await CreateNewBufferAsync(newDocument, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

        // Convert the diffs to be line based.  
        // Compute the diffs between the old text and the new.
        var diffResult = ComputeEditDifferences(oldDocument, newDocument, cancellationToken);

        // Need to show the spans in the right that are different.
        // We also need to show the spans that are in conflict.
        var originalSpans = GetOriginalSpans(diffResult, cancellationToken);
        var changedSpans = GetChangedSpans(diffResult, cancellationToken);
        string? description = null;
        NormalizedSpanCollection allSpans;

        if (newDocument.SupportsSyntaxTree)
        {
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
            var newRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
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
        //
        // Performance: Replace related documents to oldBuffer and newBuffer in these workspaces with the 
        // relating SourceText. This prevents cascading forks as taggers call to
        // GetOpenTextDocumentInCurrentContextWithChanges would eventually wind up
        // calling Solution.WithDocumentText using the related ids.
        var leftSolution = oldDocument.Project.Solution;
        var allLeftIds = leftSolution.GetRelatedDocumentIds(oldDocument.Id);
        leftSolution = leftSolution.WithDocumentText(allLeftIds, oldBuffer.AsTextContainer().CurrentText, PreservationMode.PreserveIdentity);

        using var leftWorkspace = new ReferenceCountedDisposable<PreviewWorkspace>(new PreviewWorkspace(leftSolution));
        leftWorkspace.Target.OpenDocument(oldDocument.Id, oldBuffer.AsTextContainer());

        var rightSolution = newDocument.Project.Solution;
        var allRightIds = rightSolution.GetRelatedDocumentIds(newDocument.Id);
        rightSolution = rightSolution.WithDocumentText(allRightIds, newBuffer.AsTextContainer().CurrentText, PreservationMode.PreserveIdentity);

        using var rightWorkspace = new ReferenceCountedDisposable<PreviewWorkspace>(new PreviewWorkspace(rightSolution));
        rightWorkspace.Target.OpenDocument(newDocument.Id, newBuffer.AsTextContainer());

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        return await CreateChangedDocumentViewAsync(
            oldBuffer, newBuffer, description, originalLineSpans, changedLineSpans,
            leftWorkspace, rightWorkspace, zoomLevel, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
    }

    // NOTE: We are only sharing this code between additional documents and analyzer config documents,
    // which are essentially plain text documents. Regular source documents need special handling
    // and hence have a different implementation.
    private async Task<IDifferenceViewerPreview<TDifferenceViewer>?> CreateChangedAdditionalOrAnalyzerConfigDocumentPreviewViewAsync(
        TextDocument oldDocument,
        TextDocument newDocument,
        double zoomLevel,
        CancellationToken cancellationToken)
    {
        Debug.Assert(oldDocument.Kind is TextDocumentKind.AdditionalDocument or TextDocumentKind.AnalyzerConfigDocument);

        // openTextDocument must be called from the main thread
        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        var oldBuffer = await CreateNewPlainTextBufferAsync(oldDocument, cancellationToken);
        var newBuffer = await CreateNewPlainTextBufferAsync(newDocument, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

        // Convert the diffs to be line based.  
        // Compute the diffs between the old text and the new.
        var diffResult = ComputeEditDifferences(oldDocument, newDocument, cancellationToken);

        // Need to show the spans in the right that are different.
        var originalSpans = GetOriginalSpans(diffResult, cancellationToken);
        var changedSpans = GetChangedSpans(diffResult, cancellationToken);

        var originalLineSpans = CreateLineSpans(oldBuffer.CurrentSnapshot, originalSpans, cancellationToken);
        var changedLineSpans = CreateLineSpans(newBuffer.CurrentSnapshot, changedSpans, cancellationToken);

        // TODO: Why aren't we attaching conflict / warning annotations here like we do for regular documents above?

        // Create PreviewWorkspaces around the buffers to be displayed on the left and right
        // so that all IDE services (colorizer, squiggles etc.) light up in these buffers.
        using var leftWorkspace = new ReferenceCountedDisposable<PreviewWorkspace>(new PreviewWorkspace(oldDocument.Project.Solution));
        leftWorkspace.Target.OpenDocument(oldDocument.Id, oldBuffer.AsTextContainer());

        using var rightWorkspace = new ReferenceCountedDisposable<PreviewWorkspace>(new PreviewWorkspace(newDocument.Project.Solution));
        rightWorkspace.Target.OpenDocument(newDocument.Id, newBuffer.AsTextContainer());

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        return await CreateChangedDocumentViewAsync(
            oldBuffer, newBuffer, description: null, originalLineSpans, changedLineSpans,
            leftWorkspace, rightWorkspace, zoomLevel, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
    }

    public Task<IDifferenceViewerPreview<TDifferenceViewer>?> CreateChangedAdditionalDocumentPreviewViewAsync(TextDocument oldDocument, TextDocument newDocument, double zoomLevel, CancellationToken cancellationToken)
    {
        return CreateChangedAdditionalOrAnalyzerConfigDocumentPreviewViewAsync(
            oldDocument, newDocument, zoomLevel, cancellationToken);
    }

    public Task<IDifferenceViewerPreview<TDifferenceViewer>?> CreateChangedAnalyzerConfigDocumentPreviewViewAsync(TextDocument oldDocument, TextDocument newDocument, double zoomLevel, CancellationToken cancellationToken)
    {
        return CreateChangedAdditionalOrAnalyzerConfigDocumentPreviewViewAsync(
            oldDocument, newDocument, zoomLevel, cancellationToken);
    }

    private async ValueTask<IDifferenceViewerPreview<TDifferenceViewer>?> CreateChangedDocumentViewAsync(ITextBuffer oldBuffer, ITextBuffer newBuffer, string? description,
        List<LineSpan> originalSpans, List<LineSpan> changedSpans, ReferenceCountedDisposable<PreviewWorkspace> leftWorkspace, ReferenceCountedDisposable<PreviewWorkspace> rightWorkspace,
        double zoomLevel, CancellationToken cancellationToken)
    {
        if (!(originalSpans.Any() && changedSpans.Any()))
        {
            // Both line spans must be non-empty. Otherwise, below projection buffer factory API call will throw.
            // So if either is empty (signaling that there are no changes to preview in the document), then we bail out.
            // This can happen in cases where the user has already applied the fix and light bulb has already been dismissed,
            // but platform hasn't cancelled the preview operation yet. Since the light bulb has already been dismissed at
            // this point, the preview that we return will never be displayed to the user. So returning null here is harmless.

            // TODO: understand how this can even happen. The diff input is stable -- we shouldn't be depending on some sort of
            // state that could change underneath us. If we know the file changed, how would we discover here it didn't?
            return null;
        }

        // IProjectionBufferFactoryService is a Visual Studio API which is not documented as free-threaded
        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var originalBuffer = _projectionBufferFactoryService.CreateProjectionBufferWithoutIndentation(
            _contentTypeRegistryService,
            _editorOptionsService.Factory.GlobalOptions,
            oldBuffer.CurrentSnapshot,
            "...",
            description,
            [.. originalSpans]);

        var changedBuffer = _projectionBufferFactoryService.CreateProjectionBufferWithoutIndentation(
            _contentTypeRegistryService,
            _editorOptionsService.Factory.GlobalOptions,
            newBuffer.CurrentSnapshot,
            "...",
            description,
            [.. changedSpans]);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        return await CreateNewDifferenceViewerAsync(leftWorkspace, rightWorkspace, originalBuffer, changedBuffer, zoomLevel, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
    }

    private static void AttachAnnotationsToBuffer(
        ITextBuffer newBuffer, IEnumerable<Span> conflictSpans, IEnumerable<Span> warningSpans, IEnumerable<Span> suppressDiagnosticsSpans)
    {
        // Attach the spans to the buffer.
        newBuffer.Properties.AddProperty(PredefinedPreviewTaggerKeys.ConflictSpansKey, new NormalizedSnapshotSpanCollection(newBuffer.CurrentSnapshot, conflictSpans));
        newBuffer.Properties.AddProperty(PredefinedPreviewTaggerKeys.WarningSpansKey, new NormalizedSnapshotSpanCollection(newBuffer.CurrentSnapshot, warningSpans));
        newBuffer.Properties.AddProperty(PredefinedPreviewTaggerKeys.SuppressDiagnosticsSpansKey, new NormalizedSnapshotSpanCollection(newBuffer.CurrentSnapshot, suppressDiagnosticsSpans));
    }

    private async ValueTask<ITextBuffer> CreateNewBufferAsync(Document document, CancellationToken cancellationToken)
    {
        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var contentTypeService = document.GetRequiredLanguageService<IContentTypeLanguageService>();
        var contentType = contentTypeService.GetDefaultContentType();

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        return await CreateTextBufferCoreAsync(document, contentType, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
    }

    private async ValueTask<ITextBuffer> CreateNewPlainTextBufferAsync(TextDocument document, CancellationToken cancellationToken)
    {
        // ITextBufferFactoryService is a Visual Studio API which is not documented as free-threaded
        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var contentType = _textBufferFactoryService.TextContentType;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        return await CreateTextBufferCoreAsync(document, contentType, cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
    }

    private async ValueTask<ITextBuffer> CreateTextBufferCoreAsync(TextDocument document, IContentType contentType, CancellationToken cancellationToken)
    {
        ThreadingContext.ThrowIfNotOnUIThread();

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
        var text = await document.State.GetTextAsync(cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

        var buffer = _textBufferCloneService.Clone(text, contentType);

        // Associate buffer with a text document with random file path to satisfy extensibility points expecting absolute file path.
        _textDocumentFactoryService.CreateTextDocument(buffer, Path.GetTempFileName());

        return buffer;
    }

    protected abstract IDifferenceViewerPreview<TDifferenceViewer> CreateDifferenceViewerPreview(TDifferenceViewer viewer);
    protected abstract Task<TDifferenceViewer> CreateDifferenceViewAsync(IDifferenceBuffer diffBuffer, ITextViewRoleSet previewRoleSet, DifferenceViewMode mode, double zoomLevel, CancellationToken cancellationToken);

    private async ValueTask<IDifferenceViewerPreview<TDifferenceViewer>> CreateNewDifferenceViewerAsync(
        ReferenceCountedDisposable<PreviewWorkspace>? leftWorkspace, ReferenceCountedDisposable<PreviewWorkspace>? rightWorkspace,
        IProjectionBuffer originalBuffer, IProjectionBuffer changedBuffer,
        double zoomLevel, CancellationToken cancellationToken)
    {
        // IWpfDifferenceViewerFactoryService is a Visual Studio API which is not documented as free-threaded
        await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // leftWorkspace can be null if the change is adding a document.
        // rightWorkspace can be null if the change is removing a document.
        // However both leftWorkspace and rightWorkspace can't be null at the same time.
        Contract.ThrowIfTrue((leftWorkspace == null) && (rightWorkspace == null));

        var diffBuffer = _differenceBufferService.CreateDifferenceBuffer(
            originalBuffer, changedBuffer,
            new StringDifferenceOptions(), disableEditing: true);

        var mode = leftWorkspace == null ? DifferenceViewMode.RightViewOnly :
                   rightWorkspace == null ? DifferenceViewMode.LeftViewOnly :
                                            DifferenceViewMode.Inline;

        var diffViewer = await CreateDifferenceViewAsync(diffBuffer, _previewRoleSet, mode, zoomLevel, cancellationToken).ConfigureAwait(true);

        // Claim ownership of the workspace references
        leftWorkspace = leftWorkspace?.TryAddReference();
        rightWorkspace = rightWorkspace?.TryAddReference();

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

        return CreateDifferenceViewerPreview(diffViewer);
    }

    private static List<LineSpan> CreateLineSpans(ITextSnapshot textSnapshot, NormalizedSpanCollection allSpans, CancellationToken cancellationToken)
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
    private static LineSpan GetLineSpan(
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
        var oldText = oldDocument.GetTextSynchronously(cancellationToken);
        var newText = newDocument.GetTextSynchronously(cancellationToken);

        // Defer to the editor to figure out what changes the client made.
        var diffService = _differenceSelectorService.GetTextDifferencingService(
            oldDocument.Project.Services.GetRequiredService<IContentTypeLanguageService>().GetDefaultContentType());

        diffService ??= _differenceSelectorService.DefaultTextDifferencingService;

        return diffService.DiffSourceTexts(oldText, newText, s_differenceOptions);
    }

    private static NormalizedSpanCollection GetOriginalSpans(IHierarchicalDifferenceCollection diffResult, CancellationToken cancellationToken)
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

    private static NormalizedSpanCollection GetChangedSpans(IHierarchicalDifferenceCollection diffResult, CancellationToken cancellationToken)
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
