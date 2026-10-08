// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        private readonly Dictionary<string, DocumentEntry> _documentPathsToDocuments = new(StringComparer.OrdinalIgnoreCase);

        /// <param name="IsVirtual">True if the document is backed by a <see cref="SourceTextContainer"/> instead of a file on disk.</param>
        private readonly record struct DocumentEntry(DocumentId Id, bool IsVirtual);

        /// <summary>
        /// The current list of documents that are to be added in this batch.
        /// </summary>
        private readonly ImmutableArray<DocumentInfo>.Builder _documentsAddedInBatch = ImmutableArray.CreateBuilder<DocumentInfo>();

        /// <summary>
        /// The current list of documents that are being removed in this batch. Once the document is in this list, it is no longer in <see cref="_documentPathsToDocuments"/>.
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

            return AddDocument(
                fullPath,
                sourceCodeKind,
                folders,
                _project._projectSystemProjectFactory.CreateFileTextLoader(fullPath),
                designTimeOnly: false,
                documentServiceProvider: null);
        }

        private DocumentId AddDocument(
            string fullPath,
            SourceCodeKind sourceCodeKind,
            ImmutableArray<string> folders,
            TextLoader textLoader,
            bool designTimeOnly,
            IDocumentServiceProvider? documentServiceProvider)
        {
            var isVirtual = textLoader is VirtualDocumentSourceTextLoader;
            var documentId = DocumentId.CreateNewId(_project.Id, fullPath);
            var documentInfo = DocumentInfo.Create(
                documentId,
                name: FileNameUtilities.GetFileName(fullPath),
                folders: folders.IsDefault ? null : folders,
                sourceCodeKind: sourceCodeKind,
                loader: textLoader,
                filePath: fullPath)
                .WithDesignTimeOnly(designTimeOnly)
                .WithDocumentServiceProvider(documentServiceProvider);

            using (_project._gate.DisposableWait())
            {
                if (_documentPathsToDocuments.ContainsKey(fullPath))
                {
                    throw new ArgumentException($"'{fullPath}' has already been added to this project.", nameof(fullPath));
                }

                // If we have an ordered document ids batch, we need to add the document id to the end of it as well.
                _orderedDocumentsInBatch = _orderedDocumentsInBatch?.Add(documentId);

                _documentPathsToDocuments.Add(fullPath, new DocumentEntry(documentId, isVirtual));

                // Virtual documents are not backed by a file on disk, so there is nothing to watch.
                if (!isVirtual)
                    _project._documentWatchedFiles.Add(documentId, _project._documentFileChangeContext.EnqueueWatchingFile(fullPath));

                if (_project._activeBatchScopes > 0)
                {
                    _documentsAddedInBatch.Add(documentInfo);
                }
                else if (isVirtual)
                {
                    _project._projectSystemProjectFactory.ApplyChangeToWorkspace(w =>
                    {
                        _project._projectSystemProjectFactory.AddDocumentToDocumentsNotFromFiles_NoLock(documentInfo.Id);
                        _documentAddAction(w, documentInfo);
                        if (ShouldOpenVirtualDocument(documentInfo, out var container))
                            w.OnDocumentOpened(documentInfo.Id, container);
                    });
                }
                else
                {
                    _project._projectSystemProjectFactory.ApplyChangeToWorkspace(w => _documentAddAction(w, documentInfo));
                    _project._projectSystemProjectFactory.RaiseOnDocumentsAddedMaybeAsync(useAsync: false, [fullPath]).VerifyCompleted();
                }
            }

            return documentId;
        }

        public DocumentId AddVirtualDocument(
            SourceTextContainer textContainer,
            string fullPath,
            SourceCodeKind sourceCodeKind,
            ImmutableArray<string> folders,
            bool designTimeOnly,
            bool openDocument,
            IDocumentServiceProvider? documentServiceProvider)
        {
            if (textContainer == null)
            {
                throw new ArgumentNullException(nameof(textContainer));
            }

            return AddDocument(
                fullPath,
                sourceCodeKind,
                folders,
                new VirtualDocumentSourceTextLoader(textContainer, fullPath, openDocument),
                designTimeOnly,
                documentServiceProvider);
        }

        public void RemoveFile(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentException($"{nameof(fullPath)} isn't a valid path.", nameof(fullPath));
            }

            using (_project._gate.DisposableWait())
            {
                if (!_documentPathsToDocuments.TryGetValue(fullPath, out var entry) || entry.IsVirtual)
                {
                    throw new ArgumentException($"'{fullPath}' is not a source file of this project.");
                }

                RemoveDocument_NoLock(fullPath, entry);
            }
        }

        private void RemoveDocument_NoLock(string fullPath, DocumentEntry entry)
        {
            var (documentId, isVirtual) = entry;

            if (_project._documentWatchedFiles.TryGetValue(documentId, out var watchedFile))
            {
                watchedFile.Dispose();
                _project._documentWatchedFiles.Remove(documentId);
            }

            _orderedDocumentsInBatch = _orderedDocumentsInBatch?.Remove(documentId);
            _documentPathsToDocuments.Remove(fullPath);

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
                    _project._projectSystemProjectFactory.ApplyChangeToWorkspace(w =>
                    {
                        _documentRemoveAction(w, documentId);
                        if (isVirtual)
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

        public void RemoveVirtualDocument(DocumentId documentId)
        {
            using (_project._gate.DisposableWait())
            {
                var (fullPath, entry) = _documentPathsToDocuments.FirstOrDefault(kv => kv.Value.Id == documentId && kv.Value.IsVirtual);
                if (fullPath is null)
                {
                    throw new ArgumentException(
                        "The document is not a virtual document of this project, or has already been removed.",
                        nameof(documentId));
                }

                RemoveDocument_NoLock(fullPath, entry);
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
                return _documentPathsToDocuments.ContainsKey(fullPath);
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
                    if (_documentPathsToDocuments.TryGetValue(filePath, out var entry))
                    {
                        var documentId = entry.Id;

                        // We create file watching prior to pushing the file to the workspace in batching, so it's
                        // possible we might see a file change notification early. In this case, toss it out. Since
                        // all adds/removals of documents for this project happen under our lock, it's safe to do this
                        // check without taking the main workspace lock. We don't have to check for documents removed in
                        // the batch, since those have already been removed out of _documentPathsToDocuments.
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

        public void ReorderFiles(ImmutableArray<string> filePaths)
        {
            if (filePaths.IsEmpty)
            {
                throw new ArgumentOutOfRangeException("The specified files are empty.", nameof(filePaths));
            }

            using (_project._gate.DisposableWait())
            {
                if (_documentPathsToDocuments.Count != filePaths.Length)
                {
                    throw new ArgumentException("The specified files do not equal the project document count.", nameof(filePaths));
                }

                var documentIds = ImmutableList.CreateBuilder<DocumentId>();

                foreach (var filePath in filePaths)
                {
                    if (_documentPathsToDocuments.TryGetValue(filePath, out var entry))
                    {
                        documentIds.Add(entry.Id);
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
                [.. _documentsRemovedInBatch], _orderedDocumentsInBatch);

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
                ImmutableList<DocumentId>? orderedDocumentsInBatch)
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

                    // Virtual documents don't need the host to check whether the file is already open.
                    if (documentInfo.TextLoader is not VirtualDocumentSourceTextLoader)
                        documentFileNamesAdded.Add(documentInfo.FilePath);

                    if (ShouldOpenVirtualDocument(documentInfo, out var textContainer))
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

        private static bool ShouldOpenVirtualDocument(DocumentInfo documentInfo, [NotNullWhen(true)] out SourceTextContainer? sourceTextContainer)
        {
            if (documentInfo.TextLoader is VirtualDocumentSourceTextLoader loader && loader.OpenDocument)
            {
                sourceTextContainer = loader.TextContainer;
                return true;
            }

            sourceTextContainer = null;
            return false;
        }

        private sealed class VirtualDocumentSourceTextLoader : TextLoader
        {
            internal readonly SourceTextContainer TextContainer;
            internal readonly bool OpenDocument;
            private readonly string? _filePath;

            public VirtualDocumentSourceTextLoader(SourceTextContainer textContainer, string? filePath, bool openDocument)
            {
                TextContainer = textContainer;
                _filePath = filePath;
                OpenDocument = openDocument;
            }

            public override async Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
                => TextAndVersion.Create(TextContainer.CurrentText, VersionStamp.Create(), _filePath);
        }
    }
}
