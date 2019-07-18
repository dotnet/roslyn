// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    /// <summary>
    /// A singleton that tracks the open IVsWindowFrames and can report which documents are visible or active in a given <see cref="Workspace"/>.
    /// Can be accessed via the <see cref="IDocumentTrackingService"/> as a workspace service.
    /// </summary>
    [Export]
    internal class VisualStudioActiveDocumentTracker : ForegroundThreadAffinitizedObject, IVsSelectionEvents, IDisposable
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;

        /// <summary>
        /// Collection of all asynchronous tasks that are started by this service. This should only be tasks that are implicitly
        /// async as we fetch services from the <see cref="IAsyncServiceProvider"/>, and are used to ensure we wait for them during shutdown.
        /// These should not be waited for in calls to <see cref="TryGetActiveDocument(Workspace)"/> or <see cref="GetVisibleDocuments(Workspace)"/>
        /// because those are expected to not be jumping to the UI thread per traditional Roslyn semantics and may deadlock.
        /// </summary>
        private readonly JoinableTaskCollection _asyncTasks;

        /// <summary>
        /// The list of tracked frames. This can only be written by the UI thread, although can be read (with care) from any thread.
        /// </summary>
        private ImmutableList<FrameListener> _visibleFrames = ImmutableList<FrameListener>.Empty;

        /// <summary>
        /// The active IVsWindowFrame. This can only be written by the UI thread, although can be read (with care) from any thread.
        /// </summary>
        private IVsWindowFrame _activeFrame;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioActiveDocumentTracker(
            IThreadingContext threadingContext,
            [Import(typeof(SVsServiceProvider))] IAsyncServiceProvider asyncServiceProvider,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
            : base(threadingContext, assertIsForeground: false)
        {
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _asyncTasks = new JoinableTaskCollection(threadingContext.JoinableTaskContext);
            _asyncTasks.Add(ThreadingContext.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                var monitorSelectionService = (IVsMonitorSelection)await asyncServiceProvider.GetServiceAsync(typeof(SVsShellMonitorSelection)).ConfigureAwait(false);

                if (ErrorHandler.Succeeded(monitorSelectionService.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out var value)))
                {
                    if (value is IVsWindowFrame windowFrame)
                    {
                        TrackNewActiveWindowFrame(windowFrame);
                    }
                }

                monitorSelectionService.AdviseSelectionEvents(this, out var _);
            }));
        }

        public void Dispose()
        {
            _asyncTasks.Join();
        }

        /// <summary>
        /// Raised when the set of window frames being tracked changes, which means the results from <see cref="TryGetActiveDocument"/> or <see cref="GetVisibleDocuments"/> may change.
        /// May be raised on any thread.
        /// </summary>
        public event EventHandler DocumentsChanged;

        /// <summary>
        /// Raised when a non-Roslyn text buffer is edited, which can be used to back off of expensive background processing. May be raised on any thread.
        /// </summary>
        public event EventHandler<EventArgs> NonRoslynBufferTextChanged;

        /// <summary>
        /// Returns the <see cref="DocumentId"/> of the active document in a given <see cref="Workspace"/>.
        /// </summary>
        public DocumentId TryGetActiveDocument(Workspace workspace)
        {
            ThisCanBeCalledOnAnyThread();

            // Fetch both fields locally. If there's a write between these, that's fine -- it might mean we
            // don't return the DocumentId for something we could have if _activeFrame isn't listed in _visibleFrames.
            // But given this API runs unsynchronized against the UI thread, even with locking the same could happen if somebody
            // calls just a fraction of a second early.
            var visibleFramesSnapshot = _visibleFrames;
            var activeFrameSnapshot = _activeFrame;

            if (activeFrameSnapshot == null || visibleFramesSnapshot.IsEmpty)
            {
                return null;
            }

            foreach (var listener in visibleFramesSnapshot)
            {
                if (listener.Frame == activeFrameSnapshot)
                {
                    return listener.GetDocumentId(workspace);
                }
            }

            return null;
        }

        /// <summary>
        /// Get a read-only collection of the <see cref="DocumentId"/>s of all the visible documents in the given <see cref="Workspace"/>.
        /// </summary>
        public ImmutableArray<DocumentId> GetVisibleDocuments(Workspace workspace)
        {
            ThisCanBeCalledOnAnyThread();

            var visibleFramesSnapshot = _visibleFrames;

            var ids = ArrayBuilder<DocumentId>.GetInstance(visibleFramesSnapshot.Count);

            foreach (var frame in visibleFramesSnapshot)
            {
                var documentId = frame.GetDocumentId(workspace);

                if (documentId != null)
                {
                    ids.Add(documentId);
                }
            }

            return ids.ToImmutableAndFree();
        }

        public void TrackNewActiveWindowFrame(IVsWindowFrame frame)
        {
            AssertIsForeground();

            Contract.ThrowIfNull(frame);

            _activeFrame = frame;

            if (!_visibleFrames.Any(f => f.Frame == frame))
            {
                _visibleFrames = _visibleFrames.Add(new FrameListener(this, frame));
            }

            this.DocumentsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RemoveFrame(FrameListener frame)
        {
            AssertIsForeground();

            if (frame.Frame == _activeFrame)
            {
                _activeFrame = null;
            }

            _visibleFrames = _visibleFrames.Remove(frame);

            this.DocumentsChanged?.Invoke(this, EventArgs.Empty);
        }

        int IVsSelectionEvents.OnSelectionChanged(IVsHierarchy pHierOld, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")] uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, [ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSITEMID")] uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSelectionEvents.OnElementValueChanged([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSSELELEMID")] uint elementid, object varValueOld, object varValueNew)
        {
            AssertIsForeground();

            if (elementid == (uint)VSConstants.VSSELELEMID.SEID_DocumentFrame)
            {
                // Remember the newly activated frame so it can be read from another thread.

                if (varValueNew is IVsWindowFrame frame)
                {
                    TrackNewActiveWindowFrame(frame);
                }
            }

            return VSConstants.S_OK;
        }

        int IVsSelectionEvents.OnCmdUIContextChanged([ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSCOOKIE")] uint dwCmdUICookie, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")] int fActive)
        {
            return VSConstants.E_NOTIMPL;
        }

        /// <summary>
        /// Listens to frame notifications for a visible frame. When the frame becomes invisible or closes,
        /// then it automatically disconnects.
        /// </summary>
        private class FrameListener : IVsWindowFrameNotify, IVsWindowFrameNotify2
        {
            public readonly IVsWindowFrame Frame;

            private readonly VisualStudioActiveDocumentTracker _documentTracker;
            private readonly uint _frameEventsCookie;

            private readonly ITextBuffer _textBuffer;

            public FrameListener(VisualStudioActiveDocumentTracker service, IVsWindowFrame frame)
            {
                _documentTracker = service;
                _documentTracker.AssertIsForeground();

                this.Frame = frame;

                ((IVsWindowFrame2)frame).Advise(this, out _frameEventsCookie);

                if (ErrorHandler.Succeeded(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out var docData)))
                {
                    if (docData is IVsTextBuffer bufferAdapter)
                    {
                        _textBuffer = _documentTracker._editorAdaptersFactoryService.GetDocumentBuffer(bufferAdapter);

                        if (!_textBuffer.ContentType.IsOfType(ContentTypeNames.RoslynContentType))
                        {
                            _textBuffer.Changed += NonRoslynTextBuffer_Changed;
                        }
                    }
                }
            }

            private void NonRoslynTextBuffer_Changed(object sender, TextContentChangedEventArgs e)
            {
                _documentTracker.NonRoslynBufferTextChanged?.Invoke(_documentTracker, EventArgs.Empty);
            }

            /// <summary>
            /// Returns the current DocumentId for this window frame. Care must be made with this value, since "current" could change asynchronously as the document
            /// could be unregistered from a workspace.
            /// </summary>
            public DocumentId GetDocumentId(Workspace workspace)
            {
                if (_textBuffer == null)
                {
                    return null;
                }

                var textContainer = _textBuffer.AsTextContainer();
                return workspace.GetDocumentIdInCurrentContext(textContainer);
            }

            int IVsWindowFrameNotify.OnDockableChange(int fDockable)
            {
                return VSConstants.S_OK;
            }

            int IVsWindowFrameNotify.OnMove()
            {
                return VSConstants.S_OK;
            }

            int IVsWindowFrameNotify.OnShow(int fShow)
            {
                switch ((__FRAMESHOW)fShow)
                {
                    case __FRAMESHOW.FRAMESHOW_WinClosed:
                    case __FRAMESHOW.FRAMESHOW_WinHidden:
                    case __FRAMESHOW.FRAMESHOW_TabDeactivated:
                        return Disconnect();
                }

                return VSConstants.S_OK;
            }

            int IVsWindowFrameNotify.OnSize()
            {
                return VSConstants.S_OK;
            }

            int IVsWindowFrameNotify2.OnClose(ref uint pgrfSaveOptions)
            {
                return Disconnect();
            }

            private int Disconnect()
            {
                _documentTracker.AssertIsForeground();
                _documentTracker.RemoveFrame(this);

                if (_textBuffer != null)
                {
                    _textBuffer.Changed -= NonRoslynTextBuffer_Changed;
                }

                if (_frameEventsCookie != VSConstants.VSCOOKIE_NIL)
                {
                    return ((IVsWindowFrame2)Frame).Unadvise(_frameEventsCookie);
                }
                else
                {
                    return VSConstants.S_OK;
                }
            }
        }
    }
}
