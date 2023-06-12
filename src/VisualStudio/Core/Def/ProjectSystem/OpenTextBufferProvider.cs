// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// A class that provides access to the currently open list of files in Visual Studio.
    /// </summary>
    /// <remarks>
    /// You are able to ask for the text buffer for a document on any thread; events are raised on the UI thread, and any method that provides a <see cref="IVsHierarchy"/> must be used on the UI thread.
    /// Individual methods are documented for which threading contracts they expect.
    /// </remarks>
    [Export(typeof(OpenTextBufferProvider))]
    internal sealed class OpenTextBufferProvider : IVsRunningDocTableEvents3, IDisposable
    {
        private bool _isDisposed = false;

        /// <summary>
        /// A simple object for asserting when we're on the UI thread.
        /// </summary>
        private readonly ForegroundThreadAffinitizedObject _foregroundAffinitization;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IVsRunningDocumentTable4 _runningDocumentTable;

        private ImmutableArray<IOpenTextBufferEventListener> _listeners = ImmutableArray<IOpenTextBufferEventListener>.Empty;

        /// <summary>
        /// The map from monikers to open text buffers; because we can only fetch the text buffer on the UI thread, all updates to this must be done from the UI thread.
        /// </summary>
        private ImmutableDictionary<string, ITextBuffer> _monikerToTextBufferMap = ImmutableDictionary<string, ITextBuffer>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

        private uint _runningDocumentTableEventsCookie;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OpenTextBufferProvider(
            IThreadingContext threadingContext,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _foregroundAffinitization = new ForegroundThreadAffinitizedObject(threadingContext, assertIsForeground: false);

            _editorAdaptersFactoryService = editorAdaptersFactoryService;

            // The running document table since 16.0 has limited operations that can be done in a free threaded manner, specifically fetching the service and advising events.
            // This is specifically guaranteed by the shell that those limited operations are safe and do not cause RPCs, and it's important we don't try to fetch the service
            // via a helper that will "helpfully" try to jump to the UI thread.
            var runningDocumentTable = (IVsRunningDocumentTable)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            _runningDocumentTable = (IVsRunningDocumentTable4)runningDocumentTable;
            runningDocumentTable.AdviseRunningDocTableEvents(this, out _runningDocumentTableEventsCookie);

            // We also need to check for any documents that might have been open before we subscribed. That we do have to do on the UI thread.
            var listener = listenerProvider.GetListener(FeatureAttribute.Workspace);
            var asyncToken = listener.BeginAsyncOperation(nameof(CheckForExistingOpenDocumentsAsync));
            CheckForExistingOpenDocumentsAsync(threadingContext).CompletesAsyncOperation(asyncToken);
        }

        private void RaiseEventForEachListener(Action<IOpenTextBufferEventListener> action)
        {
            _foregroundAffinitization.AssertIsForeground();

            foreach (var listener in _listeners)
            {
                try
                {
                    action(listener);
                }
                catch (Exception e) when (FatalError.ReportAndCatch(e, ErrorSeverity.Critical))
                {
                    // We'll catch the exception; this way if one listener is broken, we don't end up breaking other features that might no longer get events. Any exceptions would get caught by the
                    // RunningDocumentTable itself which wouldn't report them in a useful way regardless.
                }
            }
        }

        private async Task CheckForExistingOpenDocumentsAsync(IThreadingContext threadingContext)
        {
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

            foreach (var (filePath, textBuffer, hierarchy) in EnumerateDocumentSet())
            {
                // We might or might not have seen this file be opened if it was opened between when we subscribed to the running document table and when
                // we got scheduled to the UI thread.
                if (!_monikerToTextBufferMap.ContainsKey(filePath))
                {
                    _monikerToTextBufferMap = _monikerToTextBufferMap.Add(filePath, textBuffer);
                    RaiseEventForEachListener(l => l.OnOpenDocument(filePath, textBuffer, hierarchy));
                }
            }
        }

        public void AddListener(IOpenTextBufferEventListener listener) => ImmutableInterlocked.Update(ref _listeners, static (array, listener) => array.Add(listener), listener);
        public void RemoveListener(IOpenTextBufferEventListener listener) => ImmutableInterlocked.Update(ref _listeners, static (array, listener) => array.Remove(listener), listener);

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
            => VSConstants.E_NOTIMPL;

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            if (dwReadLocksRemaining + dwEditLocksRemaining == 0)
            {
                _foregroundAffinitization.AssertIsForeground();
                if (_runningDocumentTable.IsDocumentInitialized(docCookie))
                {
                    var moniker = _runningDocumentTable.GetDocumentMoniker(docCookie);
                    _monikerToTextBufferMap = _monikerToTextBufferMap.Remove(moniker);

                    RaiseEventForEachListener(l => l.OnCloseDocument(moniker));
                }
            }

            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
            => VSConstants.E_NOTIMPL;

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
            => VSConstants.E_NOTIMPL;

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            // Did we rename?
            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_MkDocument) != 0)
            {
                _foregroundAffinitization.AssertIsForeground();
                if (_runningDocumentTable.IsDocumentInitialized(docCookie))
                {
                    // We should already have a text buffer for this one
                    if (_monikerToTextBufferMap.TryGetValue(pszMkDocumentOld, out var textBuffer))
                    {
                        _monikerToTextBufferMap = _monikerToTextBufferMap.Remove(pszMkDocumentOld).Add(pszMkDocumentNew, textBuffer);
                    }
                    else
                    {
                        // Odd we don't have one, but fetch it now
                        if (TryGetBufferFromRunningDocumentTable(docCookie, out textBuffer))
                        {
                            _monikerToTextBufferMap = _monikerToTextBufferMap.Add(pszMkDocumentNew, textBuffer);
                        }
                    }

                    // Only raise an event if we had a text buffer; otherwise this is a rename of something else and we don't need to report it
                    if (textBuffer != null)
                    {
                        RaiseEventForEachListener(l => l.OnRenameDocument(newMoniker: pszMkDocumentNew, oldMoniker: pszMkDocumentOld, textBuffer: textBuffer));
                    }
                }
            }

            // Either RDTA_DocDataReloaded or RDTA_DocumentInitialized will be triggered if there's a lazy load and the document is now available.
            // See https://devdiv.visualstudio.com/DevDiv/_workitems/edit/937712 for a scenario where we do need the RDTA_DocumentInitialized check.
            // We still check for RDTA_DocDataReloaded because the RDT will mark something as initialized as soon as there is something in the doc data,
            // but that might still not be associated with an ITextBuffer.
            if ((grfAttribs & ((uint)__VSRDTATTRIB.RDTA_DocDataReloaded | (uint)__VSRDTATTRIB3.RDTA_DocumentInitialized)) != 0)
            {
                _foregroundAffinitization.AssertIsForeground();
                if (_runningDocumentTable.IsDocumentInitialized(docCookie) && TryGetMoniker(docCookie, out var moniker) && TryGetBufferFromRunningDocumentTable(docCookie, out var buffer))
                {
                    _monikerToTextBufferMap = _monikerToTextBufferMap.Add(moniker, buffer);
                    _runningDocumentTable.GetDocumentHierarchyItem(docCookie, out var hierarchy, out _);

                    RaiseEventForEachListener(l => l.OnOpenDocument(moniker, buffer, hierarchy));
                }
            }

            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_Hierarchy) != 0)
            {
                _foregroundAffinitization.AssertIsForeground();
                if (_runningDocumentTable.IsDocumentInitialized(docCookie) && TryGetMoniker(docCookie, out var moniker))
                {
                    _runningDocumentTable.GetDocumentHierarchyItem(docCookie, out var hierarchy, out _);

                    RaiseEventForEachListener(l => l.OnRefreshDocumentContext(moniker, hierarchy));
                }
            }

            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            // Doc data reloaded is not triggered for the underlying aspx.cs file when changes are made to the aspx file, so catch it here.
            if (fFirstShow != 0 && _runningDocumentTable.IsDocumentInitialized(docCookie) && TryGetMoniker(docCookie, out var moniker))
            {
                // If we hadn't already raised an event for this, do it now
                if (!_monikerToTextBufferMap.ContainsKey(moniker) && TryGetBufferFromRunningDocumentTable(docCookie, out var buffer))
                {
                    _monikerToTextBufferMap = _monikerToTextBufferMap.Add(moniker, buffer);
                    _runningDocumentTable.GetDocumentHierarchyItem(docCookie, out var hierarchy, out _);

                    RaiseEventForEachListener(l => l.OnOpenDocument(moniker, buffer, hierarchy));
                }

                RaiseEventForEachListener(l => l.OnDocumentOpenedIntoWindowFrame(moniker, pFrame));
            }

            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
            => VSConstants.E_NOTIMPL;

        public int OnBeforeSave(uint docCookie)
            => VSConstants.E_NOTIMPL;

        /// <summary>
        /// Attempts to get a text buffer from the specified file path. May be called on any thread.
        /// </summary>
        /// <param name="filePath">The file path to retrieve the text buffer for.</param>
        /// <param name="textBuffer">The buffer if the file is open and initialized.</param>
        /// <returns>true if the buffer was found with a non null value.</returns>
        public bool TryGetBufferFromFilePath(string filePath, [NotNullWhen(true)] out ITextBuffer? textBuffer)
        {
            return _monikerToTextBufferMap.TryGetValue(filePath, out textBuffer);
        }

        /// <summary>
        /// Checks if a file is open. May be called on any thread.
        /// </summary>
        public bool IsFileOpen(string filePath)
        {
            return _monikerToTextBufferMap.ContainsKey(filePath);
        }

        /// <summary>
        /// Fetches the <see cref="IVsHierarchy"/> for a document. Must be called on the UI thread.
        /// </summary>
        public IVsHierarchy? GetDocumentHierarchy(string filePath)
        {
            _foregroundAffinitization.AssertIsForeground();

            if (!_runningDocumentTable.IsFileOpen(filePath))
            {
                return null;
            }

            var cookie = _runningDocumentTable.GetDocumentCookie(filePath);
            _runningDocumentTable.GetDocumentHierarchyItem(cookie, out var hierarchy, out _);
            return hierarchy;
        }

        /// <summary>
        /// Enumerates the running document table to retrieve all initialized files. Must be called on the UI thread, since this returns <see cref="IVsHierarchy"/> objects.
        /// </summary>
        public IEnumerable<(string filePath, ITextBuffer textBuffer, IVsHierarchy hierarchy)> EnumerateDocumentSet()
        {
            _foregroundAffinitization.AssertIsForeground();

            var documents = ArrayBuilder<(string, ITextBuffer, IVsHierarchy)>.GetInstance();
            foreach (var cookie in GetInitializedRunningDocumentTableCookies())
            {
                if (TryGetMoniker(cookie, out var moniker) && TryGetBufferFromRunningDocumentTable(cookie, out var buffer))
                {
                    _runningDocumentTable.GetDocumentHierarchyItem(cookie, out var hierarchy, out _);
                    documents.Add((moniker, buffer, hierarchy));
                }
            }

            return documents.ToArray();
        }

        private IEnumerable<uint> GetInitializedRunningDocumentTableCookies()
        {
            foreach (var cookie in _runningDocumentTable.GetRunningDocuments())
            {
                if (_runningDocumentTable.IsDocumentInitialized(cookie))
                {
                    yield return cookie;
                }
            }
        }

        private bool TryGetMoniker(uint docCookie, out string moniker)
        {
            moniker = _runningDocumentTable.GetDocumentMoniker(docCookie);
            return !string.IsNullOrEmpty(moniker);
        }

        private bool TryGetBufferFromRunningDocumentTable(uint docCookie, [NotNullWhen(true)] out ITextBuffer? textBuffer)
        {
            _foregroundAffinitization.AssertIsForeground();
            return _runningDocumentTable.TryGetBuffer(_editorAdaptersFactoryService, docCookie, out textBuffer);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            var runningDocumentTableForEvents = (IVsRunningDocumentTable)_runningDocumentTable;
            runningDocumentTableForEvents.UnadviseRunningDocTableEvents(_runningDocumentTableEventsCookie);
            _runningDocumentTableEventsCookie = 0;

            _isDisposed = true;
        }
    }
}
