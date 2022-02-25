// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    internal sealed class InteractiveDocumentNavigationService : IDocumentNavigationService
    {
        private readonly IThreadingContext _threadingContext;

        public InteractiveDocumentNavigationService(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public async Task<bool> CanNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            // This switch is technically not needed as the call to CanNavigateToSpan just returns 'true'.
            // However, this abides by the contract that CanNavigateToSpan only be called on the UI thread.
            // It also means if we ever update that method, this code will stay corrrect.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return CanNavigateToSpan(workspace, documentId, textSpan, cancellationToken);
        }

        public bool CanNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
            => true;

        public bool CanNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken)
            => false;

        public bool CanNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken)
            => false;

        public async Task<bool> TryNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, bool allowInvalidSpan, CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return TryNavigateToSpan(workspace, documentId, textSpan, options, allowInvalidSpan, cancellationToken);
        }

        public bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, bool allowInvalidSpan, CancellationToken cancellationToken)
        {
            if (workspace is not InteractiveWindowWorkspace interactiveWorkspace)
            {
                Debug.Fail("InteractiveDocumentNavigationService called with incorrect workspace!");
                return false;
            }

            if (interactiveWorkspace.Window is null)
            {
                Debug.Fail("We are trying to navigate with a workspace that doesn't have a window!");
                return false;
            }

            var textView = interactiveWorkspace.Window.TextView;
            var document = interactiveWorkspace.CurrentSolution.GetDocument(documentId);

            var textSnapshot = document?.GetTextSynchronously(cancellationToken).FindCorrespondingEditorTextSnapshot();
            if (textSnapshot == null)
            {
                return false;
            }

            var snapshotSpan = new SnapshotSpan(textSnapshot, textSpan.Start, textSpan.Length);
            var virtualSnapshotSpan = new VirtualSnapshotSpan(snapshotSpan);

            if (!textView.TryGetSurfaceBufferSpan(virtualSnapshotSpan, out var surfaceBufferSpan))
            {
                return false;
            }

            textView.Selection.Select(surfaceBufferSpan.Start, surfaceBufferSpan.End);
            textView.ViewScroller.EnsureSpanVisible(surfaceBufferSpan.SnapshotSpan, EnsureSpanVisibleOptions.AlwaysCenter);

            // Moving the caret must be the last operation involving surfaceBufferSpan because 
            // it might update the version number of textView.TextSnapshot (VB does line commit
            // when the caret leaves a line which might cause pretty listing), which must be 
            // equal to surfaceBufferSpan.SnapshotSpan.Snapshot's version number.
            textView.Caret.MoveTo(surfaceBufferSpan.Start);

            textView.VisualElement.Focus();

            return true;
        }

        public bool TryNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, NavigationOptions options, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, NavigationOptions options, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<bool> TryNavigateToPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, NavigationOptions options, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
