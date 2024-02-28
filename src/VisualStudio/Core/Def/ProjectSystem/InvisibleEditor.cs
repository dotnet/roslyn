// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal partial class InvisibleEditor : ForegroundThreadAffinitizedObject, IInvisibleEditor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _filePath;
    private readonly bool _needsSave = false;

    /// <summary>
    /// The text buffer. null if the object has been disposed.
    /// </summary>
    private ITextBuffer? _buffer;
    private IVsTextLines _vsTextLines;
    private IVsInvisibleEditor _invisibleEditor;
    private OLE.Interop.IOleUndoManager? _manager;
    private readonly bool _needsUndoRestored;

    /// <remarks>
    /// <para>The optional project is used to obtain an <see cref="IVsProject"/> instance. When this instance is
    /// provided, Visual Studio will use <see cref="IVsProject.IsDocumentInProject"/> to attempt to locate the
    /// specified file within a project. If no project is specified, Visual Studio falls back to using
    /// <see cref="IVsUIShellOpenDocument4.IsDocumentInAProject2"/>, which performs a much slower query of all
    /// projects in the solution.</para>
    /// </remarks>
    public InvisibleEditor(IServiceProvider serviceProvider, string filePath, IVsHierarchy? hierarchy, bool needsSave, bool needsUndoDisabled)
        : base(serviceProvider.GetMefService<IThreadingContext>(), assertIsForeground: true)
    {
        _serviceProvider = serviceProvider;
        _filePath = filePath;
        _needsSave = needsSave;

        var invisibleEditorManager = (IIntPtrReturningVsInvisibleEditorManager)serviceProvider.GetService(typeof(SVsInvisibleEditorManager));
        var vsProject = hierarchy as IVsProject;
        Marshal.ThrowExceptionForHR(invisibleEditorManager.RegisterInvisibleEditor(filePath, vsProject, 0, null, out var invisibleEditorPtr));

        try
        {
            _invisibleEditor = (IVsInvisibleEditor)Marshal.GetUniqueObjectForIUnknown(invisibleEditorPtr);

            _vsTextLines = RetrieveDocData(_invisibleEditor, needsSave);

            var editorAdapterFactoryService = serviceProvider.GetMefService<IVsEditorAdaptersFactoryService>();
            _buffer = editorAdapterFactoryService.GetDocumentBuffer(_vsTextLines);
            if (needsUndoDisabled)
            {
                Marshal.ThrowExceptionForHR(_vsTextLines.GetUndoManager(out _manager));
                Marshal.ThrowExceptionForHR(((IVsUndoState)_manager).IsEnabled(out var isEnabled));
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
            // We need to clean up the extra reference we have, now that we have an RCW holding onto the object.
            Marshal.Release(invisibleEditorPtr);
        }

        // Try casting the doc data to IVsTextLines first.
        // If it fails try casting to IVsTextBufferProvider as some files like .aspx use that to provide the buffer
        static IVsTextLines RetrieveDocData(IVsInvisibleEditor invisibleEditor, bool needsSave)
        {
            IVsTextLines? buffer = null;
            var docDataPtrViaTextBufferProvider = IntPtr.Zero;

            var hr = invisibleEditor.GetDocData(fEnsureWritable: needsSave ? 1 : 0, riid: typeof(IVsTextLines).GUID, ppDocData: out var docDataPtrViaTextLines);
            try
            {
                if (ErrorHandler.Succeeded(hr) &&
                    Marshal.GetObjectForIUnknown(docDataPtrViaTextLines) is IVsTextLines vsTextLines)
                {
                    buffer = vsTextLines;
                }
                else
                {
                    hr = invisibleEditor.GetDocData(fEnsureWritable: needsSave ? 1 : 0, riid: typeof(IVsTextBufferProvider).GUID, ppDocData: out docDataPtrViaTextBufferProvider);
                    if (ErrorHandler.Succeeded(hr) &&
                        Marshal.GetObjectForIUnknown(docDataPtrViaTextBufferProvider) is IVsTextBufferProvider vsTextBufferProvider)
                    {
                        hr = vsTextBufferProvider.GetTextBuffer(out buffer);
                    }
                }
            }
            finally
            {
                if (docDataPtrViaTextBufferProvider != IntPtr.Zero)
                    Marshal.Release(docDataPtrViaTextBufferProvider);

                if (docDataPtrViaTextLines != IntPtr.Zero)
                    Marshal.Release(docDataPtrViaTextLines);
            }

            Marshal.ThrowExceptionForHR(hr);
            Contract.ThrowIfNull(buffer, $"We were unable to fetch a buffer in {nameof(InvisibleEditor)}.");

            return buffer;
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
        AssertIsForeground();

        _buffer = null;
        _vsTextLines = null!;

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
            _invisibleEditor = null!;

            GC.SuppressFinalize(this);
        }
        catch (Exception ex) when (FatalError.ReportAndPropagate(ex, ErrorSeverity.Critical)) // critical severity, since this means we're not saving edited files
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

#if DEBUG
    ~InvisibleEditor()
        => Debug.Assert(Environment.HasShutdownStarted, GetType().Name + " was leaked without Dispose being called.");
#endif
}
