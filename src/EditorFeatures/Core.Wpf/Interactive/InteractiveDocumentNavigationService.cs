// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal sealed class InteractiveDocumentNavigationService : IDocumentNavigationService
    {
        private readonly IThreadingContext _threadingContext;

        public InteractiveDocumentNavigationService(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public Task<bool> CanNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken)
            => SpecializedTasks.True;

        public Task<bool> CanNavigateToPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public async Task<INavigableLocation?> GetLocationForSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            if (workspace is not InteractiveWindowWorkspace interactiveWorkspace)
            {
                Debug.Fail("InteractiveDocumentNavigationService called with incorrect workspace!");
                return null;
            }

            if (interactiveWorkspace.Window is null)
            {
                Debug.Fail("We are trying to navigate with a workspace that doesn't have a window!");
                return null;
            }

            var textView = interactiveWorkspace.Window.TextView;
            var document = interactiveWorkspace.CurrentSolution.GetDocument(documentId);
            if (document is null)
                return null;

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var textSnapshot = text.FindCorrespondingEditorTextSnapshot();
            if (textSnapshot == null)
                return null;

            var snapshotSpan = new SnapshotSpan(textSnapshot, textSpan.Start, textSpan.Length);
            var virtualSnapshotSpan = new VirtualSnapshotSpan(snapshotSpan);

            if (!textView.TryGetSurfaceBufferSpan(virtualSnapshotSpan, out var surfaceBufferSpan))
            {
                return null;
            }

            return new NavigableLocation(async (options, cancellationToken) =>
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                textView.Selection.Select(surfaceBufferSpan.Start, surfaceBufferSpan.End);
                textView.ViewScroller.EnsureSpanVisible(surfaceBufferSpan.SnapshotSpan, EnsureSpanVisibleOptions.AlwaysCenter);

                // Moving the caret must be the last operation involving surfaceBufferSpan because 
                // it might update the version number of textView.TextSnapshot (VB does line commit
                // when the caret leaves a line which might cause pretty listing), which must be 
                // equal to surfaceBufferSpan.SnapshotSpan.Snapshot's version number.
                textView.Caret.MoveTo(surfaceBufferSpan.Start);

                textView.VisualElement.Focus();

                return true;
            });
        }

        public Task<INavigableLocation?> GetLocationForPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken)
            => SpecializedTasks.Null<INavigableLocation>();
    }
}
