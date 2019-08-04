// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// Class to register with the RDT and forward RDT events.
    /// Handles common conditions and delegates implementation to the <see cref="IRunningDocumentTableEventListener"/>
    /// </summary>
    internal sealed class RunningDocumentTableEventTracker : IVsRunningDocTableEvents3, IDisposable
    {
        private bool _isDisposed = false; // To detect redundant calls

        private readonly ForegroundThreadAffinitizedObject _foregroundAffinitization;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IVsRunningDocumentTable4 _runningDocumentTable;
        private readonly IRunningDocumentTableEventListener _listener;
        private uint _runningDocumentTableEventsCookie;

        public RunningDocumentTableEventTracker(IThreadingContext threadingContext, IVsEditorAdaptersFactoryService editorAdaptersFactoryService, IVsRunningDocumentTable runningDocumentTable,
            IRunningDocumentTableEventListener listener)
        {
            Contract.ThrowIfNull(threadingContext);
            Contract.ThrowIfNull(editorAdaptersFactoryService);
            Contract.ThrowIfNull(runningDocumentTable);
            Contract.ThrowIfNull(listener);

            // Advise / Unadvise for the RDT is free threaded past 16.0
            _foregroundAffinitization = new ForegroundThreadAffinitizedObject(threadingContext, assertIsForeground: false);
            _runningDocumentTable = (IVsRunningDocumentTable4)runningDocumentTable;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _listener = listener;

            ((IVsRunningDocumentTable)_runningDocumentTable).AdviseRunningDocTableEvents(this, out _runningDocumentTableEventsCookie);
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            if (dwReadLocksRemaining + dwEditLocksRemaining == 0)
            {
                _foregroundAffinitization.AssertIsForeground();
                if (_runningDocumentTable.IsDocumentInitialized(docCookie))
                {
                    _listener.OnCloseDocument(_runningDocumentTable.GetDocumentMoniker(docCookie));
                }
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
            // Did we rename?
            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_MkDocument) != 0)
            {
                _foregroundAffinitization.AssertIsForeground();
                if (_runningDocumentTable.IsDocumentInitialized(docCookie) && TryGetBuffer(docCookie, out var buffer))
                {
                    _listener.OnRenameDocument(newMoniker: pszMkDocumentNew, oldMoniker: pszMkDocumentOld, textBuffer: buffer);
                }
            }

            // Doc data reloaded is the most reliable way to know when a document has been loaded and may have a text buffer we can get.
            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_DocDataReloaded) != 0)
            {
                _foregroundAffinitization.AssertIsForeground();
                if (_runningDocumentTable.IsDocumentInitialized(docCookie) && TryGetMoniker(docCookie, out var moniker) && TryGetBuffer(docCookie, out var buffer))
                {
                    _runningDocumentTable.GetDocumentHierarchyItem(docCookie, out var hierarchy, out _);
                    _listener.OnOpenDocument(moniker, buffer, hierarchy);
                }
            }

            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_Hierarchy) != 0)
            {
                _foregroundAffinitization.AssertIsForeground();
                if (_runningDocumentTable.IsDocumentInitialized(docCookie) && TryGetMoniker(docCookie, out var moniker))
                {
                    _runningDocumentTable.GetDocumentHierarchyItem(docCookie, out var hierarchy, out _);
                    _listener.OnRefreshDocumentContext(moniker, hierarchy);
                }
            }

            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            // Doc data reloaded is not triggered for the underlying aspx.cs file when changes are made to the aspx file, so catch it here.
            if (fFirstShow != 0 && _runningDocumentTable.IsDocumentInitialized(docCookie) && TryGetMoniker(docCookie, out var moniker) && TryGetBuffer(docCookie, out var buffer))
            {
                _runningDocumentTable.GetDocumentHierarchyItem(docCookie, out var hierarchy, out _);
                _listener.OnOpenDocument(moniker, buffer, hierarchy);
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

        public bool IsFileOpen(string fileName) => _runningDocumentTable.IsMonikerValid(fileName);

        /// <summary>
        /// Attempts to get a text buffer from the specified moniker.
        /// </summary>
        /// <param name="moniker">the moniker to retrieve the text buffer for.</param>
        /// <param name="textBuffer">the output text buffer or null if the moniker is invalid / document is not initialized.</param>
        /// <returns>true if the buffer was found with a non null value.</returns>
        public bool TryGetBufferFromMoniker(string moniker, out ITextBuffer textBuffer)
        {
            _foregroundAffinitization.AssertIsForeground();

            textBuffer = null;
            if (!IsFileOpen(moniker))
            {
                return false;
            }

            var cookie = _runningDocumentTable.GetDocumentCookie(moniker);
            if (!_runningDocumentTable.IsDocumentInitialized(cookie))
            {
                return false;
            }

            return TryGetBuffer(cookie, out textBuffer);
        }

        public IVsHierarchy GetDocumentHierarchy(string moniker)
        {
            if (!IsFileOpen(moniker))
            {
                return null;
            }

            var cookie = _runningDocumentTable.GetDocumentCookie(moniker);
            _runningDocumentTable.GetDocumentHierarchyItem(cookie, out var hierarchy, out _);
            return hierarchy;
        }

        /// <summary>
        /// Enumerates the running document table to retrieve all initialized files.
        /// </summary>
        public IEnumerable<(string moniker, ITextBuffer textBuffer, IVsHierarchy hierarchy)> EnumerateDocumentSet()
        {
            _foregroundAffinitization.AssertIsForeground();

            var documents = ArrayBuilder<(string, ITextBuffer, IVsHierarchy)>.GetInstance();
            foreach (var cookie in GetInitializedRunningDocumentTableCookies())
            {
                if (TryGetMoniker(cookie, out var moniker) && TryGetBuffer(cookie, out var buffer))
                {
                    _runningDocumentTable.GetDocumentHierarchyItem(cookie, out var hierarchy, out _);
                    documents.Add((moniker, buffer, hierarchy));
                }
            }

            return documents.ToArray();
        }

        private IEnumerable<uint> GetInitializedRunningDocumentTableCookies()
        {
            // Some methods we need here only exist in IVsRunningDocumentTable and not the IVsRunningDocumentTable4 that we
            // hold onto as a field
            var runningDocumentTable = (IVsRunningDocumentTable)_runningDocumentTable;
            ErrorHandler.ThrowOnFailure(runningDocumentTable.GetRunningDocumentsEnum(out var enumRunningDocuments));
            var cookies = new uint[16];

            while (ErrorHandler.Succeeded(enumRunningDocuments.Next((uint)cookies.Length, cookies, out var cookiesFetched))
                   && cookiesFetched > 0)
            {
                for (var cookieIndex = 0; cookieIndex < cookiesFetched; cookieIndex++)
                {
                    var cookie = cookies[cookieIndex];

                    if (_runningDocumentTable.IsDocumentInitialized(cookie))
                    {
                        yield return cookie;
                    }
                }
            }
        }

        private bool TryGetMoniker(uint docCookie, out string moniker)
        {
            moniker = _runningDocumentTable.GetDocumentMoniker(docCookie);
            return !string.IsNullOrEmpty(moniker);
        }

        private bool TryGetBuffer(uint docCookie, out ITextBuffer textBuffer)
        {
            textBuffer = null;

            // The cast from dynamic to object doesn't change semantics, but avoids loading the dynamic binder
            // which saves us JIT time in this method and an assembly load.
            if ((object)_runningDocumentTable.GetDocumentData(docCookie) is IVsTextBuffer bufferAdapter)
            {
                textBuffer = _editorAdaptersFactoryService.GetDocumentBuffer(bufferAdapter);
                return textBuffer != null;
            }

            return false;
        }

        #region IDisposable Support
        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    var runningDocumentTableForEvents = (IVsRunningDocumentTable)_runningDocumentTable;
                    runningDocumentTableForEvents.UnadviseRunningDocTableEvents(_runningDocumentTableEventsCookie);
                    _runningDocumentTableEventsCookie = 0;
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
