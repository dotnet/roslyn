// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin
{
    internal class InheritanceMarginViewMargin : ForegroundThreadAffinitizedObject, IWpfTextViewMargin
    {
        // 16 (width of the crisp image) + 2 * 1 (width of the border) = 18
        private const double HeightAndWidthOfMargin = 18;
        private readonly IWpfTextView _textView;
        private readonly ITagAggregator<InheritanceMarginTag> _tagAggregator;
        private readonly IGlobalOptionService _globalOptions;
        private readonly InheritanceGlyphManager _glyphManager;
        private readonly string _languageName;
        private readonly Grid _grid;
        private readonly Canvas _mainCanvas;

        /// <summary>
        /// A flag indicates all the glyphs in this margin needs be refreshed when the Layout of the TextView changes.
        /// Should only be read or written to by the UI thread.
        /// </summary>
        private bool _refreshAllGlyphs;
        private bool _disposed;

        public InheritanceMarginViewMargin(IWpfTextView textView,
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            IUIThreadOperationExecutor operationExecutor,
            IClassificationFormatMap classificationFormatMap,
            ClassificationTypeMap classificationTypeMap,
            ITagAggregator<InheritanceMarginTag> tagAggregator,
            IEditorFormatMap editorFormatMap,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListener listener,
            string languageName) : base(threadingContext)
        {
            _textView = textView;
            _tagAggregator = tagAggregator;
            _globalOptions = globalOptions;
            _languageName = languageName;
            _mainCanvas = new Canvas { ClipToBounds = true, Width = HeightAndWidthOfMargin };
            _grid = new Grid();
            _grid.Children.Add(_mainCanvas);
            _glyphManager = new InheritanceGlyphManager(
                textView,
                threadingContext,
                streamingFindUsagesPresenter,
                classificationFormatMap,
                classificationTypeMap,
                operationExecutor,
                editorFormatMap,
                listener,
                _mainCanvas,
                HeightAndWidthOfMargin);
            _refreshAllGlyphs = true;
            _disposed = false;

            _tagAggregator.BatchedTagsChanged += OnTagsChanged;
            _textView.LayoutChanged += OnLayoutChanged;
            _textView.ZoomLevelChanged += OnZoomLevelChanged;
            _globalOptions.OptionChanged += OnGlobalOptionChanged;

            _grid.LayoutTransform = new ScaleTransform(
                scaleX: _textView.ZoomLevel / 100,
                scaleY: _textView.ZoomLevel / 100);
            _grid.LayoutTransform.Freeze();
            UpdateMarginVisibility();
        }

        void IDisposable.Dispose()
        {
            AssertIsForeground();
            if (!_disposed)
            {
                _disposed = true;
                _tagAggregator.BatchedTagsChanged -= OnTagsChanged;
                _textView.LayoutChanged -= OnLayoutChanged;
                _textView.ZoomLevelChanged -= OnZoomLevelChanged;
                _globalOptions.OptionChanged -= OnGlobalOptionChanged;
                _tagAggregator.Dispose();
                ((IDisposable)_glyphManager).Dispose();
            }
        }

        private void OnZoomLevelChanged(object sender, ZoomLevelChangedEventArgs e)
        {
            _grid.LayoutTransform = e.ZoomTransform;
            _refreshAllGlyphs = true;
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            _glyphManager.SetSnapshotAndUpdate(
                _textView.TextSnapshot,
                e.NewOrReformattedLines,
                e.VerticalTranslation ? _textView.TextViewLines : e.TranslatedLines);

            IList<ITextViewLine> lines = _refreshAllGlyphs ? _textView.TextViewLines : e.NewOrReformattedLines;
            foreach (var line in lines)
            {
                _glyphManager.RemoveGlyphs(line.Extent);
                RefreshGlyphsOver(line);
            }

            _refreshAllGlyphs = false;
        }

        private void OnGlobalOptionChanged(object sender, OptionChangedEventArgs e)
        {
            if (e.Option.Equals(FeatureOnOffOptions.ShowInheritanceMargin) ||
                e.Option.Equals(FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin))
            {
                UpdateMarginVisibility();
            }
        }

        private void UpdateMarginVisibility()
        {
            _mainCanvas.Visibility =
                (_globalOptions.GetOption(FeatureOnOffOptions.ShowInheritanceMargin, _languageName) ?? true) &&
                !_globalOptions.GetOption(FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnTagsChanged(object sender, BatchedTagsChangedEventArgs e)
        {
            if (_textView.IsClosed)
            {
                return;
            }

            using var _ = CodeAnalysis.PooledObjects.ArrayBuilder<SnapshotSpan>.GetInstance(out var builder);
            foreach (var mappingSpan in e.Spans)
            {
                var normalizedSpan = mappingSpan.GetSpans(_textView.TextSnapshot);
                builder.AddRange(normalizedSpan);
            }

            var changedSnapshotSpans = builder.ToImmutable();
            if (changedSnapshotSpans.Length == 0)
            {
                return;
            }

            var startOfChangedSpan = changedSnapshotSpans.Min(span => span.Start);
            var endOfChangedSpan = changedSnapshotSpans.Max(span => span.End);
            var changedSpan = new SnapshotSpan(startOfChangedSpan, endOfChangedSpan);

            _glyphManager.RemoveGlyphs(changedSpan);

            foreach (var line in _textView.TextViewLines.GetTextViewLinesIntersectingSpan(changedSpan))
            {
                if (line.IsValid)
                {
                    RefreshGlyphsOver(line);
                }
            }
        }

        private void RefreshGlyphsOver(ITextViewLine textViewLine)
        {
            if (!_globalOptions.GetOption(FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin))
            {
                foreach (var mappingTagSpan in _tagAggregator.GetTags(textViewLine.ExtentAsMappingSpan))
                {
                    // Only take tag spans with a visible start point and that map to something
                    // in the edit buffer and *start* on this line
                    if (mappingTagSpan.Span.Start.GetPoint(_textView.VisualSnapshot.TextBuffer, PositionAffinity.Predecessor) != null)
                    {
                        var tagSpans = mappingTagSpan.Span.GetSpans(_textView.TextSnapshot);
                        if (tagSpans.Count > 0)
                        {
                            _glyphManager.AddGlyph(mappingTagSpan.Tag, tagSpans[0]);
                        }
                    }
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(InheritanceMarginViewMargin));
            }
        }

        FrameworkElement IWpfTextViewMargin.VisualElement
        {
            get
            {
                ThrowIfDisposed();
                return _grid;
            }
        }

        double ITextViewMargin.MarginSize
        {
            get
            {
                ThrowIfDisposed();
                return _grid.ActualWidth;
            }
        }

        bool ITextViewMargin.Enabled
        {
            get
            {
                ThrowIfDisposed();
                return true;
            }
        }

        ITextViewMargin? ITextViewMargin.GetTextViewMargin(string marginName)
            => marginName == nameof(InheritanceMarginViewMargin) ? this : null;
    }
}
