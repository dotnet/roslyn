// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense
{
    internal partial class DebuggerTextView : IWpfTextView, IDebuggerTextView2, ITextView2
    {
        /// <summary>
        /// The actual debugger view of the watch or immediate window that we're wrapping
        /// </summary>
        private readonly IWpfTextView _innerTextView;
        private readonly IVsTextLines _debuggerTextLinesOpt;

        private IMultiSelectionBroker _multiSelectionBroker;

        // This name "CompletionRoot" is specified on the Editor side.
        // Roslyn must match the name.
        // The const should be removed with resolution of https://github.com/dotnet/roslyn/issues/31189
        public const string CompletionRoot = nameof(CompletionRoot);

        public DebuggerTextView(
            IWpfTextView innerTextView,
            IBufferGraph bufferGraph,
            IVsTextLines debuggerTextLinesOpt,
            bool isImmediateWindow)
        {
            _innerTextView = innerTextView;
            _debuggerTextLinesOpt = debuggerTextLinesOpt;
            BufferGraph = bufferGraph;
            IsImmediateWindow = isImmediateWindow;

            // The editor requires the current top buffer.
            // TODO it seems to be a hack. It should be removed.
            // Here is an issue to track: https://github.com/dotnet/roslyn/issues/31189
            // Starting debugging for the second time, we already have the property set.
            _innerTextView.Properties[CompletionRoot] = bufferGraph.TopBuffer;
        }

        /// <summary>
        /// We basically replace the innerTextView's BufferGraph with our own custom projection graph
        /// that projects the immediate window contents into a context buffer:
        /// 
        ///             (1)
        ///         (2)     (5)
        ///         (3)     (6)
        ///         (4)
        /// (1) Top level projection buffer - the subject buffer used by intellisense
        /// (2/3) Currently a double projection buffer combo that elides away the ? in the immediate window, and may add some 
        ///       boilerplate code to force an expression context.
        /// (4) innerTextView.TextBuffer, what the user actually sees in the watch/immediate windows
        /// (5) A read-only projection of (6)
        /// (6) The context buffer which is typically a source file
        /// 
        /// </summary>
        public IBufferGraph BufferGraph
        {
            get;
        }

        public bool IsImmediateWindow
        {
            get;
        }

        public uint StartBufferUpdate()
        {
            // null in unit tests
            if (_debuggerTextLinesOpt == null)
            {
                return 0;
            }

            _debuggerTextLinesOpt.GetStateFlags(out var bufferFlags);
            _debuggerTextLinesOpt.SetStateFlags((uint)((BUFFERSTATEFLAGS)bufferFlags & ~BUFFERSTATEFLAGS.BSF_USER_READONLY));
            return bufferFlags;
        }

        public void EndBufferUpdate(uint cookie)
        {
            // null in unit tests
            _debuggerTextLinesOpt?.SetStateFlags(cookie);
        }

        public ITextCaret Caret
        {
            get { return _innerTextView.Caret; }
        }

        public bool HasAggregateFocus
        {
            get { return _innerTextView.HasAggregateFocus; }
        }

        public bool InLayout
        {
            get { return _innerTextView.InLayout; }
        }

        public bool IsClosed
        {
            get { return _innerTextView.IsClosed; }
        }

        public bool IsMouseOverViewOrAdornments
        {
            get { return _innerTextView.IsMouseOverViewOrAdornments; }
        }

        public double LineHeight
        {
            get { return _innerTextView.LineHeight; }
        }

        public double MaxTextRightCoordinate
        {
            get { return _innerTextView.MaxTextRightCoordinate; }
        }

        public IEditorOptions Options
        {
            get { return _innerTextView.Options; }
        }

        public PropertyCollection Properties
        {
            get { return _innerTextView.Properties; }
        }

        public ITrackingSpan ProvisionalTextHighlight
        {
            get
            {
                return _innerTextView.ProvisionalTextHighlight;
            }

            set
            {
                _innerTextView.ProvisionalTextHighlight = value;
            }
        }

        public ITextViewRoleSet Roles
        {
            get { return _innerTextView.Roles; }
        }

        public ITextSelection Selection
        {
            get { return _innerTextView.Selection; }
        }

        public ITextViewLineCollection TextViewLines
        {
            get { return _innerTextView.TextViewLines; }
        }

        public ITextViewModel TextViewModel
        {
            get { return _innerTextView.TextViewModel; }
        }

        public IViewScroller ViewScroller
        {
            get { return _innerTextView.ViewScroller; }
        }

        public double ViewportBottom
        {
            get { return _innerTextView.ViewportBottom; }
        }

        public double ViewportHeight
        {
            get { return _innerTextView.ViewportHeight; }
        }

        public double ViewportLeft
        {
            get
            {
                return _innerTextView.ViewportLeft;
            }

            set
            {
                _innerTextView.ViewportLeft = value;
            }
        }

        public double ViewportRight
        {
            get { return _innerTextView.ViewportRight; }
        }

        public double ViewportTop
        {
            get { return _innerTextView.ViewportTop; }
        }

        public double ViewportWidth
        {
            get { return _innerTextView.ViewportWidth; }
        }

        public ITextSnapshot VisualSnapshot
        {
            get { return _innerTextView.VisualSnapshot; }
        }

        public ITextDataModel TextDataModel
        {
            get { return _innerTextView.TextDataModel; }
        }

        public ITextBuffer TextBuffer
        {
            get
            {
                return _innerTextView.TextBuffer;
            }
        }

        public ITextSnapshot TextSnapshot
        {
            get
            {
                return _innerTextView.TextSnapshot;
            }
        }

        public FrameworkElement VisualElement
        {
            get
            {
                return _innerTextView.VisualElement;
            }
        }

        public Brush Background
        {
            get
            {
                return _innerTextView.Background;
            }

            set
            {
                _innerTextView.Background = value;
            }
        }

        IWpfTextViewLineCollection IWpfTextView.TextViewLines
        {
            get
            {
                return _innerTextView.TextViewLines;
            }
        }

        public IFormattedLineSource FormattedLineSource
        {
            get
            {
                return _innerTextView.FormattedLineSource;
            }
        }

        public ILineTransformSource LineTransformSource
        {
            get
            {
                return _innerTextView.LineTransformSource;
            }
        }

        public double ZoomLevel
        {
            get
            {
                return _innerTextView.ZoomLevel;
            }

            set
            {
                _innerTextView.ZoomLevel = value;
            }
        }

        public bool InOuterLayout => throw new NotImplementedException();

        public IMultiSelectionBroker MultiSelectionBroker
        {
            get
            {
                _multiSelectionBroker ??= _innerTextView.GetMultiSelectionBroker();

                return _multiSelectionBroker;
            }
        }

        public void Close()
            => throw new NotSupportedException();

        public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride)
            => throw new NotSupportedException();

        public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo)
            => throw new NotSupportedException();

        public SnapshotSpan GetTextElementSpan(SnapshotPoint point)
            => throw new NotSupportedException();

        public ITextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition)
            => _innerTextView.GetTextViewLineContainingBufferPosition(bufferPosition);

        public void QueueSpaceReservationStackRefresh()
            => throw new NotSupportedException();

        public IAdornmentLayer GetAdornmentLayer(string name)
            => _innerTextView.GetAdornmentLayer(name);

        public ISpaceReservationManager GetSpaceReservationManager(string name)
            => _innerTextView.GetSpaceReservationManager(name);

        IWpfTextViewLine IWpfTextView.GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition)
            => _innerTextView.GetTextViewLineContainingBufferPosition(bufferPosition);

        public void Cleanup()
        {
            // The innerTextView of the immediate window never closes, but we want
            // our completion subscribers to unsubscribe from events when this
            // DebuggerTextView is no longer in use.
            if (this.IsImmediateWindow)
            {
                this.ClosedInternal?.Invoke(this, EventArgs.Empty);
            }

            _innerTextView.Properties.RemoveProperty(CompletionRoot);
        }

        public void QueuePostLayoutAction(Action action) => _innerTextView.QueuePostLayoutAction(action);

        public bool TryGetTextViewLines(out ITextViewLineCollection textViewLines) => _innerTextView.TryGetTextViewLines(out textViewLines);

        public bool TryGetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition, out ITextViewLine textViewLine)
            => _innerTextView.TryGetTextViewLineContainingBufferPosition(bufferPosition, out textViewLine);

        private event EventHandler ClosedInternal;

#pragma warning disable 67
        public event EventHandler MaxTextRightCoordinateChanged;
#pragma warning restore 67

        public event EventHandler Closed
        {
            add
            {
                if (this.IsImmediateWindow)
                {
                    this.ClosedInternal += value;
                }
                else
                {
                    _innerTextView.Closed += value;
                }
            }

            remove
            {
                if (this.IsImmediateWindow)
                {
                    this.ClosedInternal -= value;
                }
                else
                {
                    _innerTextView.Closed -= value;
                }
            }
        }

        public event EventHandler GotAggregateFocus
        {
            add { _innerTextView.GotAggregateFocus += value; }
            remove { _innerTextView.GotAggregateFocus -= value; }
        }

        public event EventHandler<TextViewLayoutChangedEventArgs> LayoutChanged
        {
            add { _innerTextView.LayoutChanged += value; }
            remove { _innerTextView.LayoutChanged -= value; }
        }

        public event EventHandler LostAggregateFocus
        {
            add { _innerTextView.LostAggregateFocus += value; }
            remove { _innerTextView.LostAggregateFocus -= value; }
        }

        public event EventHandler<MouseHoverEventArgs> MouseHover
        {
            add { _innerTextView.MouseHover += value; }
            remove { _innerTextView.MouseHover -= value; }
        }

        public event EventHandler ViewportHeightChanged
        {
            add { _innerTextView.ViewportHeightChanged += value; }
            remove { _innerTextView.ViewportHeightChanged -= value; }
        }

        public event EventHandler ViewportLeftChanged
        {
            add { _innerTextView.ViewportLeftChanged += value; }
            remove { _innerTextView.ViewportLeftChanged -= value; }
        }

        public event EventHandler ViewportWidthChanged
        {
            add { _innerTextView.ViewportWidthChanged += value; }
            remove { _innerTextView.ViewportWidthChanged -= value; }
        }

        public event EventHandler<BackgroundBrushChangedEventArgs> BackgroundBrushChanged
        {
            add { _innerTextView.BackgroundBrushChanged += value; }
            remove { _innerTextView.BackgroundBrushChanged -= value; }
        }

        public event EventHandler<ZoomLevelChangedEventArgs> ZoomLevelChanged
        {
            add { _innerTextView.ZoomLevelChanged += value; }
            remove { _innerTextView.ZoomLevelChanged -= value; }
        }
    }
}
