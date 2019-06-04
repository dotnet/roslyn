// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
        private readonly IRunningDocumentTableEventListener _runningDocumentTableEventListener;
        private uint _runningDocumentTableEventsCookie;

        public RunningDocumentTableEventTracker(IThreadingContext threadingContext, IVsEditorAdaptersFactoryService editorAdaptersFactoryService, IVsRunningDocumentTable4 runningDocumentTable,
            IRunningDocumentTableEventListener runningDocumentTableEventListener)
        {
            Contract.ThrowIfNull(threadingContext);
            Contract.ThrowIfNull(editorAdaptersFactoryService);
            Contract.ThrowIfNull(runningDocumentTable);
            Contract.ThrowIfNull(runningDocumentTableEventListener);

            _foregroundAffinitization = new ForegroundThreadAffinitizedObject(threadingContext, assertIsForeground: true);
            _runningDocumentTable = runningDocumentTable;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _runningDocumentTableEventListener = runningDocumentTableEventListener;

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
                if (CheckPreconditions(docCookie))
                {
                    _runningDocumentTableEventListener.OnCloseDocument(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie));
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
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            // Did we rename?
            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_MkDocument) != 0)
            {
                if (CheckPreconditions(docCookie))
                {
                    _runningDocumentTableEventListener.OnRenameDocument(docCookie, pszMkDocumentNew, pszMkDocumentOld);
                }
            }

            if ((grfAttribs & (uint)__VSRDTATTRIB3.RDTA_DocumentInitialized) != 0)
            {
                if (TryGetBuffer(docCookie, out var buffer))
                {
                    _runningDocumentTableEventListener.OnInitializedDocument(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie), buffer);
                }
            }

            // When starting a diff, the RDT doesn't call OnBeforeDocumentWindowShow, but it does call
            // OnAfterAttributeChangeEx for the temporary buffer. The native IDE used this even to
            // add misc files, so we'll do the same.
            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_DocDataReloaded) != 0)
            {
                if (CheckPreconditions(docCookie))
                {
                    _runningDocumentTableEventListener.OnReloadDocumentData(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie));
                }
            }

            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_Hierarchy) != 0)
            {
                _runningDocumentTableEventListener.OnRefreshDocumentContext(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie));
            }

            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            if (fFirstShow != 0)
            {
                if (TryGetBuffer(docCookie, out var buffer))
                {
                    _runningDocumentTableEventListener.OnBeforeOpenDocument(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie), buffer);
                }
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

        /// <summary>
        /// Gets the text buffer for a document cookie.
        /// Also checks to make sure the document is initialized before returning.
        /// </summary>
        public bool TryGetBuffer(uint docCookie, out ITextBuffer textBuffer)
        {
            textBuffer = null;
            if (!CheckPreconditions(docCookie))
            {
                return false;
            }

            if ((object)_runningDocumentTable.GetDocumentData(docCookie) is IVsTextBuffer bufferAdapter)
            {
                textBuffer = _editorAdaptersFactoryService.GetDocumentBuffer(bufferAdapter);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks that we're on the UI thread and that the document has already been initialized.
        /// </summary>
        private bool CheckPreconditions(uint docCookie)
        {
            _foregroundAffinitization.AssertIsForeground();

            if (!_runningDocumentTable.IsDocumentInitialized(docCookie))
            {
                // We never want to touch documents that haven't been initialized yet, so immediately bail. Any further
                // calls to the RDT might accidentally initialize it.
                return false;
            }

            return true;
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
