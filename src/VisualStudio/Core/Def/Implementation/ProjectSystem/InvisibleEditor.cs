// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class InvisibleEditor : IInvisibleEditor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _filePath;
        private readonly bool _needsSave = false;

        /// <summary>
        /// The text buffer. null if the object has been disposed.
        /// </summary>
        private ITextBuffer _buffer;
        private IVsTextLines _vsTextLines;
        private IVsInvisibleEditor _invisibleEditor;
        private OLE.Interop.IOleUndoManager _manager;
        private readonly bool _needsUndoRestored;

        public InvisibleEditor(IServiceProvider serviceProvider, string filePath, bool needsSave, bool needsUndoDisabled)
        {
            _serviceProvider = serviceProvider;
            _filePath = filePath;
            _needsSave = needsSave;

            var invisibleEditorManager = (IIntPtrReturningVsInvisibleEditorManager)serviceProvider.GetService(typeof(SVsInvisibleEditorManager));
            var invisibleEditorPtr = IntPtr.Zero;
            Marshal.ThrowExceptionForHR(invisibleEditorManager.RegisterInvisibleEditor(filePath, null, 0, null, out invisibleEditorPtr));

            try
            {
                _invisibleEditor = (IVsInvisibleEditor)Marshal.GetUniqueObjectForIUnknown(invisibleEditorPtr);

                var docDataPtr = IntPtr.Zero;
                Marshal.ThrowExceptionForHR(_invisibleEditor.GetDocData(fEnsureWritable: needsSave ? 1 : 0, riid: typeof(IVsTextLines).GUID, ppDocData: out docDataPtr));

                try
                {
                    var docData = Marshal.GetObjectForIUnknown(docDataPtr);
                    _vsTextLines = docData as IVsTextLines;
                    var vsTextBuffer = (IVsTextBuffer)docData;
                    var editorAdapterFactoryService = serviceProvider.GetMefService<IVsEditorAdaptersFactoryService>();
                    _buffer = editorAdapterFactoryService.GetDocumentBuffer(vsTextBuffer);
                    if (needsUndoDisabled)
                    {
                        Marshal.ThrowExceptionForHR(vsTextBuffer.GetUndoManager(out _manager));
                        int isEnabled;
                        Marshal.ThrowExceptionForHR((_manager as IVsUndoState).IsEnabled(out isEnabled));
                        _needsUndoRestored = isEnabled != 0;
                        if (_needsUndoRestored)
                        {
                            _manager.DiscardFrom(null); // Discard the undo history for this document
                            _manager.Enable(0); // Disable Undo for this document
                        }
                    }
                }
                finally
                {
                    Marshal.Release(docDataPtr);
                }
            }
            finally
            {
                // We need to clean up the extra reference we have, now that we have an RCW holding onto the object.
                Marshal.Release(invisibleEditorPtr);
            }
        }

        public IVsTextLines VsTextLines
        {
            get
            {
                return _vsTextLines;
            }
        }

        public ITextBuffer TextBuffer
        {
            get
            {
                if (_buffer == null)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                return _buffer;
            }
        }

        /// <summary>
        /// Closes the invisible editor and saves the underlying document as appropriate.
        /// </summary>
        public void Dispose()
        {
            _buffer = null;
            _vsTextLines = null;

            try
            {
                if (_needsSave)
                {
                    // We need to tell this document to save before we get rid of the invisible editor. Otherwise,
                    // the invisible editor never actually makes the document go away. Check out CLockHolder::ReleaseEditLock
                    // in env\msenv\core\editmgr.cpp for details. We choose this particular technique for saving files
                    // since it's what the old cslangsvc.dll used.
                    var runningDocumentTable4 = (IVsRunningDocumentTable4)_serviceProvider.GetService(typeof(SVsRunningDocumentTable));

                    if (runningDocumentTable4.IsMonikerValid(_filePath))
                    {
                        var cookie = runningDocumentTable4.GetDocumentCookie(_filePath);
                        var runningDocumentTable = (IVsRunningDocumentTable)runningDocumentTable4;

                        // Old cslangsvc.dll requested not to add to MRU for, and I quote, "performance!". Makes sense not
                        // to include it in the MRU anyways.
                        ErrorHandler.ThrowOnFailure(runningDocumentTable.ModifyDocumentFlags(cookie, (uint)_VSRDTFLAGS.RDT_DontAddToMRU, fSet: 1));

                        runningDocumentTable.SaveDocuments((uint)__VSRDTSAVEOPTIONS.RDTSAVEOPT_SaveIfDirty, pHier: null, itemid: 0, docCookie: cookie);
                    }
                }

                if (_needsUndoRestored && _manager != null)
                {
                    _manager.Enable(1);
                    _manager = null;
                }

                // Clean up our RCW. This RCW is a unique RCW, so this is actually safe to do!
                Marshal.ReleaseComObject(_invisibleEditor);
                _invisibleEditor = null;

                GC.SuppressFinalize(this);
            }
            catch (Exception ex) when (FatalError.Report(ex))
            {
            }
        }

        ~InvisibleEditor()
        {
            Debug.Assert(Environment.HasShutdownStarted, GetType().Name + " was leaked without Dispose being called.");
        }
    }
}
