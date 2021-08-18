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
    internal partial class InheritanceMarginViewMargin : UserControl, IWpfTextViewMargin
    {
        private readonly IWpfTextViewHost _textViewHost;
        private readonly IWpfTextView _textView;
        private readonly ITagAggregator<InheritanceMarginTag> _tagAggregator;
        private readonly IThreadingContext _threadingContext;
        private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly IUIThreadOperationExecutor _operationExecutor;
        private readonly IOptionService _optionService;
        private readonly string _languageName;
        private Dictionary<SnapshotSpan, MarginGlyph.InheritanceMargin> _snapshotSpanToMargin;
        // Same size as the glyph margin
        private const double HeightAndWidthOfMargin = 18;

        public InheritanceMarginViewMargin(
            IWpfTextViewHost textViewHost,
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            IUIThreadOperationExecutor operationExecutor,
            IClassificationFormatMap classificationFormatMap,
            ClassificationTypeMap classificationTypeMap,
            ITagAggregator<InheritanceMarginTag> tagAggregator,
            Document document)
        {
            InitializeComponent();
            _textViewHost = textViewHost;
            _threadingContext = threadingContext;
            _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
            _classificationFormatMap = classificationFormatMap;
            _classificationTypeMap = classificationTypeMap;
            _operationExecutor = operationExecutor;
            _textView = textViewHost.TextView;
            _tagAggregator = tagAggregator;
            _snapshotSpanToMargin = new Dictionary<SnapshotSpan, MarginGlyph.InheritanceMargin>();
            _optionService = document.Project.Solution.Workspace.Services.GetRequiredService<IOptionService>();
            _languageName = document.Project.Language;

            _tagAggregator.BatchedTagsChanged += OnTagsChanged;
            _textView.LayoutChanged += OnLayoutChanged;
            _textView.ZoomLevelChanged += OnZoomLevelChanged;
            _textView.Options.OptionChanged += OnTextViewOptionChanged;
            _optionService.OptionChanged += OnRoslynOptionChanged;

            MainCanvas.Width = MarginSize;

            MainCanvas.LayoutTransform = new ScaleTransform(
                scaleX: _textView.ZoomLevel / 100,
                scaleY: _textView.ZoomLevel / 100);
            MainCanvas.LayoutTransform.Freeze();
        }

        private void OnZoomLevelChanged(object sender, ZoomLevelChangedEventArgs e)
        {
            // Set ZoomTransform for Canvas.
            // Don't need to update the ZoomLevel for glpyhs here, when zoom level changes, OnLayoutChanged will be
            // fired all the margin will be recreated.
            MainCanvas.LayoutTransform = e.ZoomTransform;
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            SetSnapshotAndUpdate(e.NewOrReformattedLines);
            foreach (var line in e.NewOrReformattedLines)
            {
                RemoveGlyphByVisualSpan(line.Extent);
                RefreshGlyphsOver(line);
            }
        }

        private void OnTextViewOptionChanged(object sender, EditorOptionChangedEventArgs e)
        {
            if (e.OptionId == DefaultTextViewHostOptions.GlyphMarginName)
            {
                UpdateCombinedMarginPositions();
            }
        }

        private void OnRoslynOptionChanged(object sender, OptionChangedEventArgs e)
        {
            if (e.Option == FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin)
            {
                UpdateCombinedMarginPositions();
            }
        }

        private void UpdateCombinedMarginPositions()
        {
            var marginOffset = GetMarginOffset();
            if (marginOffset == 0)
            {
                MainCanvas.Width = HeightAndWidthOfMargin;
            }
            else
            {
                MainCanvas.Width = 0;
            }

            foreach (var (_, margin) in _snapshotSpanToMargin)
            {
                Canvas.SetLeft(margin, marginOffset);
            }
        }

        private double GetMarginOffset()
        {
            var inheritanceMarginCombinedWithIndicatorMargin = _optionService.GetOption(FeatureOnOffOptions.InheritanceMarginCombinedWithIndicatorMargin, _languageName)
                && _textView.Options.GetOptionValue(DefaultTextViewHostOptions.GlyphMarginId);

            if (inheritanceMarginCombinedWithIndicatorMargin
                /*&& _textViewHost.GetTextViewMargin(DefaultTextViewHostOptions.GlyphMarginName) is { Enabled: true, VisualElement: UIElement glyphMargin }*/)
            {
                return -HeightAndWidthOfMargin;
                //return glyphMargin.TranslatePoint(new Point(0, 0), this).X;
            }
            else
            {
                return 0;
            }
        }

        private void SetSnapshotAndUpdate(IReadOnlyCollection<ITextViewLine> newOrReformattedLines)
        {
            var newSnapPointToMarginMap = new Dictionary<SnapshotSpan, MarginGlyph.InheritanceMargin>(_snapshotSpanToMargin.Count);
            foreach (var (span, margin) in _snapshotSpanToMargin)
            {
                var translatedSpan = span.TranslateTo(_textView.TextSnapshot, SpanTrackingMode.EdgeInclusive);
                var containingLine = _textView.TextViewLines.GetTextViewLineContainingBufferPosition(translatedSpan.Start);

                // Remove the glyph from Canvas if
                // 1. If the line is no longer in the _textView
                // 2. If the glyph enters in the newOrReformattedLines.
                // (e.g. If a region in the editor is collapsed, and there is a margin in the region, we should remove it)
                if (containingLine == null)
                {
                    MainCanvas.Children.Remove(margin);
                }
                else if (newOrReformattedLines.Any(line => line.IntersectsBufferSpan(translatedSpan)))
                {
                    MainCanvas.Children.Remove(margin);
                }
                else
                {
                    newSnapPointToMarginMap[translatedSpan] = margin;
                    Canvas.SetTop(margin, containingLine.TextTop - _textView.ViewportTop);
                }
            }

            _snapshotSpanToMargin = newSnapPointToMarginMap;
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

            RemoveGlyphByVisualSpan(changedSpan);
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
                        AddGlyph(textViewLine, mappingTagSpan.Tag, tagSpans[0]);
                    }
                }
            }
        }

        private void AddGlyph(ITextViewLine textViewLine, InheritanceMarginTag tag, SnapshotSpan span)
        {
            if (!_textView.TextViewLines.IntersectsBufferSpan(span))
            {
                return;
            }

            if (this.Visibility == Visibility.Collapsed)
            {
                var inheritanceMarginEnabled = _optionService.GetOption(FeatureOnOffOptions.ShowInheritanceMargin, _languageName);
                if (inheritanceMarginEnabled == true)
                {
                    this.Visibility = Visibility.Visible;
                }
            }

            var margin = new MarginGlyph.InheritanceMargin(
                _threadingContext,
                _streamingFindUsagesPresenter,
                _classificationTypeMap,
                _classificationFormatMap,
                _operationExecutor,
                tag,
                _textView);

            margin.Height = HeightAndWidthOfMargin;
            margin.Width = HeightAndWidthOfMargin;
            _snapshotSpanToMargin[span] = margin;
            Canvas.SetTop(margin, textViewLine.TextTop - _textView.ViewportTop);
            Canvas.SetLeft(margin, GetMarginOffset());
            MainCanvas.Children.Add(margin);

        }

        private void RemoveGlyphByVisualSpan(SnapshotSpan snapshotSpan)
        {
            var mariginsToRemove = _snapshotSpanToMargin
                .Where(kvp => snapshotSpan.IntersectsWith(kvp.Key))
                .ToImmutableArray();
            foreach (var (span, margin) in mariginsToRemove)
            {
                MainCanvas.Children.Remove(margin);
                _snapshotSpanToMargin.Remove(span);
            }

            if (MainCanvas.Children.Count == 0)
            {
                var inheritanceMarginEnabled = _optionService.GetOption(FeatureOnOffOptions.ShowInheritanceMargin, _languageName);
                if (inheritanceMarginEnabled == false)
                {
                    this.Visibility = Visibility.Collapsed;
                }
            }
        }

        #region IWpfTextViewMargin
        public FrameworkElement VisualElement => this;

        public double MarginSize => MainCanvas.Width;

        public bool Enabled => true;

        public ITextViewMargin? GetTextViewMargin(string marginName)
            => marginName == nameof(InheritanceMarginViewMargin) ? this : null;

        public void Dispose()
        {
            _tagAggregator.BatchedTagsChanged -= OnTagsChanged;
            _textView.LayoutChanged -= OnLayoutChanged;
            _textView.ZoomLevelChanged -= OnZoomLevelChanged;
            _textView.Options.OptionChanged -= OnTextViewOptionChanged;
            _optionService.OptionChanged -= OnRoslynOptionChanged;
            _tagAggregator.Dispose();
        }
        #endregion
    }
}
