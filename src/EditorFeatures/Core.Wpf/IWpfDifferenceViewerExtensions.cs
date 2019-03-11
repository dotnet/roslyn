// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class IWpfDifferenceViewerExtensions
    {
        private class SizeToFitHelper : ForegroundThreadAffinitizedObject
        {
            private int _calculationStarted;
            private readonly IWpfDifferenceViewer _diffViewer;
            private readonly TaskCompletionSource<object> _taskCompletion;

            private readonly double _minWidth;
            private double _width;
            private double _height;

            public SizeToFitHelper(IThreadingContext threadingContext, IWpfDifferenceViewer diffViewer, double minWidth)
                : base(threadingContext)
            {
                _calculationStarted = 0;
                _diffViewer = diffViewer;
                _minWidth = minWidth;
                _taskCompletion = new TaskCompletionSource<object>();
            }

            public async Task SizeToFitAsync()
            {
                // The following work must always happen on UI thread.
                AssertIsForeground();

                // We won't know how many lines there will be in the inline diff or how
                // wide the widest line in the inline diff will be until the inline diff
                // snapshot has been computed. We register an event handler here that will
                // allow us to calculate the required width and height once the inline diff
                // snapshot has been computed.
                _diffViewer.DifferenceBuffer.SnapshotDifferenceChanged += SnapshotDifferenceChanged;

                // The inline diff snapshot may already have been computed before we registered the
                // above event handler. In this case, we can go ahead and calculate the required width
                // and height.
                CalculateSize();

                // IDifferenceBuffer calculates the inline diff snapshot on the UI thread (on idle).
                // Since we are already on the UI thread, we need to yield control so that the
                // inline diff snapshot computation (and the event handler we registered above to
                // calculate required width and height) get a chance to run and we need to wait until
                // this computation is complete. Once computation is complete, the width and height
                // need to be set from the UI thread. We use ConfigureAwait(true) to stay on the UI thread.
                await _taskCompletion.Task.ConfigureAwait(true);

                // The following work must always happen on UI thread.
                AssertIsForeground();

                // We have the height and width required to display the inline diff snapshot now.
                // Set the height and width of the IWpfDifferenceViewer accordingly.
                _diffViewer.VisualElement.Width = _width;
                _diffViewer.VisualElement.Height = _height;
            }

            private void SnapshotDifferenceChanged(object sender, SnapshotDifferenceChangeEventArgs args)
            {
                // The following work must always happen on UI thread.
                AssertIsForeground();

                // This event handler will only be called when the inline diff snapshot computation is complete.
                Contract.ThrowIfNull(_diffViewer.DifferenceBuffer.CurrentInlineBufferSnapshot);

                // We can go ahead and calculate the required height and width now.
                CalculateSize();
            }

            private void CalculateSize()
            {
                // The following work must always happen on UI thread.
                AssertIsForeground();

                if ((_diffViewer.DifferenceBuffer.CurrentInlineBufferSnapshot == null) ||
                    (Interlocked.CompareExchange(ref _calculationStarted, 1, 0) == 1))
                {
                    // Return if inline diff snapshot is not yet ready or
                    // if the size calculation is already in progress.
                    return;
                }

                // Unregister the event handler - we don't need it anymore since the inline diff
                // snapshot is available at this point.
                _diffViewer.DifferenceBuffer.SnapshotDifferenceChanged -= SnapshotDifferenceChanged;

                IWpfTextView textView;
                ITextSnapshot snapshot;
                if (_diffViewer.ViewMode == DifferenceViewMode.RightViewOnly)
                {
                    textView = _diffViewer.RightView;
                    snapshot = _diffViewer.DifferenceBuffer.RightBuffer.CurrentSnapshot;
                }
                else if (_diffViewer.ViewMode == DifferenceViewMode.LeftViewOnly)
                {
                    textView = _diffViewer.LeftView;
                    snapshot = _diffViewer.DifferenceBuffer.LeftBuffer.CurrentSnapshot;
                }
                else
                {
                    textView = _diffViewer.InlineView;
                    snapshot = _diffViewer.DifferenceBuffer.CurrentInlineBufferSnapshot;
                }

                // Perform a layout without actually rendering the content on the screen so that
                // we can calculate the exact height and width required to render the content on
                // the screen before actually rendering it. This helps us avoiding the flickering
                // effect that would be caused otherwise when the UI is rendered twice with
                // different sizes.
                textView.DisplayTextLineContainingBufferPosition(
                    new SnapshotPoint(snapshot, 0),
                    0.0, ViewRelativePosition.Top, double.MaxValue, double.MaxValue);

                _width = Math.Max(textView.MaxTextRightCoordinate * (textView.ZoomLevel / 100), _minWidth); // Width of the widest line.
                Contract.ThrowIfFalse(IsNormal(_width));

                _height = textView.LineHeight * (textView.ZoomLevel / 100) * // Height of each line.
                         snapshot.LineCount;                                // Number of lines.
                Contract.ThrowIfFalse(IsNormal(_height));

                // Calculation of required height and width is now complete.
                _taskCompletion.SetResult(null);
            }

            private static bool IsNormal(double value)
            {
                return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0;
            }
        }

        public static Task SizeToFitAsync(this IWpfDifferenceViewer diffViewer, IThreadingContext threadingContext, double minWidth = 400.0)
        {
            var helper = new SizeToFitHelper(threadingContext, diffViewer, minWidth);
            return helper.SizeToFitAsync();
        }
    }
}
