// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    internal sealed class InteractiveDocumentNavigationService : IDocumentNavigationService
    {
        public bool CanNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan)
        {
            return true;
        }

        public bool CanNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset)
        {
            return false;
        }

        public bool CanNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0)
        {
            return false;
        }

        public bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, OptionSet options)
        {
            var interactiveWorkspace = workspace as InteractiveWorkspace;
            if (interactiveWorkspace == null)
            {
                Debug.Fail("InteractiveDocumentNavigationService called with incorrect workspace!");
                return false;
            }

            var textView = interactiveWorkspace.Engine.CurrentWindow.TextView;
            var document = interactiveWorkspace.CurrentSolution.GetDocument(documentId);

            var textSnapshot = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None).FindCorrespondingEditorTextSnapshot();
            if (textSnapshot == null)
            {
                return false;
            }

            var snapshotSpan = new SnapshotSpan(textSnapshot, textSpan.Start, textSpan.Length);
            var virtualSnapshotSpan = new VirtualSnapshotSpan(snapshotSpan);

            VirtualSnapshotSpan surfaceBufferSpan;
            if (!textView.TryGetSurfaceBufferSpan(virtualSnapshotSpan, out surfaceBufferSpan))
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

        public bool TryNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, OptionSet options)
        {
            throw new NotSupportedException();
        }

        public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, OptionSet options)
        {
            throw new NotSupportedException();
        }
    }
}
