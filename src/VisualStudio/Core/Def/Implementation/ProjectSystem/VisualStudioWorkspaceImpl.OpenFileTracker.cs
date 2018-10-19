using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioWorkspaceImpl
    {
        /// <summary>
        /// Singleton the subscribes to the running document table and connects/disconnects files to files that are opened.
        /// </summary>
        public sealed class OpenFileTracker
        {
            private readonly ForegroundThreadAffinitizedObject _foregroundAffinitization;

            private readonly VisualStudioWorkspaceImpl _workspace;
            private readonly IVsRunningDocumentTable4 _runningDocumentTable;
            private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
            private readonly IAsynchronousOperationListener _asyncOperationListener;

            #region Fields read/written to from multiple threads to track files that need to be checked

            /// <summary>
            /// A object to be used for a gate for modifications to <see cref="_fileNamesToCheckForOpenDocuments"/>,
            /// <see cref="_justEnumerateTheEntireRunningDocumentTable"/> and <see cref="_taskPending"/>. These are the only mutable fields
            /// in this class that are modified from multiple threads.
            /// </summary>
            private readonly object _gate = new object();
            private HashSet<string> _fileNamesToCheckForOpenDocuments;
            private bool _justEnumerateTheEntireRunningDocumentTable;

            private bool _taskPending;

            #endregion

            #region Fields read/and written to only on the UI thread to track active context for files

            private readonly Dictionary<IVsHierarchy, HierarchyEventSink> _hierarchyEventSinks = new Dictionary<IVsHierarchy, HierarchyEventSink>();

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

            private OpenFileTracker(VisualStudioWorkspaceImpl workspace, IVsRunningDocumentTable4 runningDocumentTable, IComponentModel componentModel)
            {
                _workspace = workspace;
                _foregroundAffinitization = new ForegroundThreadAffinitizedObject(workspace._threadingContext, assertIsForeground: true);
                _runningDocumentTable = runningDocumentTable;
                _editorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
                _asyncOperationListener = componentModel.GetService<IAsynchronousOperationListenerProvider>().GetListener(FeatureAttribute.Workspace);
            }

            public async static Task<OpenFileTracker> CreateAsync(VisualStudioWorkspaceImpl workspace, IAsyncServiceProvider asyncServiceProvider)
            {
                var runningDocumentTable = (IVsRunningDocumentTable4)await asyncServiceProvider.GetServiceAsync(typeof(SVsRunningDocumentTable)).ConfigureAwait(true);
                var componentModel = (IComponentModel)await asyncServiceProvider.GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(true);

                var openFileTracker = new OpenFileTracker(workspace, runningDocumentTable, componentModel);
                openFileTracker.ConnectToRunningDocumentTable();

                return openFileTracker;
            }

            private void ConnectToRunningDocumentTable()
            {
                _foregroundAffinitization.AssertIsForeground();

                // Some methods we need here only exist in IVsRunningDocumentTable and not the IVsRunningDocumentTable4 that we
                // hold onto as a field
                var runningDocumentTable = ((IVsRunningDocumentTable)_runningDocumentTable);
                runningDocumentTable.AdviseRunningDocTableEvents(new RunningDocumentTableEventSink(this), out var docTableEventsCookie);
            }

            public void CheckForOpenDocumentsByEnumeratingTheRunningDocumentTable()
            {
                _foregroundAffinitization.AssertIsForeground();

                lock (_gate)
                {
                    // Since we're scanning the full RDT, we can skip any explicit names we already have queued
                    ClearPendingFilesForBeingOpen_NoLock();
                }

                foreach (var cookie in GetInitializedRunningDocumentTableCookies())
                {
                    TryOpeningDocumentsForNewCookie(cookie);
                }
            }

            private IEnumerable<uint> GetInitializedRunningDocumentTableCookies()
            {
                // Some methods we need here only exist in IVsRunningDocumentTable and not the IVsRunningDocumentTable4 that we
                // hold onto as a field
                var runningDocumentTable = ((IVsRunningDocumentTable)_runningDocumentTable);
                ErrorHandler.ThrowOnFailure(runningDocumentTable.GetRunningDocumentsEnum(out var enumRunningDocuments));
                uint[] cookies = new uint[16];

                while (ErrorHandler.Succeeded(enumRunningDocuments.Next((uint)cookies.Length, cookies, out var cookiesFetched))
                       && cookiesFetched > 0)
                {
                    for (int cookieIndex = 0; cookieIndex < cookiesFetched; cookieIndex++)
                    {
                        var cookie = cookies[cookieIndex];

                        if (_runningDocumentTable.IsDocumentInitialized(cookie))
                        {
                            yield return cookie;
                        }
                    }
                }
            }

            private void TryOpeningDocumentsForNewCookie(uint cookie)
            {
                _foregroundAffinitization.AssertIsForeground();

                if (!_runningDocumentTable.IsDocumentInitialized(cookie))
                {
                    // We never want to touch documents that haven't been initialized yet, so immediately bail. Any further
                    // calls to the RDT might accidentally initialize it.
                    return;
                }

                var moniker = _runningDocumentTable.GetDocumentMoniker(cookie);
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
                        _runningDocumentTable.GetDocumentHierarchyItem(cookie, out var hierarchy, out _);
                        activeContextProjectId = GetActiveContextProjectId(hierarchy, documentIds.Select(d => d.ProjectId));
                    }

                    if ((object)_runningDocumentTable.GetDocumentData(cookie) is IVsTextBuffer bufferAdapter)
                    {
                        var textBuffer = _editorAdaptersFactoryService.GetDocumentBuffer(bufferAdapter);

                        if (textBuffer != null)
                        {
                            var textContainer = textBuffer.AsTextContainer();

                            foreach (var documentId in documentIds)
                            {
                                if (!w.IsDocumentOpen(documentId) && !_workspace._documentsNotFromFiles.Contains(documentId))
                                {
                                    w.OnDocumentOpened(documentId, textContainer, isCurrentContext: documentId.ProjectId == activeContextProjectId);
                                }
                            }
                        }
                    }
                });
            }

            private ProjectId GetActiveContextProjectId(IVsHierarchy hierarchy, IEnumerable<ProjectId> projectIds)
            {
                _foregroundAffinitization.AssertIsForeground();

                if (hierarchy == null)
                {
                    // Any item in the RDT should have a hierarchy associated; in this case we don't so there's absolutely nothing
                    // we can do at this point.
                    return projectIds.First();
                }

                // Take a snapshot of the immutable data structure here to avoid mutation underneath us
                var projectToHierarchyMap = _workspace._projectToHierarchyMap;
                var solution = _workspace.CurrentSolution;

                // We now must chase to the actual hierarchy that we know about. We'll do this as a loop as there may be multiple steps in order.
                // intermediateHierarchy will be where we are so far, and we'll keep track of all of our intermediate steps (think a breadcrumb trail)
                // in intermediateHierarchies.
                var intermediateHierarchy = hierarchy;
                var intermediateHierarchies = new HashSet<IVsHierarchy>();

                while (true)
                {
                    if (!intermediateHierarchies.Add(intermediateHierarchy))
                    {
                        // We ended up somewhere we already were -- either we have a loop or we weren't able to make further progress. In this case,
                        // just bail.
                        break;
                    }

                    // Have we already arrived at a hierarchy we know about?
                    var matchingProjectId = projectToHierarchyMap.FirstOrDefault(d => projectIds.Contains(d.Key) &&
                                                                                      d.Value == intermediateHierarchy).Key;

                    if (matchingProjectId != null)
                    {
                        return matchingProjectId;
                    }

                    // This is some intermediate hierarchy which we need to direct us somewhere else. At this point, we need to add an event sink to be aware if the redirection
                    // ever changes.
                    if (!_hierarchyEventSinks.ContainsKey(hierarchy))
                    {
                        var eventSink = new HierarchyEventSink(intermediateHierarchy, this);
                        if (eventSink.TryAdviseHierarchy())
                        {
                            _hierarchyEventSinks.Add(intermediateHierarchy, eventSink);
                        }
                    }

                    // If this is a shared hierarchy, we can possibly ask it for it's context
                    var contextHierarchy = intermediateHierarchy.GetActiveProjectContext();
                    if (contextHierarchy != null)
                    {
                        intermediateHierarchy = contextHierarchy;
                        continue;
                    }

                    if (ErrorHandler.Succeeded(intermediateHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext, out object contextProjectNameObject)))
                    {
                        if (contextProjectNameObject is string contextProjectName)
                        {
                            var contextProject = solution.Projects.FirstOrDefault(p => p.Name == contextProjectName);
                            if (contextProject != null)
                            {
                                return contextProject.Id;
                            }
                        }
                    }
                }

                // If we had some trouble finding the project, we'll just pick one arbitrarily
                return projectIds.First();
            }

            private void RefreshContextForRunningDocumentTableHierarchyChange(uint cookie)
            {
                _foregroundAffinitization.AssertIsForeground();

                var moniker = _runningDocumentTable.GetDocumentMoniker(cookie);
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

                    _runningDocumentTable.GetDocumentHierarchyItem(cookie, out var hierarchy, out _);
                    var activeProjectId = GetActiveContextProjectId(hierarchy, documentIds.Select(d => d.ProjectId));
                    w.OnDocumentContextUpdated(documentIds.FirstOrDefault(d => d.ProjectId == activeProjectId));
                });
            }

            private void RefreshContextForHierarchyPropertyChange(IVsHierarchy hierarchy)
            {
                // HACK: for now, just refresh all the things. This is expensive
                _foregroundAffinitization.AssertIsForeground();

                foreach (var cookie in GetInitializedRunningDocumentTableCookies())
                {
                    RefreshContextForRunningDocumentTableHierarchyChange(cookie);
                }
            }

            private void TryClosingDocumentsForCookie(uint cookie)
            {
                _foregroundAffinitization.AssertIsForeground();

                if (!_runningDocumentTable.IsDocumentInitialized(cookie))
                {
                    // We never want to touch documents that haven't been initialized yet, so immediately bail. Any further
                    // calls to the RDT might accidentally initialize it.
                    return;
                }

                var moniker = _runningDocumentTable.GetDocumentMoniker(cookie);
                _workspace.ApplyChangeToWorkspace(w =>
                {
                    var documentIds = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(moniker);
                    if (documentIds.IsDefaultOrEmpty)
                    {
                        return;
                    }

                    foreach (var documentId in documentIds)
                    {
                        if (_workspace.IsDocumentOpen(documentId) && !_workspace._documentsNotFromFiles.Contains(documentId))
                        {
                            w.OnDocumentClosed(documentId, new FileTextLoader(moniker, defaultEncoding: null));
                        }
                    }
                });
            }

            /// <summary>
            /// Queues a new task to check for files being open for these file names.
            /// </summary>
            public void CheckForFilesBeingOpen(ImmutableArray<string> newFileNames)
            {
                _foregroundAffinitization.ThisCanBeCalledOnAnyThread();

                bool shouldStartTask = false;

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
                    var asyncToken = _asyncOperationListener.BeginAsyncOperation(nameof(CheckForFilesBeingOpen));

                    Task.Run(async () =>
                    {
                        await _foregroundAffinitization.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                        CheckForFilesBeingOpenOnUIThread();
                    }).CompletesAsyncOperation(asyncToken);
                }
            }

            private void CheckForFilesBeingOpenOnUIThread()
            {
                _foregroundAffinitization.AssertIsForeground();

                // Just pulling off the values from the shared state to the local funtion...
                HashSet<string> fileNamesToCheckForOpenDocuments;
                bool justEnumerateTheEntireRunningDocumentTable;
                lock (_gate)
                {
                    fileNamesToCheckForOpenDocuments = _fileNamesToCheckForOpenDocuments;
                    justEnumerateTheEntireRunningDocumentTable = _justEnumerateTheEntireRunningDocumentTable;

                    ClearPendingFilesForBeingOpen_NoLock();
                }

                if (justEnumerateTheEntireRunningDocumentTable)
                {
                    CheckForOpenDocumentsByEnumeratingTheRunningDocumentTable();
                }
                else
                {
                    foreach (var filename in fileNamesToCheckForOpenDocuments)
                    {
                        if (_runningDocumentTable.IsMonikerValid(filename))
                        {
                            var cookie = _runningDocumentTable.GetDocumentCookie(filename);
                            TryOpeningDocumentsForNewCookie(cookie);
                        }
                    }
                }
            }

            private void ClearPendingFilesForBeingOpen_NoLock()
            {
                _fileNamesToCheckForOpenDocuments = null;
                _justEnumerateTheEntireRunningDocumentTable = false;

                _taskPending = false;
            }

            private class RunningDocumentTableEventSink : IVsRunningDocTableEvents3
            {
                private readonly OpenFileTracker _openFileTracker;

                public RunningDocumentTableEventSink(OpenFileTracker openFileTracker)
                {
                    _openFileTracker = openFileTracker;
                }

                public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
                {
                    return VSConstants.E_NOTIMPL;
                }

                public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
                {
                    if (dwReadLocksRemaining + dwEditLocksRemaining == 0)
                    {
                        _openFileTracker.TryClosingDocumentsForCookie(docCookie);
                    }

                    return VSConstants.S_OK;
                }

                public int OnAfterSave(uint docCookie)
                {
                    return VSConstants.E_NOTIMPL;
                }

                public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
                {
                    return VSConstants.E_NOTIMPL;
                }

                public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
                {
                    if ((grfAttribs & (uint)__VSRDTATTRIB3.RDTA_DocumentInitialized) != 0)
                    {
                        _openFileTracker.TryOpeningDocumentsForNewCookie(docCookie);
                    }

                    if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_Hierarchy) != 0)
                    {
                        _openFileTracker.RefreshContextForRunningDocumentTableHierarchyChange(docCookie);
                    }

                    return VSConstants.S_OK;
                }

                public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
                {
                    if (fFirstShow != 0)
                    {
                        _openFileTracker.TryOpeningDocumentsForNewCookie(docCookie);
                    }

                    return VSConstants.S_OK;
                }

                public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
                {
                    return VSConstants.E_NOTIMPL;
                }

                public int OnBeforeSave(uint docCookie)
                {
                    return VSConstants.E_NOTIMPL;
                }
            }

            private class HierarchyEventSink : IVsHierarchyEvents
            {
                private uint _cookie;
                private readonly IVsHierarchy _hierarchy;
                private readonly OpenFileTracker _openFileTracker;

                public HierarchyEventSink(IVsHierarchy hierarchy, OpenFileTracker openFileTracker)
                {
                    _hierarchy = hierarchy;
                    _openFileTracker = openFileTracker;
                }

                public bool TryAdviseHierarchy()
                {
                    return ErrorHandler.Succeeded(_hierarchy.AdviseHierarchyEvents(this, out _cookie));
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
                        _openFileTracker.RefreshContextForHierarchyPropertyChange(_hierarchy);
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
