// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    internal sealed class VsReadOnlyDocumentTracker : ForegroundThreadAffinitizedObject, IDisposable
    {
        private readonly IEditAndContinueService _encService;
        private readonly IVsEditorAdaptersFactoryService _adapters;
        private readonly Workspace _workspace;

        private bool _isDisposed;

        internal static readonly TraceLog log = new TraceLog(2048, "VsReadOnlyDocumentTracker");

        public VsReadOnlyDocumentTracker(IThreadingContext threadingContext, IEditAndContinueService encService, IVsEditorAdaptersFactoryService adapters)
            : base(threadingContext, assertIsForeground: true)
        {
            Debug.Assert(encService.DebuggingSession != null);

            _encService = encService;
            _adapters = adapters;
            _workspace = encService.DebuggingSession.InitialSolution.Workspace;

            _workspace.DocumentOpened += OnDocumentOpened;
            UpdateWorkspaceDocuments();
        }

        private void OnDocumentOpened(object sender, DocumentEventArgs e)
        {
            InvokeBelowInputPriorityAsync(() =>
            {
                if (!_isDisposed)
                {
                    SetReadOnly(e.Document);
                }
            });
        }

        internal void UpdateWorkspaceDocuments()
        {
            foreach (var documentId in _workspace.GetOpenDocumentIds())
            {
                var document = _workspace.CurrentSolution.GetDocument(documentId);
                if (document != null)
                {
                    SetReadOnly(document);
                }
            }
        }

        public void Dispose()
        {
            AssertIsForeground();

            if (_isDisposed)
            {
                return;
            }

            _workspace.DocumentOpened -= OnDocumentOpened;

            UpdateWorkspaceDocuments();

            // event handlers may be queued after the disposal - they will be a no-op
            _isDisposed = true;
        }

        private void SetReadOnly(Document document)
        {
            // Only set documents read-only if they're part of a project that supports Enc.

            if (document.Project.Solution.Workspace is VisualStudioWorkspace workspace && VsENCRebuildableProjectImpl.TryGetRebuildableProject(document.Project.Id) != null)
            {
                SetReadOnly(document.Id, _encService.IsProjectReadOnly(document.Project.Id, out var sessionReason, out var projectReason) && AllowsReadOnly(document.Id));
            }
        }

        private bool AllowsReadOnly(DocumentId documentId)
        {
            // All documents of regular running projects are read-only until the debugger breaks the app.
            // However, ASP.NET doesn’t want its views (aspx, cshtml, or vbhtml) to be read-only, so they can be editable
            // while the code is running and get refreshed next time the web page is hit.

            // Note that Razor-like views are modelled as a ContainedDocument
            var containedDocument = ContainedDocument.TryGetContainedDocument(documentId);
            return containedDocument == null;
        }

        internal void SetReadOnly(DocumentId documentId, bool value)
        {
            AssertIsForeground();
            Debug.Assert(!_isDisposed);

            var textBuffer = GetTextBuffer(_workspace, documentId);
            if (textBuffer != null)
            {
                var vsBuffer = _adapters.GetBufferAdapter(textBuffer);
                if (vsBuffer != null)
                {
                    SetReadOnlyFlag(vsBuffer, value);
                }
            }
        }

        private void SetReadOnlyFlag(IVsTextBuffer buffer, bool value)
        {
            uint newFlags;
            buffer.GetStateFlags(out var oldFlags);
            if (value)
            {
                newFlags = oldFlags | (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY;
            }
            else
            {
                newFlags = oldFlags & ~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY;
            }

            if (oldFlags != newFlags)
            {
                buffer.SetStateFlags(newFlags);
            }
        }

        private static ITextBuffer GetTextBuffer(Workspace workspace, DocumentId documentId)
        {
            var doc = workspace.CurrentSolution.GetDocument(documentId);
            if (doc == null)
            {
                // TODO (https://github.com/dotnet/roslyn/issues/1204): this check should be unnecessary.
                if (documentId != null)
                {
                    log.Write("GetTextBuffer: document not found for '#{0} - {1}'", documentId.Id.ToString(), documentId.DebugName);
                }
                else
                {
                    log.Write("GetTextBuffer: document not found");
                }

                return null;
            }

            if (!doc.TryGetText(out var text))
            {
                // TODO: should not happen since the document is open (see bug 896058)
                return null;
            }

            var snapshot = text.FindCorrespondingEditorTextSnapshot();
            if (snapshot == null)
            {
                return null;
            }

            return snapshot.TextBuffer;
        }
    }
}
