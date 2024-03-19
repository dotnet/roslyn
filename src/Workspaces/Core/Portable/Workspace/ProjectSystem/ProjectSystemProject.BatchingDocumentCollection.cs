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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
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

            if (_project._eventSubscriptionTracker.Add(fileInfoProvider))
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
                            documentsToChange.Add((documentId, new WorkspaceFileTextLoader(_project._projectSystemProjectFactory.Workspace.Services.SolutionServices, filePath, defaultEncoding: null)));
                        }
                    }
                }

                // Nothing actually matched, so we're done
                if (documentsToChange.Count == 0)
                {
                    return;
                }

                await _project._projectSystemProjectFactory.ApplyBatchChangeToWorkspaceAsync(solutionChanges =>
                {
                    foreach (var (documentId, textLoader) in documentsToChange)
                    {
                        if (!_project._projectSystemProjectFactory.Workspace.IsDocumentOpen(documentId))
                        {
                            solutionChanges.UpdateSolutionForDocumentAction(
                                _documentTextLoaderChangedAction(solutionChanges.Solution, documentId, textLoader),
                                _documentChangedWorkspaceKind,
                                SpecializedCollections.SingletonEnumerable(documentId));
                        }
                    }
                }).ConfigureAwait(false);

                documentsToChange.Free();
            }
        }

        /// <summary>
        /// Process file content changes
        /// </summary>
        /// <param name="projectSystemFilePath">filepath given from project system</param>
        /// <param name="workspaceFilePath">filepath used in workspace. it might be different than projectSystemFilePath</param>
        public void ProcessDynamicFileChange(string projectSystemFilePath, string workspaceFilePath)
        {
            using (_project._gate.DisposableWait())
            {
                // If our project has already been removed, this is a stale notification, and we can disregard.
                if (_project.HasBeenRemoved)
                {
                    return;
                }

                if (_documentPathsToDocumentIds.TryGetValue(workspaceFilePath, out var documentId))
                {
                    // We create file watching prior to pushing the file to the workspace in batching, so it's
                    // possible we might see a file change notification early. In this case, toss it out. Since
                    // all adds/removals of documents for this project happen under our lock, it's safe to do this
                    // check without taking the main workspace lock. We don't have to check for documents removed in
                    // the batch, since those have already been removed out of _documentPathsToDocumentIds.
                    if (_documentsAddedInBatch.Any(d => d.Id == documentId))
                    {
                        return;
                    }

                    Contract.ThrowIfFalse(_documentIdToDynamicFileInfoProvider.TryGetValue(documentId, out var fileInfoProvider));

                    _project._projectSystemProjectFactory.ApplyChangeToWorkspace(w =>
                    {
                        if (w.IsDocumentOpen(documentId))
                        {
                            return;
                        }

                        // we do not expect JTF to be used around this code path. and contract of fileInfoProvider is it being real free-threaded
                        // meaning it can't use JTF to go back to UI thread.
                        // so, it is okay for us to call regular ".Result" on a task here.
                        var fileInfo = fileInfoProvider.GetDynamicFileInfoAsync(
                            _project.Id, _project._filePath, projectSystemFilePath, CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);

                        Contract.ThrowIfNull(fileInfo, "We previously received a dynamic file for this path, and we're responding to a change, so we expect to get a new one.");

                        // Right now we're only supporting dynamic files as actual source files, so it's OK to call GetDocument here
                        var attributes = w.CurrentSolution.GetRequiredDocument(documentId).State.Attributes;

                        var documentInfo = new DocumentInfo(attributes, fileInfo.TextLoader, fileInfo.DocumentServiceProvider);

                        w.OnDocumentReloaded(documentInfo);
                    });
                }
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

        internal void UpdateSolutionForBatch(
            SolutionChangeAccumulator solutionChanges,
            ImmutableArray<string>.Builder documentFileNamesAdded,
            List<(DocumentId documentId, SourceTextContainer textContainer)> documentsToOpen,
            Func<Solution, ImmutableArray<DocumentInfo>, Solution> addDocuments,
            WorkspaceChangeKind addDocumentChangeKind,
            Func<Solution, ImmutableArray<DocumentId>, Solution> removeDocuments,
            WorkspaceChangeKind removeDocumentChangeKind)
        {
            // Document adding...
            solutionChanges.UpdateSolutionForDocumentAction(
                newSolution: addDocuments(solutionChanges.Solution, _documentsAddedInBatch.ToImmutable()),
                changeKind: addDocumentChangeKind,
                documentIds: _documentsAddedInBatch.Select(d => d.Id));

            foreach (var documentInfo in _documentsAddedInBatch)
            {
                Contract.ThrowIfNull(documentInfo.FilePath, "We shouldn't be adding documents without file paths.");
                documentFileNamesAdded.Add(documentInfo.FilePath);

                if (_sourceTextContainersToDocumentIds.TryGetKey(documentInfo.Id, out var textContainer))
                {
                    documentsToOpen.Add((documentInfo.Id, textContainer));
                }
            }

            ClearAndZeroCapacity(_documentsAddedInBatch);

            // Document removing...
            solutionChanges.UpdateSolutionForRemovedDocumentAction(removeDocuments(solutionChanges.Solution, _documentsRemovedInBatch.ToImmutableArray()),
                removeDocumentChangeKind,
                _documentsRemovedInBatch);

            ClearAndZeroCapacity(_documentsRemovedInBatch);

            // Update project's order of documents.
            if (_orderedDocumentsInBatch != null)
            {
                solutionChanges.UpdateSolutionForProjectAction(
                    _project.Id,
                    solutionChanges.Solution.WithProjectDocumentsOrder(_project.Id, _orderedDocumentsInBatch));
                _orderedDocumentsInBatch = null;
            }
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
