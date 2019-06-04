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
    /// Handles common conditions before sending out notifications.
    /// </summary>
    internal sealed class RunningDocumentTableEventTracker : IVsRunningDocTableEvents3, IDisposable
    {
        private bool _isDisposed = false; // To detect redundant calls

        private readonly ForegroundThreadAffinitizedObject _foregroundAffinitization;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IVsRunningDocumentTable4 _runningDocumentTable;
        private uint _runningDocumentTableEventsCookie;

        public event EventHandler<RunningDocumentTableEventArgs> OnCloseDocument;
        public event EventHandler<RunningDocumentTableEventArgs> OnRefreshDocumentContext;
        public event EventHandler<RunningDocumentTableEventArgs> OnReloadDocumentData;
        public event EventHandler<RunningDocumentTableInitializedEventArgs> OnBeforeOpenDocument;
        public event EventHandler<RunningDocumentTableInitializedEventArgs> OnInitializedDocument;
        public event EventHandler<RunningDocumentTableRenamedEventArgs> OnRenameDocument;

        public RunningDocumentTableEventTracker(IThreadingContext threadingContext, IVsEditorAdaptersFactoryService editorAdaptersFactoryService, IVsRunningDocumentTable4 runningDocumentTable)
        {
            Contract.ThrowIfNull(threadingContext);
            Contract.ThrowIfNull(editorAdaptersFactoryService);
            Contract.ThrowIfNull(runningDocumentTable);

            _foregroundAffinitization = new ForegroundThreadAffinitizedObject(threadingContext, assertIsForeground: true);
            _runningDocumentTable = runningDocumentTable;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;

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
                    var args = new RunningDocumentTableEventArgs(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie));
                    OnCloseDocument?.Invoke(this, args);
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
                    var args = new RunningDocumentTableRenamedEventArgs(docCookie, pszMkDocumentNew, pszMkDocumentOld);
                    OnRenameDocument?.Invoke(this, args);
                }
            }

            if ((grfAttribs & (uint)__VSRDTATTRIB3.RDTA_DocumentInitialized) != 0)
            {
                if (TryGetBuffer(docCookie, out var buffer))
                {
                    var args = new RunningDocumentTableInitializedEventArgs(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie), buffer);
                    OnInitializedDocument?.Invoke(this, args);
                }
            }

            // When starting a diff, the RDT doesn't call OnBeforeDocumentWindowShow, but it does call
            // OnAfterAttributeChangeEx for the temporary buffer. The native IDE used this even to
            // add misc files, so we'll do the same.
            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_DocDataReloaded) != 0)
            {
                if (CheckPreconditions(docCookie))
                {
                    var args = new RunningDocumentTableEventArgs(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie));
                    OnReloadDocumentData?.Invoke(this, args);
                }
            }

            if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_Hierarchy) != 0)
            {
                var args = new RunningDocumentTableEventArgs(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie));
                OnRefreshDocumentContext?.Invoke(this, args);
            }

            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            if (fFirstShow != 0)
            {
                if (TryGetBuffer(docCookie, out var buffer))
                {
                    var args = new RunningDocumentTableInitializedEventArgs(docCookie, _runningDocumentTable.GetDocumentMoniker(docCookie), buffer);
                    OnBeforeOpenDocument?.Invoke(this, args);
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

    /// <summary>
    /// Base event args for events coming from the RDT.
    /// </summary>
    internal class RunningDocumentTableEventArgs : EventArgs
    {
        public uint DocCookie { get; private set; }

        public string Moniker { get; private set; }

        public RunningDocumentTableEventArgs(uint docCookie, string moniker)
        {
            DocCookie = docCookie;
            Moniker = moniker;
        }
    }

    /// <summary>
    /// Event args for document open events that also include the text buffer.
    /// </summary>
    internal class RunningDocumentTableInitializedEventArgs : RunningDocumentTableEventArgs
    {
        public ITextBuffer TextBuffer { get; private set; }

        public RunningDocumentTableInitializedEventArgs(uint docCookie, string moniker, ITextBuffer textBuffer) : base(docCookie, moniker)
        {
            TextBuffer = textBuffer;
        }
    }

    /// <summary>
    /// Event for document full path event changes.
    /// Includes the old document moniker and new document moniker.
    /// </summary>
    internal class RunningDocumentTableRenamedEventArgs : RunningDocumentTableEventArgs
    {
        public string OldMoniker { get; private set; }

        public RunningDocumentTableRenamedEventArgs(uint docCookie, string moniker, string oldMoniker) : base(docCookie, moniker)
        {
            OldMoniker = oldMoniker;
        }
    }
}
