// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class IWpfDifferenceViewerExtensions
    {
        private class SizeToFitHelper : ForegroundThreadAffinitizedObject
        {
            private readonly IWpfDifferenceViewer _diffViewer;
            private readonly double _minWidth;

            private double _width;
            private double _height;

            public SizeToFitHelper(IThreadingContext threadingContext, IWpfDifferenceViewer diffViewer, double minWidth)
                : base(threadingContext)
            {
                _diffViewer = diffViewer;
                _minWidth = minWidth;
            }

            public async Task SizeToFitAsync(CancellationToken cancellationToken)
            {
                await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
                await CalculateSizeAsync(cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

                // We have the height and width required to display the inline diff snapshot now.
                // Set the height and width of the IWpfDifferenceViewer accordingly.
                _diffViewer.VisualElement.Width = _width;
                _diffViewer.VisualElement.Height = _height;
            }

            private async Task<IProjectionSnapshot> GetInlineBufferSnapshotAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_diffViewer.DifferenceBuffer.CurrentInlineBufferSnapshot is { } snapshot)
                {
                    return snapshot;
                }

                var completionSource = new TaskCompletionSource<IProjectionSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
                _diffViewer.DifferenceBuffer.SnapshotDifferenceChanged += HandleSnapshotDifferenceChanged;

                // Handle cases where the snapshot was set between the previous check and the event registration
                if (_diffViewer.DifferenceBuffer.CurrentInlineBufferSnapshot is { } snapshot2)
                    completionSource.SetResult(snapshot2);

                try
                {
                    return await completionSource.Task.WithCancellation(cancellationToken).ConfigureAwaitRunInline();
                }
                finally
                {
                    _diffViewer.DifferenceBuffer.SnapshotDifferenceChanged -= HandleSnapshotDifferenceChanged;
                }

                // Local function
                void HandleSnapshotDifferenceChanged(object sender, SnapshotDifferenceChangeEventArgs e)
                {
                    // This event handler will only be called when the inline diff snapshot computation is complete.
                    Contract.ThrowIfNull(_diffViewer.DifferenceBuffer.CurrentInlineBufferSnapshot);

                    completionSource.SetResult(_diffViewer.DifferenceBuffer.CurrentInlineBufferSnapshot);
                }
            }

            private async Task CalculateSizeAsync(CancellationToken cancellationToken)
            {
                await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
                    snapshot = await GetInlineBufferSnapshotAsync(cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
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
            }

            private static bool IsNormal(double value)
                => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0;
        }

        public static Task SizeToFitAsync(this IWpfDifferenceViewer diffViewer, IThreadingContext threadingContext, double minWidth = 400.0, CancellationToken cancellationToken = default)
        {
            var helper = new SizeToFitHelper(threadingContext, diffViewer, minWidth);
            return helper.SizeToFitAsync(cancellationToken);
        }
    }
}
