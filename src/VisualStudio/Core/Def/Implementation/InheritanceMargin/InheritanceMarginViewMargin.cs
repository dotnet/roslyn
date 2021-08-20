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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin
{
    internal partial class InheritanceMarginViewMargin : IWpfTextViewMargin
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
                _mainCanvas);
            _refreshAllGlyphs = true;
            _disposed = false;

            _tagAggregator.BatchedTagsChanged += OnTagsChanged;
            _textView.LayoutChanged += OnLayoutChanged;
            _textView.ZoomLevelChanged += OnZoomLevelChanged;
            _textView.Options.OptionChanged += OnTextViewOptionChanged;
            _optionService.OptionChanged += OnRoslynOptionChanged;
            ((FrameworkElement)_textViewHost).Loaded += OnTextViewHostLoaded;

            _mainCanvas.Width = HeightAndWidthOfMargin;

            _grid.LayoutTransform = new ScaleTransform(
                scaleX: _textView.ZoomLevel / 100,
                scaleY: _textView.ZoomLevel / 100);
            _grid.LayoutTransform.Freeze();
        }

        private void OnTextViewHostLoaded(object sender, RoutedEventArgs e)
            => UpdateMarginPosition();

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

        private void OnTextViewOptionChanged(object sender, EditorOptionChangedEventArgs e)
        {
            if (e.OptionId == DefaultTextViewHostOptions.GlyphMarginName)
            {
                UpdateMarginPosition();
            }
        }

        private void OnRoslynOptionChanged(object sender, OptionChangedEventArgs e)
        {
            if (e.Option == FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin)
            {
                UpdateMarginPosition();
            }

            if (e.Option == FeatureOnOffOptions.ShowInheritanceMargin)
            {
                UpdateVisibilityOfMargin();
            }
        }

        private void UpdateMarginPosition()
        {
            var isGlyphMarginOpen = _textView.Options.GetOptionValue(DefaultTextViewHostOptions.GlyphMarginId);

            // Make sure the GlyphMargin is avaliable before we try to get it. If it is disposed/closed then don't do anything
            if (!isGlyphMarginOpen)
            {
                return;
            }

            var glyphTextViewMargin = _textViewHost.GetTextViewMargin(PredefinedMarginNames.Glyph);
            if (glyphTextViewMargin is null || glyphTextViewMargin.VisualElement is not Grid indicatorMarginGrid)
            {
                return;
            }

            var shouldCombinedWithIndicatorMargin = _optionService.GetOption(FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin, _languageName);
            var isCanvasCombined = indicatorMarginGrid.Children.Contains(_mainCanvas);
            if (shouldCombinedWithIndicatorMargin && !isCanvasCombined)
            {
                RoslynDebug.Assert(_grid.Children.Contains(_mainCanvas));
                _grid.Children.Remove(_mainCanvas);
                indicatorMarginGrid.Children.Add(_mainCanvas);
                return;
            }

            if (!shouldCombinedWithIndicatorMargin && isCanvasCombined)
            {
                RoslynDebug.Assert(_grid.Children.Count == 0);
                indicatorMarginGrid.Children.Remove(_mainCanvas);
                _grid.Children.Add(_mainCanvas);
                return;
            }
        }

        private void UpdateVisibilityOfMargin()
        {
            var featureEnabled = _optionService.GetOption(FeatureOnOffOptions.ShowInheritanceMargin, _languageName);
            if (featureEnabled == false)
            {
                _mainCanvas.Visibility = Visibility.Collapsed;
            }
            else
            {
                _mainCanvas.Visibility = Visibility.Visible;
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
                _textView.Options.OptionChanged -= OnTextViewOptionChanged;
                _optionService.OptionChanged -= OnRoslynOptionChanged;
                ((FrameworkElement)_textViewHost).Loaded -= OnTextViewHostLoaded;
                _tagAggregator.Dispose();
                _disposed = true;
            }
        }
        #endregion
    }
}
