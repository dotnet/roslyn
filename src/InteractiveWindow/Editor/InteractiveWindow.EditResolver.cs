// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal partial class InteractiveWindow
    {
        private sealed class EditResolver : IProjectionEditResolver
        {
            private readonly InteractiveWindow _window;

            public EditResolver(InteractiveWindow window)
            {
                _window = window;
            }

            // We always favor the last buffer of our language type.  This handles cases where we're on a boundary between a prompt and a language 
            // buffer - we favor the language buffer because the prompts cannot be edited.  In the case of two language buffers this also works because
            // our spans are laid out like:
            // <lang span 1 including newline>
            // <prompt span><lang span 2>
            // 
            // In the case where the prompts are in the margin we have an insertion conflict between the two language spans.  But because
            // lang span 1 includes the new line in order to be on the boundary we need to be on lang span 2's line.
            // 
            // This works the same way w/ our input buffer where the input buffer present instead of <lang span 2>.

            void IProjectionEditResolver.FillInInsertionSizes(SnapshotPoint projectionInsertionPoint, ReadOnlyCollection<SnapshotPoint> sourceInsertionPoints, string insertionText, IList<int> insertionSizes)
            {
                int index = _window.UIThread(uiOnly => IndexOfEditableBuffer(sourceInsertionPoints, uiOnly));
                if (index != -1)
                {
                    insertionSizes[index] = insertionText.Length;
                }
            }

            int IProjectionEditResolver.GetTypicalInsertionPosition(SnapshotPoint projectionInsertionPoint, ReadOnlyCollection<SnapshotPoint> sourceInsertionPoints)
            {
                int index = _window.UIThread(uiOnly => IndexOfEditableBuffer(sourceInsertionPoints, uiOnly));
                return index != -1 ? index : 0;
            }

            void IProjectionEditResolver.FillInReplacementSizes(SnapshotSpan projectionReplacementSpan, ReadOnlyCollection<SnapshotSpan> sourceReplacementSpans, string insertionText, IList<int> insertionSizes)
            {
                int index = _window.UIThread(uiOnly => IndexOfEditableBuffer(sourceReplacementSpans, uiOnly));
                if (index != -1)
                {
                    insertionSizes[index] = insertionText.Length;
                }
            }

            private int IndexOfEditableBuffer(ReadOnlyCollection<SnapshotPoint> points, UIThreadOnly uiOnly)
            {
                Debug.Assert(_window.OnUIThread());
                for (int i = points.Count - 1; i >= 0; i--)
                {
                    if (IsEditableBuffer(points[i].Snapshot.TextBuffer, uiOnly))
                    {
                        return i;
                    }
                }

                return -1;
            }

            private int IndexOfEditableBuffer(ReadOnlyCollection<SnapshotSpan> spans, UIThreadOnly uiOnly)
            {
                Debug.Assert(_window.OnUIThread());
                for (int i = spans.Count - 1; i >= 0; i--)
                {
                    if (IsEditableBuffer(spans[i].Snapshot.TextBuffer, uiOnly))
                    {
                        return i;
                    }
                }

                return -1;
            }

            private bool IsEditableBuffer(ITextBuffer buffer, UIThreadOnly uiOnly)
            {
                return buffer == uiOnly.CurrentLanguageBuffer || buffer == uiOnly.StandardInputBuffer;
            }
        }
    }
}