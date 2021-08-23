// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
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
    internal class InheritanceMarginViewMargin : IWpfTextViewMargin
    {
        private readonly IWpfTextViewHost _textViewHost;
        private readonly IWpfTextView _textView;
        private readonly ITagAggregator<InheritanceMarginTag> _tagAggregator;
        private readonly IOptionService _optionService;
        private readonly InheritanceGlyphManager _glyphManager;
        private readonly string _languageName;
        private bool _refreshAllGlyphs;
        private bool _disposed;
        private readonly Grid _grid;
        private readonly Canvas _mainCanvas;

        // Same size as the Glyph Margin
        private const double HeightAndWidthOfMargin = 17;

        public InheritanceMarginViewMargin(
            IWpfTextViewHost textViewHost,
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            IUIThreadOperationExecutor operationExecutor,
            IClassificationFormatMap classificationFormatMap,
            ClassificationTypeMap classificationTypeMap,
            IEditorFormatMap editorFormatMap,
            IAsynchronousOperationListener listener,
            ITagAggregator<InheritanceMarginTag> tagAggregator,
            Document document)
        {
            _textViewHost = textViewHost;
            _textView = textViewHost.TextView;
            _tagAggregator = tagAggregator;
            _optionService = document.Project.Solution.Workspace.Services.GetRequiredService<IOptionService>();
            _languageName = document.Project.Language;
            _mainCanvas = new Canvas { ClipToBounds = true };
            _grid = new Grid();
            _grid.Children.Add(_mainCanvas);
            _glyphManager = new InheritanceGlyphManager(
                textViewHost.TextView,
                threadingContext,
                streamingFindUsagesPresenter,
                classificationTypeMap,
                classificationFormatMap,
                operationExecutor,
                editorFormatMap,
                listener,
                _mainCanvas);
            _refreshAllGlyphs = true;
            _disposed = false;

            _tagAggregator.BatchedTagsChanged += OnTagsChanged;
            _textView.LayoutChanged += OnLayoutChanged;
            _textView.ZoomLevelChanged += OnZoomLevelChanged;
            _optionService.OptionChanged += OnRoslynOptionChanged;

            _mainCanvas.Width = HeightAndWidthOfMargin;

            _grid.LayoutTransform = new ScaleTransform(
                scaleX: _textView.ZoomLevel / 100,
                scaleY: _textView.ZoomLevel / 100);
            _grid.LayoutTransform.Freeze();
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
                _glyphManager.RemoveGlyph(line.Extent);
                RefreshGlyphsOver(line);
            }

            _refreshAllGlyphs = false;
        }

        private void OnRoslynOptionChanged(object sender, OptionChangedEventArgs e)
        {
            if (e.Option == FeatureOnOffOptions.ShowInheritanceMargin || e.Option == FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin)
            {
                UpdateMarginVisibility();
            }
        }

        private void UpdateMarginVisibility()
        {
            var featureEnabled = _optionService.GetOption(FeatureOnOffOptions.ShowInheritanceMargin, _languageName) != false;
            var showMargin = !_optionService.GetOption(FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin, _languageName);
            if (showMargin && featureEnabled)
            {
                _mainCanvas.Visibility = Visibility.Visible;
            }
            else
            {
                _mainCanvas.Visibility = Visibility.Collapsed;
            }
        }

        private void OnTagsChanged(object sender, BatchedTagsChangedEventArgs e)
        {
            if (_textView.IsClosed)
            {
                return;
            }

            var changedSnapshotSpans = e.Spans.SelectMany(span => span.GetSpans(_textView.TextSnapshot)).ToImmutableArray();
            if (changedSnapshotSpans.Length == 0)
            {
                return;
            }

            var startOfChangedSpan = changedSnapshotSpans.Min(span => span.Start);
            var endOfChangedSpan = changedSnapshotSpans.Max(span => span.End);
            var changedSpan = new SnapshotSpan(startOfChangedSpan, endOfChangedSpan);

            _glyphManager.RemoveGlyph(changedSpan);

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
            if (!_optionService.GetOption(FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin, _languageName))
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

        #region IWpfTextViewMargin

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(InheritanceMarginViewMargin));
            }
        }

        public FrameworkElement VisualElement
        {
            get
            {
                ThrowIfDisposed();
                return _grid;
            }
        }

        public double MarginSize
        {
            get
            {
                ThrowIfDisposed();
                return _grid.ActualWidth;
            }
        }

        public bool Enabled
        {
            get
            {
                ThrowIfDisposed();
                return true;
            }
        }

        public ITextViewMargin? GetTextViewMargin(string marginName) =>
            marginName == nameof(InheritanceMarginViewMargin) ? this : null;

        public void Dispose()
        {
            if (!_disposed)
            {
                _tagAggregator.BatchedTagsChanged -= OnTagsChanged;
                _textView.LayoutChanged -= OnLayoutChanged;
                _textView.ZoomLevelChanged -= OnZoomLevelChanged;
                _optionService.OptionChanged -= OnRoslynOptionChanged;
                _tagAggregator.Dispose();
                _glyphManager.Dispose();
                _disposed = true;
            }
        }
        #endregion
    }
}
