// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioWorkspaceImpl
    {
        /// <summary>
        /// Singleton the subscribes to the running document table and connects/disconnects files to files that are opened.
        /// </summary>
        public sealed class OpenFileTracker : IRunningDocumentTableEventListener
        {
            private readonly ForegroundThreadAffinitizedObject _foregroundAffinitization;

            private readonly VisualStudioWorkspaceImpl _workspace;
            private readonly IAsynchronousOperationListener _asyncOperationListener;

            private readonly RunningDocumentTableEventTracker _runningDocumentTableEventTracker;

            #region Fields read/written to from multiple threads to track files that need to be checked

            /// <summary>
            /// A object to be used for a gate for modifications to <see cref="_fileNamesToCheckForOpenDocuments"/>,
            /// <see cref="_justEnumerateTheEntireRunningDocumentTable"/> and <see cref="_taskPending"/>. These are the only mutable fields
            /// in this class that are modified from multiple threads.
            /// </summary>
            private readonly object _gate = new object();
            private HashSet<string> _fileNamesToCheckForOpenDocuments;

            /// <summary>
            /// Tracks whether we have decided to just scan the entire running document table for files that might already be in the workspace rather than checking
            /// each file one-by-one. This starts out at true, because we are created asynchronously, and files might have already been added to the workspace
            /// that we never got a call to <see cref="QueueCheckForFilesBeingOpen(ImmutableArray{string})"/> for.
            /// </summary>
            private bool _justEnumerateTheEntireRunningDocumentTable = true;

            private bool _taskPending;

            #endregion

            #region Fields read/and written to only on the UI thread to track active context for files

            private readonly ReferenceCountedDisposableCache<IVsHierarchy, HierarchyEventSink> _hierarchyEventSinkCache = new ReferenceCountedDisposableCache<IVsHierarchy, HierarchyEventSink>();

            /// <summary>
            /// The IVsHierarchies we have subscribed to to watch for any changes to this moniker. We track this per moniker, so
            /// when a document is closed we know what we have to incrementally unsubscribe from rather than having to unsubscribe from everything.
            /// </summary>
            private readonly MultiDictionary<string, IReferenceCountedDisposable<ICacheEntry<IVsHierarchy, HierarchyEventSink>>> _watchedHierarchiesForDocumentMoniker
                = new MultiDictionary<string, IReferenceCountedDisposable<ICacheEntry<IVsHierarchy, HierarchyEventSink>>>();

            #endregion

            /// <summary>
            /// A cutoff to use when we should stop checking the RDT for individual documents and just rescan all open documents.
            /// </summary>
            /// <remarks>If a single document is added to a project, we need to check if it's already open. We can easily do
            /// that by calling <see cref="IVsRunningDocumentTable4.GetDocumentCookie(string)"/> and going from there. That's fine
            /// for a few documents, but is not wise during solution load when you have potentially thousands of files. In that
            /// case, we can just enumerate all open files and check if we know about them, on the assumption the number of
            /// open files is far less than the number of total files.
            /// 
            /// This cutoff of 10 was chosen arbitrarily and with no evidence whatsoever.</remarks>
            private const int CutoffForCheckingAllRunningDocumentTableDocuments = 10;

            private OpenFileTracker(VisualStudioWorkspaceImpl workspace, IVsRunningDocumentTable runningDocumentTable, IComponentModel componentModel)
            {
                _workspace = workspace;
                _foregroundAffinitization = new ForegroundThreadAffinitizedObject(workspace._threadingContext, assertIsForeground: true);
                _asyncOperationListener = componentModel.GetService<IAsynchronousOperationListenerProvider>().GetListener(FeatureAttribute.Workspace);
                _runningDocumentTableEventTracker = new RunningDocumentTableEventTracker(workspace._threadingContext,
                    componentModel.GetService<IVsEditorAdaptersFactoryService>(), runningDocumentTable, this);
            }

            void IRunningDocumentTableEventListener.OnOpenDocument(string moniker, ITextBuffer textBuffer, IVsHierarchy hierarchy)
                => TryOpeningDocumentsForMoniker(moniker, textBuffer, hierarchy);

            void IRunningDocumentTableEventListener.OnCloseDocument(string moniker)
                => TryClosingDocumentsForMoniker(moniker);

            void IRunningDocumentTableEventListener.OnRefreshDocumentContext(string moniker, IVsHierarchy hierarchy)
                => RefreshContextForMoniker(moniker, hierarchy);

            /// <summary>
            /// When a file is renamed, the old document is removed and a new document is added by the workspace.
            /// </summary>
            void IRunningDocumentTableEventListener.OnRenameDocument(string newMoniker, string oldMoniker, ITextBuffer buffer)
            {
            }

            public async static Task<OpenFileTracker> CreateAsync(VisualStudioWorkspaceImpl workspace, IAsyncServiceProvider asyncServiceProvider)
            {
                var runningDocumentTable = (IVsRunningDocumentTable)await asyncServiceProvider.GetServiceAsync(typeof(SVsRunningDocumentTable)).ConfigureAwait(true);
                var componentModel = (IComponentModel)await asyncServiceProvider.GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(true);

                return new OpenFileTracker(workspace, runningDocumentTable, componentModel);
            }

            private void TryOpeningDocumentsForMoniker(string moniker, ITextBuffer textBuffer, IVsHierarchy hierarchy)
            {
                _foregroundAffinitization.AssertIsForeground();

                _workspace.ApplyChangeToWorkspace(w =>
                {
                    var documentIds = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(moniker);
                    if (documentIds.IsDefaultOrEmpty)
                    {
                        return;
                    }

                    if (documentIds.All(w.IsDocumentOpen))
                    {
                        return;
                    }

                    ProjectId activeContextProjectId;

                    if (documentIds.Length == 1)
                    {
                        activeContextProjectId = documentIds.Single().ProjectId;
                    }
                    else
                    {
                        activeContextProjectId = GetActiveContextProjectIdAndWatchHierarchies(moniker, documentIds.Select(d => d.ProjectId), hierarchy);
                    }

                    var textContainer = textBuffer.AsTextContainer();

                    foreach (var documentId in documentIds)
                    {
                        if (!w.IsDocumentOpen(documentId) && !_workspace._documentsNotFromFiles.Contains(documentId))
                        {
                            var isCurrentContext = documentId.ProjectId == activeContextProjectId;
                            if (w.CurrentSolution.ContainsDocument(documentId))
                            {
                                w.OnDocumentOpened(documentId, textContainer, isCurrentContext);
                            }
                            else if (w.CurrentSolution.ContainsAdditionalDocument(documentId))
                            {
                                w.OnAdditionalDocumentOpened(documentId, textContainer, isCurrentContext);
                            }
                            else
                            {
                                Debug.Assert(w.CurrentSolution.ContainsAnalyzerConfigDocument(documentId));
                                w.OnAnalyzerConfigDocumentOpened(documentId, textContainer, isCurrentContext);
                            }
                        }
                    }
                });
            }

            private ProjectId GetActiveContextProjectIdAndWatchHierarchies(string moniker, IEnumerable<ProjectId> projectIds, IVsHierarchy hierarchy)
            {
                _foregroundAffinitization.AssertIsForeground();

                // First clear off any existing IVsHierarchies we are watching. Any ones that still matter we will resubscribe to.
                // We could be fancy and diff, but the cost is probably neglible.
                UnsubscribeFromWatchedHierarchies(moniker);

                if (hierarchy == null)
                {
                    // Any item in the RDT should have a hierarchy associated; in this case we don't so there's absolutely nothing
                    // we can do at this point.
                    return projectIds.First();
                }

                void WatchHierarchy(IVsHierarchy hierarchyToWatch)
                {
                    _watchedHierarchiesForDocumentMoniker.Add(moniker, _hierarchyEventSinkCache.GetOrCreate(hierarchyToWatch, h => new HierarchyEventSink(h, this)));
                }

                // Take a snapshot of the immutable data structure here to avoid mutation underneath us
                var projectToHierarchyMap = _workspace._projectToHierarchyMap;
                var solution = _workspace.CurrentSolution;

                // We now must chase to the actual hierarchy that we know about. First, we'll chase through multiple shared asset projects if
                // we need to do so.
                while (true)
                {
                    var contextHierarchy = hierarchy.GetActiveProjectContext();

                    // The check for if contextHierarchy == hierarchy is working around downstream impacts of https://devdiv.visualstudio.com/DevDiv/_git/CPS/pullrequest/158271
                    // Since that bug means shared projects have themselves as their own owner, it sometimes results in us corrupting state where we end up
                    // having the context of shared project be itself, it seems.
                    if (contextHierarchy == null || contextHierarchy == hierarchy)
                    {
                        break;
                    }

                    WatchHierarchy(hierarchy);
                    hierarchy = contextHierarchy;
                }

                // We may have multiple projects with the same hierarchy, but we can use __VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext to distinguish
                if (ErrorHandler.Succeeded(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext, out var contextProjectNameObject)))
                {
                    WatchHierarchy(hierarchy);

                    if (contextProjectNameObject is string contextProjectName)
                    {
                        var project = _workspace.GetProjectWithHierarchyAndName(hierarchy, contextProjectName);

                        if (project != null && projectIds.Contains(project.Id))
                        {
                            return project.Id;
                        }
                    }
                }

                // At this point, we should hopefully have only one project that maches by hierarchy. If there's multiple, at this point we can't figure anything
                // out better.
                var matchingProjectId = projectIds.FirstOrDefault(id => projectToHierarchyMap.GetValueOrDefault(id, null) == hierarchy);

                if (matchingProjectId != null)
                {
                    return matchingProjectId;
                }

                // If we had some trouble finding the project, we'll just pick one arbitrarily
                return projectIds.First();
            }

            private void UnsubscribeFromWatchedHierarchies(string moniker)
            {
                _foregroundAffinitization.AssertIsForeground();

                foreach (var watchedHierarchy in _watchedHierarchiesForDocumentMoniker[moniker])
                {
                    watchedHierarchy.Dispose();
                }

                _watchedHierarchiesForDocumentMoniker.Remove(moniker);
            }

            private void RefreshContextForMoniker(string moniker, IVsHierarchy hierarchy)
            {
                _foregroundAffinitization.AssertIsForeground();

                _workspace.ApplyChangeToWorkspace(w =>
                {
                    var documentIds = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(moniker);
                    if (documentIds.IsDefaultOrEmpty || documentIds.Length == 1)
                    {
                        return;
                    }

                    if (!documentIds.All(w.IsDocumentOpen))
                    {
                        return;
                    }

                    var activeProjectId = GetActiveContextProjectIdAndWatchHierarchies(moniker, documentIds.Select(d => d.ProjectId), hierarchy);
                    w.OnDocumentContextUpdated(documentIds.FirstOrDefault(d => d.ProjectId == activeProjectId));
                });
            }

            private void RefreshContextsForHierarchyPropertyChange(IVsHierarchy hierarchy)
            {
                _foregroundAffinitization.AssertIsForeground();

                // We're going to go through each file that has subscriptions, and update them appropriately.
                // We have to clone this since we will be modifying it under the covers.
                foreach (var moniker in _watchedHierarchiesForDocumentMoniker.Keys.ToList())
                {
                    foreach (var subscribedHierarchy in _watchedHierarchiesForDocumentMoniker[moniker])
                    {
                        if (subscribedHierarchy.Target.Key == hierarchy)
                        {
                            RefreshContextForMoniker(moniker, hierarchy);
                        }
                    }
                }
            }

            private void TryClosingDocumentsForMoniker(string moniker)
            {
                _foregroundAffinitization.AssertIsForeground();

                UnsubscribeFromWatchedHierarchies(moniker);

                _workspace.ApplyChangeToWorkspace(w =>
                {
                    var documentIds = w.CurrentSolution.GetDocumentIdsWithFilePath(moniker);
                    if (documentIds.IsDefaultOrEmpty)
                    {
                        return;
                    }

                    foreach (var documentId in documentIds)
                    {
                        if (w.IsDocumentOpen(documentId) && !_workspace._documentsNotFromFiles.Contains(documentId))
                        {
                            if (w.CurrentSolution.ContainsDocument(documentId))
                            {
                                w.OnDocumentClosed(documentId, new FileTextLoader(moniker, defaultEncoding: null));
                            }
                            else if (w.CurrentSolution.ContainsAdditionalDocument(documentId))
                            {
                                w.OnAdditionalDocumentClosed(documentId, new FileTextLoader(moniker, defaultEncoding: null));
                            }
                            else
                            {
                                Debug.Assert(w.CurrentSolution.ContainsAnalyzerConfigDocument(documentId));
                                w.OnAnalyzerConfigDocumentClosed(documentId, new FileTextLoader(moniker, defaultEncoding: null));
                            }
                        }
                    }
                });
            }

            /// <summary>
            /// Queues a new task to check for files being open for these file names.
            /// </summary>
            public void QueueCheckForFilesBeingOpen(ImmutableArray<string> newFileNames)
            {
                _foregroundAffinitization.ThisCanBeCalledOnAnyThread();

                var shouldStartTask = false;

                lock (_gate)
                {
                    // If we've already decided to enumerate the full table, nothing further to do.
                    if (!_justEnumerateTheEntireRunningDocumentTable)
                    {
                        // If this is going to push us over our threshold for scanning the entire table then just give up
                        if ((_fileNamesToCheckForOpenDocuments?.Count ?? 0) + newFileNames.Length > CutoffForCheckingAllRunningDocumentTableDocuments)
                        {
                            _fileNamesToCheckForOpenDocuments = null;
                            _justEnumerateTheEntireRunningDocumentTable = true;
                        }
                        else
                        {
                            if (_fileNamesToCheckForOpenDocuments == null)
                            {
                                _fileNamesToCheckForOpenDocuments = new HashSet<string>(newFileNames);
                            }
                            else
                            {
                                foreach (var filename in newFileNames)
                                {
                                    _fileNamesToCheckForOpenDocuments.Add(filename);
                                }
                            }
                        }
                    }

                    if (!_taskPending)
                    {
                        _taskPending = true;
                        shouldStartTask = true;
                    }
                }

                if (shouldStartTask)
                {
                    var asyncToken = _asyncOperationListener.BeginAsyncOperation(nameof(QueueCheckForFilesBeingOpen));

                    Task.Run(async () =>
                    {
                        await _foregroundAffinitization.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                        ProcessQueuedWorkOnUIThread();
                    }).CompletesAsyncOperation(asyncToken);
                }
            }

            public void ProcessQueuedWorkOnUIThread()
            {
                _foregroundAffinitization.AssertIsForeground();

                // Just pulling off the values from the shared state to the local function.
                HashSet<string> fileNamesToCheckForOpenDocuments;
                bool justEnumerateTheEntireRunningDocumentTable;
                lock (_gate)
                {
                    fileNamesToCheckForOpenDocuments = _fileNamesToCheckForOpenDocuments;
                    justEnumerateTheEntireRunningDocumentTable = _justEnumerateTheEntireRunningDocumentTable;

                    _fileNamesToCheckForOpenDocuments = null;
                    _justEnumerateTheEntireRunningDocumentTable = false;

                    _taskPending = false;
                }

                if (justEnumerateTheEntireRunningDocumentTable)
                {
                    var documents = _runningDocumentTableEventTracker.EnumerateDocumentSet();
                    foreach (var (moniker, textBuffer, hierarchy) in documents)
                    {
                        TryOpeningDocumentsForMoniker(moniker, textBuffer, hierarchy);
                    }
                }
                else if (fileNamesToCheckForOpenDocuments != null)
                {
                    foreach (var fileName in fileNamesToCheckForOpenDocuments)
                    {
                        if (_runningDocumentTableEventTracker.IsFileOpen(fileName) && _runningDocumentTableEventTracker.TryGetBufferFromMoniker(fileName, out var buffer))
                        {
                            var hierarchy = _runningDocumentTableEventTracker.GetDocumentHierarchy(fileName);
                            TryOpeningDocumentsForMoniker(fileName, buffer, hierarchy);
                        }
                    }
                }
            }

            private class HierarchyEventSink : IVsHierarchyEvents, IDisposable
            {
                private readonly IVsHierarchy _hierarchy;
                private readonly uint _cookie;
                private readonly OpenFileTracker _openFileTracker;

                public HierarchyEventSink(IVsHierarchy hierarchy, OpenFileTracker openFileTracker)
                {
                    _hierarchy = hierarchy;
                    _openFileTracker = openFileTracker;
                    ErrorHandler.ThrowOnFailure(_hierarchy.AdviseHierarchyEvents(this, out _cookie));
                }

                void IDisposable.Dispose()
                {
                    _hierarchy.UnadviseHierarchyEvents(_cookie);
                }

                int IVsHierarchyEvents.OnItemAdded(uint itemidParent, uint itemidSiblingPrev, uint itemidAdded)
                {
                    return VSConstants.E_NOTIMPL;
                }

                int IVsHierarchyEvents.OnItemsAppended(uint itemidParent)
                {
                    return VSConstants.E_NOTIMPL;
                }

                int IVsHierarchyEvents.OnItemDeleted(uint itemid)
                {
                    return VSConstants.E_NOTIMPL;
                }

                int IVsHierarchyEvents.OnPropertyChanged(uint itemid, int propid, uint flags)
                {
                    if (propid == (int)__VSHPROPID7.VSHPROPID_SharedItemContextHierarchy ||
                        propid == (int)__VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext)
                    {
                        _openFileTracker.RefreshContextsForHierarchyPropertyChange(_hierarchy);
                    }

                    return VSConstants.S_OK;
                }

                int IVsHierarchyEvents.OnInvalidateItems(uint itemidParent)
                {
                    return VSConstants.E_NOTIMPL;
                }

                int IVsHierarchyEvents.OnInvalidateIcon(IntPtr hicon)
                {
                    return VSConstants.E_NOTIMPL;
                }
            }
        }
    }
}
