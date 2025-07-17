// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

internal sealed partial class ProjectSystemProject
{
    /// <summary>
    /// Helper class to manage collections of source-file like things; this exists just to avoid duplicating all the logic for regular source files
    /// and additional files.
    /// </summary>
    /// <remarks>This class should be free-threaded, and any synchronization is done via <see cref="ProjectSystemProject._gate"/>.
    /// This class is otherwise free to operate on private members of <see cref="_project"/> if needed.</remarks>
    private sealed class BatchingDocumentCollection
    {
        private readonly ProjectSystemProject _project;

        /// <summary>
        /// The map of file paths to the underlying <see cref="DocumentId"/>. This document may exist in <see cref="_documentsAddedInBatch"/> or has been
        /// pushed to the actual workspace.
        /// </summary>
        private readonly Dictionary<string, DocumentId> _documentPathsToDocumentIds = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A map of explicitly-added "always open" <see cref="SourceTextContainer"/> and their associated <see cref="DocumentId"/>. This does not contain
        /// any regular files that have been open.
        /// </summary>
        private IBidirectionalMap<SourceTextContainer, DocumentId> _sourceTextContainersToDocumentIds = BidirectionalMap<SourceTextContainer, DocumentId>.Empty;

        /// <summary>
        /// The map of <see cref="DocumentId"/> to <see cref="IDynamicFileInfoProvider"/> whose <see cref="DynamicFileInfo"/> got added into <see cref="Workspace"/>
        /// </summary>
        private readonly Dictionary<DocumentId, IDynamicFileInfoProvider> _documentIdToDynamicFileInfoProvider = [];

        /// <summary>
        /// The current list of documents that are to be added in this batch.
        /// </summary>
        private readonly ImmutableArray<DocumentInfo>.Builder _documentsAddedInBatch = ImmutableArray.CreateBuilder<DocumentInfo>();

        /// <summary>
        /// The current list of documents that are being removed in this batch. Once the document is in this list, it is no longer in <see cref="_documentPathsToDocumentIds"/>.
        /// </summary>
        private readonly List<DocumentId> _documentsRemovedInBatch = [];

        /// <summary>
        /// The current list of document file paths that will be ordered in a batch.
        /// </summary>
        private ImmutableList<DocumentId>? _orderedDocumentsInBatch = null;

        private readonly Func<Solution, DocumentId, bool> _documentAlreadyInWorkspace;
        private readonly Action<Workspace, DocumentInfo> _documentAddAction;
        private readonly Action<Workspace, DocumentId> _documentRemoveAction;
        private readonly Func<Solution, DocumentId, TextLoader, Solution> _documentTextLoaderChangedAction;
        private readonly WorkspaceChangeKind _documentChangedWorkspaceKind;

        /// <summary>
        /// An <see cref="AsyncBatchingWorkQueue"/> for processing updates to dynamic files. This is lazily created the first time we see
        /// a change to process, since dynamic files are only used in certain Razor scenarios and most projects won't ever have one.
        /// </summary>
        /// <remarks>
        /// This is used for two reasons: first, if we have a flurry of events we want to deduplicate them. But it also ensures ordering -- if we were to get a change to
        /// a dynamic file while we're already processing another change, we want to ensure that the first change is processed before the second one. Otherwise we might
        /// end up with the updates being applied out of order (since we're not always holding a lock while calling to the dynamic file info provider) and we might end up with
        /// an old version stuck in the workspace.
        /// </remarks>
        private AsyncBatchingWorkQueue<(string projectSystemFilePath, string workspaceFilePath)>? _dynamicFilesToRefresh;

        public BatchingDocumentCollection(ProjectSystemProject project,
            Func<Solution, DocumentId, bool> documentAlreadyInWorkspace,
            Action<Workspace, DocumentInfo> documentAddAction,
            Action<Workspace, DocumentId> documentRemoveAction,
            Func<Solution, DocumentId, TextLoader, Solution> documentTextLoaderChangedAction,
            WorkspaceChangeKind documentChangedWorkspaceKind)
        {
            _project = project;
            _documentAlreadyInWorkspace = documentAlreadyInWorkspace;
            _documentAddAction = documentAddAction;
            _documentRemoveAction = documentRemoveAction;
            _documentTextLoaderChangedAction = documentTextLoaderChangedAction;
            _documentChangedWorkspaceKind = documentChangedWorkspaceKind;
        }

        public DocumentId AddFile(string fullPath, SourceCodeKind sourceCodeKind, ImmutableArray<string> folders)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentException($"{nameof(fullPath)} isn't a valid path.", nameof(fullPath));
            }

            var documentId = DocumentId.CreateNewId(_project.Id, fullPath);
            var textLoader = _project._projectSystemProjectFactory.CreateFileTextLoader(fullPath);
            var documentInfo = DocumentInfo.Create(
                documentId,
                name: FileNameUtilities.GetFileName(fullPath),
                folders: folders.IsDefault ? null : folders,
                sourceCodeKind: sourceCodeKind,
                loader: textLoader,
                filePath: fullPath);

            using (_project._gate.DisposableWait())
            {
                if (_documentPathsToDocumentIds.ContainsKey(fullPath))
                {
                    throw new ArgumentException($"'{fullPath}' has already been added to this project.", nameof(fullPath));
                }

                // If we have an ordered document ids batch, we need to add the document id to the end of it as well.
                _orderedDocumentsInBatch = _orderedDocumentsInBatch?.Add(documentId);

                _documentPathsToDocumentIds.Add(fullPath, documentId);
                _project._documentWatchedFiles.Add(documentId, _project._documentFileChangeContext.EnqueueWatchingFile(fullPath));

                if (_project._activeBatchScopes > 0)
                {
                    _documentsAddedInBatch.Add(documentInfo);
                }
                else
                {
                    _project._projectSystemProjectFactory.ApplyChangeToWorkspace(w => _documentAddAction(w, documentInfo));
                    _project._projectSystemProjectFactory.RaiseOnDocumentsAddedMaybeAsync(useAsync: false, [fullPath]).VerifyCompleted();
                }
            }

            return documentId;
        }

        public DocumentId AddTextContainer(
            SourceTextContainer textContainer,
            string fullPath,
            SourceCodeKind sourceCodeKind,
            ImmutableArray<string> folders,
            bool designTimeOnly,
            IDocumentServiceProvider? documentServiceProvider)
        {
            if (textContainer == null)
            {
                throw new ArgumentNullException(nameof(textContainer));
            }

            var documentId = DocumentId.CreateNewId(_project.Id, fullPath);
            var textLoader = new SourceTextLoader(textContainer, fullPath);
            var documentInfo = DocumentInfo.Create(
                documentId,
                FileNameUtilities.GetFileName(fullPath),
                folders: folders.NullToEmpty(),
                sourceCodeKind: sourceCodeKind,
                loader: textLoader,
                filePath: fullPath)
                .WithDesignTimeOnly(designTimeOnly)
                .WithDocumentServiceProvider(documentServiceProvider);

            using (_project._gate.DisposableWait())
            {
                if (_sourceTextContainersToDocumentIds.ContainsKey(textContainer))
                {
                    throw new ArgumentException($"{nameof(textContainer)} is already added to this project.", nameof(textContainer));
                }

                if (fullPath != null)
                {
                    if (_documentPathsToDocumentIds.ContainsKey(fullPath))
                    {
                        throw new ArgumentException($"'{fullPath}' has already been added to this project.");
                    }

                    _documentPathsToDocumentIds.Add(fullPath, documentId);
                }

                _sourceTextContainersToDocumentIds = _sourceTextContainersToDocumentIds.Add(textContainer, documentInfo.Id);

                if (_project._activeBatchScopes > 0)
                {
                    _documentsAddedInBatch.Add(documentInfo);
                }
                else
                {
                    _project._projectSystemProjectFactory.ApplyChangeToWorkspace(w =>
                    {
                        _project._projectSystemProjectFactory.AddDocumentToDocumentsNotFromFiles_NoLock(documentInfo.Id);
                        _documentAddAction(w, documentInfo);
                        w.OnDocumentOpened(documentInfo.Id, textContainer);
                    });
                }
            }

            return documentId;
        }

        public void AddDynamicFile_NoLock(IDynamicFileInfoProvider fileInfoProvider, DynamicFileInfo fileInfo, ImmutableArray<string> folders)
        {
            Debug.Assert(_project._gate.CurrentCount == 0);

            var documentInfo = CreateDocumentInfoFromFileInfo(fileInfo, folders.NullToEmpty());

            // Generally, DocumentInfo.FilePath can be null, but we always have file paths for dynamic files.
            Contract.ThrowIfNull(documentInfo.FilePath);
            var documentId = documentInfo.Id;

            var filePath = documentInfo.FilePath;
            if (_documentPathsToDocumentIds.ContainsKey(filePath))
            {
                throw new ArgumentException($"'{filePath}' has already been added to this project.", nameof(filePath));
            }

            // If we have an ordered document ids batch, we need to add the document id to the end of it as well.
            _orderedDocumentsInBatch = _orderedDocumentsInBatch?.Add(documentId);

            _documentPathsToDocumentIds.Add(filePath, documentId);

            _documentIdToDynamicFileInfoProvider.Add(documentId, fileInfoProvider);

            if (_project._dynamicFileInfoProvidersSubscribedTo.Add(fileInfoProvider))
            {
                // subscribe to the event when we use this provider the first time
                fileInfoProvider.Updated += _project.OnDynamicFileInfoUpdated;
            }

            if (_project._activeBatchScopes > 0)
            {
                _documentsAddedInBatch.Add(documentInfo);
            }
            else
            {
                // right now, assumption is dynamically generated file can never be opened in editor
                _project._projectSystemProjectFactory.ApplyChangeToWorkspace(w => _documentAddAction(w, documentInfo));
            }
        }

        public IDynamicFileInfoProvider RemoveDynamicFile_NoLock(string fullPath)
        {
            Debug.Assert(_project._gate.CurrentCount == 0);

            if (string.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentException($"{nameof(fullPath)} isn't a valid path.", nameof(fullPath));
            }

            if (!_documentPathsToDocumentIds.TryGetValue(fullPath, out var documentId) ||
                !_documentIdToDynamicFileInfoProvider.TryGetValue(documentId, out var fileInfoProvider))
            {
                throw new ArgumentException($"'{fullPath}' is not a dynamic file of this project.");
            }

            _documentIdToDynamicFileInfoProvider.Remove(documentId);

            RemoveFileInternal(documentId, fullPath);

            return fileInfoProvider;
        }

        public void RemoveFile(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentException($"{nameof(fullPath)} isn't a valid path.", nameof(fullPath));
            }

            using (_project._gate.DisposableWait())
            {
                if (!_documentPathsToDocumentIds.TryGetValue(fullPath, out var documentId))
                {
                    throw new ArgumentException($"'{fullPath}' is not a source file of this project.");
                }

                _project._documentWatchedFiles[documentId].Dispose();
                _project._documentWatchedFiles.Remove(documentId);

                RemoveFileInternal(documentId, fullPath);
            }
        }

        private void RemoveFileInternal(DocumentId documentId, string fullPath)
        {
            _orderedDocumentsInBatch = _orderedDocumentsInBatch?.Remove(documentId);
            _documentPathsToDocumentIds.Remove(fullPath);

            // There are two cases:
            // 
            // 1. This file is actually been pushed to the workspace, and we need to remove it (either
            //    as a part of the active batch or immediately)
            // 2. It hasn't been pushed yet, but is contained in _documentsAddedInBatch
            if (_documentAlreadyInWorkspace(_project._projectSystemProjectFactory.Workspace.CurrentSolution, documentId))
            {
                if (_project._activeBatchScopes > 0)
                {
                    _documentsRemovedInBatch.Add(documentId);
                }
                else
                {
                    _project._projectSystemProjectFactory.ApplyChangeToWorkspace(w => _documentRemoveAction(w, documentId));
                }
            }
            else
            {
                for (var i = 0; i < _documentsAddedInBatch.Count; i++)
                {
                    if (_documentsAddedInBatch[i].Id == documentId)
                    {
                        _documentsAddedInBatch.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public void RemoveTextContainer(SourceTextContainer textContainer)
        {
            if (textContainer == null)
            {
                throw new ArgumentNullException(nameof(textContainer));
            }

            using (_project._gate.DisposableWait())
            {
                if (!_sourceTextContainersToDocumentIds.TryGetValue(textContainer, out var documentId))
                {
                    throw new ArgumentException($"{nameof(textContainer)} is not a text container added to this project.");
                }

                _sourceTextContainersToDocumentIds = _sourceTextContainersToDocumentIds.RemoveKey(textContainer);

                // if the TextContainer had a full path provided, remove it from the map.
                var entry = _documentPathsToDocumentIds.Where(kv => kv.Value == documentId).FirstOrDefault();
                if (entry.Key != null)
                {
                    _documentPathsToDocumentIds.Remove(entry.Key);
                }

                // There are two cases:
                // 
                // 1. This file is actually been pushed to the workspace, and we need to remove it (either
                //    as a part of the active batch or immediately)
                // 2. It hasn't been pushed yet, but is contained in _documentsAddedInBatch
                if (_project._projectSystemProjectFactory.Workspace.CurrentSolution.GetDocument(documentId) != null)
                {
                    if (_project._activeBatchScopes > 0)
                    {
                        _documentsRemovedInBatch.Add(documentId);
                    }
                    else
                    {
                        _project._projectSystemProjectFactory.ApplyChangeToWorkspace(w =>
                        {
                            // Just pass null for the filePath, since this document is immediately being removed
                            // anyways -- whatever we set won't really be read since the next change will
                            // come through.
                            // TODO: Can't we just remove the document without closing it?
                            w.OnDocumentClosed(documentId, new SourceTextLoader(textContainer, filePath: null));
                            _documentRemoveAction(w, documentId);
                            _project._projectSystemProjectFactory.RemoveDocumentToDocumentsNotFromFiles_NoLock(documentId);
                        });
                    }
                }
                else
                {
                    for (var i = 0; i < _documentsAddedInBatch.Count; i++)
                    {
                        if (_documentsAddedInBatch[i].Id == documentId)
                        {
                            _documentsAddedInBatch.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        public bool ContainsFile(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentException($"{nameof(fullPath)} isn't a valid path.", nameof(fullPath));
            }

            using (_project._gate.DisposableWait())
            {
                return _documentPathsToDocumentIds.ContainsKey(fullPath);
            }
        }

        public async ValueTask ProcessRegularFileChangesAsync(ImmutableSegmentedList<string> filePaths)
        {
            using (await _project._gate.DisposableWaitAsync().ConfigureAwait(false))
            {
                // If our project has already been removed, this is a stale notification, and we can disregard.
                if (_project.HasBeenRemoved)
                {
                    return;
                }

                var documentsToChange = ArrayBuilder<(DocumentId, TextLoader)>.GetInstance(filePaths.Count);

                foreach (var filePath in filePaths)
                {
                    if (_documentPathsToDocumentIds.TryGetValue(filePath, out var documentId))
                    {
                        // We create file watching prior to pushing the file to the workspace in batching, so it's
                        // possible we might see a file change notification early. In this case, toss it out. Since
                        // all adds/removals of documents for this project happen under our lock, it's safe to do this
                        // check without taking the main workspace lock. We don't have to check for documents removed in
                        // the batch, since those have already been removed out of _documentPathsToDocumentIds.
                        if (!_documentsAddedInBatch.Any(d => d.Id == documentId))
                        {
                            documentsToChange.Add((documentId, new WorkspaceFileTextLoader(_project._projectSystemProjectFactory.SolutionServices, filePath, defaultEncoding: null)));
                        }
                    }
                }

                // Nothing actually matched, so we're done
                if (documentsToChange.Count == 0)
                {
                    return;
                }

                await _project._projectSystemProjectFactory.ApplyBatchChangeToWorkspaceAsync((solutionChanges, projectUpdateState) =>
                {
                    foreach (var (documentId, textLoader) in documentsToChange)
                    {
                        if (!_project._projectSystemProjectFactory.Workspace.IsDocumentOpen(documentId))
                        {
                            solutionChanges.UpdateSolutionForDocumentAction(
                                _documentTextLoaderChangedAction(solutionChanges.Solution, documentId, textLoader),
                                _documentChangedWorkspaceKind,
                                [documentId]);
                        }
                    }

                    return projectUpdateState;
                }, onAfterUpdateAlways: null).ConfigureAwait(false);

                documentsToChange.Free();
            }
        }

        /// <summary>
        /// Process file content changes
        /// </summary>
        /// <param name="projectSystemFilePath">File path given from project system for the .cshtml file</param>
        /// <param name="workspaceFilePath">File path for the equivalent .cs document used in workspace. it might be different than projectSystemFilePath.</param>
        public void ProcessDynamicFileChange(string projectSystemFilePath, string workspaceFilePath)
        {
            InterlockedOperations.Initialize(ref _dynamicFilesToRefresh, () =>
            {
                return new AsyncBatchingWorkQueue<(string, string)>(
                    TimeSpan.FromMilliseconds(200), // 200 chosen with absolutely no evidence whatsoever
                    ProcessDynamicFileChangesAsync,
                    EqualityComparer<(string, string)>.Default, // uses ordinal string comparison which is what we want
                    _project._projectSystemProjectFactory.WorkspaceListener,
                    _project._asynchronousFileChangeProcessingCancellationTokenSource.Token);
            });

            _dynamicFilesToRefresh.AddWork((projectSystemFilePath, workspaceFilePath));
        }

        private async ValueTask ProcessDynamicFileChangesAsync(ImmutableSegmentedList<(string projectSystemFilePath, string workspaceFilePath)> batch, CancellationToken cancellationToken)
        {
            foreach (var (projectSystemPath, workspaceFilePath) in batch)
            {
                DocumentId? documentId;
                IDynamicFileInfoProvider? fileInfoProvider;

                using (await _project._gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // If our project has already been removed, and we can disregard everything and just 'return'
                    if (_project.HasBeenRemoved)
                        return;

                    // For everything else, if it's not here 'continue' to the next item in the batch.
                    if (!_documentPathsToDocumentIds.TryGetValue(workspaceFilePath, out documentId))
                        continue;

                    if (!_documentIdToDynamicFileInfoProvider.TryGetValue(documentId, out fileInfoProvider))
                        continue;
                }

                // Now that we've got all our basic data, let's fetch the new document outside the lock, since this could be expensive.
                var fileInfo = await fileInfoProvider.GetDynamicFileInfoAsync(
                    _project.Id, _project._filePath, projectSystemPath, CancellationToken.None).ConfigureAwait(false);
                Contract.ThrowIfNull(fileInfo, "We previously received a dynamic file for this path, and we're responding to a change, so we expect to get a new one.");

                await _project._projectSystemProjectFactory.ApplyChangeToWorkspaceAsync(w =>
                    {
                        if (w.IsDocumentOpen(documentId))
                        {
                            return;
                        }

                        // Right now we're only supporting dynamic files as actual source files, so it's OK to call GetDocument here.
                        // If the document is longer present, that could mean we unloaded the project, or the dynamic file was removed while we had released the lock.
                        var documentToReload = w.CurrentSolution.GetDocument(documentId);

                        if (documentToReload is null)
                            return;

                        var documentInfo = new DocumentInfo(documentToReload.State.Attributes, fileInfo.TextLoader, fileInfo.DocumentServiceProvider);

                        w.OnDocumentReloaded(documentInfo);
                    }, cancellationToken).ConfigureAwait(false);
            }
        }

        public void ReorderFiles(ImmutableArray<string> filePaths)
        {
            if (filePaths.IsEmpty)
            {
                throw new ArgumentOutOfRangeException("The specified files are empty.", nameof(filePaths));
            }

            using (_project._gate.DisposableWait())
            {
                if (_documentPathsToDocumentIds.Count != filePaths.Length)
                {
                    throw new ArgumentException("The specified files do not equal the project document count.", nameof(filePaths));
                }

                var documentIds = ImmutableList.CreateBuilder<DocumentId>();

                foreach (var filePath in filePaths)
                {
                    if (_documentPathsToDocumentIds.TryGetValue(filePath, out var documentId))
                    {
                        documentIds.Add(documentId);
                    }
                    else
                    {
                        throw new InvalidOperationException($"The file '{filePath}' does not exist in the project.");
                    }
                }

                if (_project._activeBatchScopes > 0)
                {
                    _orderedDocumentsInBatch = documentIds.ToImmutable();
                }
                else
                {
                    _project._projectSystemProjectFactory.ApplyChangeToWorkspace(_project.Id, solution => solution.WithProjectDocumentsOrder(_project.Id, documentIds.ToImmutable()));
                }
            }
        }

        /// <summary>
        /// Updates the solution for a set of batch changes.
        /// While it is OK for this method to *read* local state, it cannot *modify* it as this may
        /// be called multiple times (when the workspace update fails due to interceding updates).
        /// </summary>
        internal ImmutableArray<(DocumentId documentId, SourceTextContainer textContainer)> UpdateSolutionForBatch(
            SolutionChangeAccumulator solutionChanges,
            ImmutableArray<string>.Builder documentFileNamesAdded,
            Func<Solution, ImmutableArray<DocumentInfo>, Solution> addDocuments,
            WorkspaceChangeKind addDocumentChangeKind,
            Func<Solution, ImmutableArray<DocumentId>, Solution> removeDocuments,
            WorkspaceChangeKind removeDocumentChangeKind)
        {
            // Intentionally making copies to pass into the static update function.
            // State is cleared at the end once the solution changes are actually applied via ClearBatchState.
            return UpdateSolutionForBatch(solutionChanges, documentFileNamesAdded, addDocuments,
                addDocumentChangeKind, removeDocuments, removeDocumentChangeKind, _project.Id, _documentsAddedInBatch.ToImmutableArray(),
                [.. _documentsRemovedInBatch], _orderedDocumentsInBatch,
                documentId => _sourceTextContainersToDocumentIds.GetKeyOrDefault(documentId));

            static ImmutableArray<(DocumentId documentId, SourceTextContainer textContainer)> UpdateSolutionForBatch(
                SolutionChangeAccumulator solutionChanges,
                ImmutableArray<string>.Builder documentFileNamesAdded,
                Func<Solution, ImmutableArray<DocumentInfo>, Solution> addDocuments,
                WorkspaceChangeKind addDocumentChangeKind,
                Func<Solution, ImmutableArray<DocumentId>, Solution> removeDocuments,
                WorkspaceChangeKind removeDocumentChangeKind,
                ProjectId projectId,
                ImmutableArray<DocumentInfo> documentsAddedInBatch,
                ImmutableArray<DocumentId> documentsRemovedInBatch,
                ImmutableList<DocumentId>? orderedDocumentsInBatch,
                Func<DocumentId, SourceTextContainer?> getContainer)
            {
                using var _ = ArrayBuilder<(DocumentId documentId, SourceTextContainer textContainer)>.GetInstance(out var documentsToOpen);

                // Document adding...
                solutionChanges.UpdateSolutionForDocumentAction(
                    newSolution: addDocuments(solutionChanges.Solution, documentsAddedInBatch),
                    changeKind: addDocumentChangeKind,
                    documentIds: documentsAddedInBatch.Select(d => d.Id));

                foreach (var documentInfo in documentsAddedInBatch)
                {
                    Contract.ThrowIfNull(documentInfo.FilePath, "We shouldn't be adding documents without file paths.");
                    documentFileNamesAdded.Add(documentInfo.FilePath);

                    var textContainer = getContainer(documentInfo.Id);
                    if (textContainer != null)
                    {
                        documentsToOpen.Add((documentInfo.Id, textContainer));
                    }
                }

                // Document removing...
                solutionChanges.UpdateSolutionForRemovedDocumentAction(removeDocuments(solutionChanges.Solution, documentsRemovedInBatch),
                    removeDocumentChangeKind,
                    documentsRemovedInBatch);

                // Update project's order of documents.
                if (orderedDocumentsInBatch != null)
                {
                    solutionChanges.UpdateSolutionForProjectAction(
                        projectId,
                        solutionChanges.Solution.WithProjectDocumentsOrder(projectId, orderedDocumentsInBatch));
                }

                return documentsToOpen.ToImmutable();
            }
        }

        internal void ClearBatchState()
        {
            ClearAndZeroCapacity(_documentsAddedInBatch);
            ClearAndZeroCapacity(_documentsRemovedInBatch);
            _orderedDocumentsInBatch = null;
        }

        private DocumentInfo CreateDocumentInfoFromFileInfo(DynamicFileInfo fileInfo, ImmutableArray<string> folders)
        {
            Contract.ThrowIfTrue(folders.IsDefault);

            // we use this file path for editorconfig. 
            var filePath = fileInfo.FilePath;

            var name = FileNameUtilities.GetFileName(filePath);
            var documentId = DocumentId.CreateNewId(_project.Id, filePath);

            return DocumentInfo.Create(
                documentId,
                name,
                folders: folders,
                sourceCodeKind: fileInfo.SourceCodeKind,
                loader: fileInfo.TextLoader,
                filePath: filePath,
                isGenerated: false)
                .WithDesignTimeOnly(true)
                .WithDocumentServiceProvider(fileInfo.DocumentServiceProvider);
        }

        private sealed class SourceTextLoader : TextLoader
        {
            private readonly SourceTextContainer _textContainer;
            private readonly string? _filePath;

            public SourceTextLoader(SourceTextContainer textContainer, string? filePath)
            {
                _textContainer = textContainer;
                _filePath = filePath;
            }

            public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
                => Task.FromResult(TextAndVersion.Create(_textContainer.CurrentText, VersionStamp.Create(), _filePath));
        }
    }
}
