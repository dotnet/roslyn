// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.CorDebugInterop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin
{
    /// <summary>
    /// Interaction logic for InheritanceMarginViewMargin.xaml
    /// </summary>
    internal partial class InheritanceMarginViewMargin : UserControl, IWpfTextViewMargin
    {
        private readonly IWpfTextViewHost _textViewHost;
        private readonly IWpfTextView _textView;
        private readonly ITagAggregator<InheritanceMarginTag> _tagAggregator;
        private readonly Dictionary<int, MarginGlyph.InheritanceMargin> _lineNumberToMargin;

        private readonly IThreadingContext _threadingContext;
        private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly IUIThreadOperationExecutor _operationExecutor;

        public InheritanceMarginViewMargin(
            IWpfTextViewHost textViewHost,
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            IUIThreadOperationExecutor operationExecutor,
            IClassificationFormatMap classificationFormatMap,
            ClassificationTypeMap classificationTypeMap,
            ITagAggregator<InheritanceMarginTag> tagAggregator)
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
            _tagAggregator.BatchedTagsChanged += OnTagsChanged;
            _lineNumberToMargin = new Dictionary<int, MarginGlyph.InheritanceMargin>();
            MainCanvas.Width = MarginSize;
        }

        private void OnTagsChanged(object sender, BatchedTagsChangedEventArgs e)
        {
            if (_textView.IsClosed)
            {
                return;
            }

            var textSnapshot = _textView.TextSnapshot;
            var changedSnapshotSpans = e.Spans.SelectMany(span => span.GetSpans(textSnapshot)).ToImmutableArray();
            if (changedSnapshotSpans.Length == 0)
            {
                return;
            }

            var startOfChangedSpans = changedSnapshotSpans.Min(span => span.Start);
            var endOfChangedSpan = changedSnapshotSpans.Max(span => span.End);

            foreach (var line in _textView.TextViewLines.GetTextViewLinesIntersectingSpan(new SnapshotSpan(startOfChangedSpans, endOfChangedSpan)))
            {
                RemoveGlyphForLine(line);
                AddGlyphForLine(line);
            }
        }

        private void AddGlyphForLine(ITextViewLine line)
        {
            if (!line.IsValid)
            {
                return;
            }

            // Tagger of Inheritance always tags the start of the line;
            var tags = _tagAggregator.GetTags(new SnapshotSpan(line.Start, 0)).ToImmutableArray();
            if (tags.Length == 0)
            {
                return;
            }

            foreach (var tag in tags)
            {
                var margin = new MarginGlyph.InheritanceMargin(
                    _threadingContext,
                    _streamingFindUsagesPresenter,
                    _classificationTypeMap,
                    _classificationFormatMap,
                    _operationExecutor,
                    tag.Tag,
                    _textView);
                var lineNumber = GetLineNumber(line);
                _lineNumberToMargin[lineNumber] = margin;
                Canvas.SetTop(margin, line.TextTop - _textView.ViewportTop);
                MainCanvas.Children.Add(margin);
            }
        }

        private void RemoveGlyphForLine(ITextViewLine line)
        {
            if (!line.IsValid)
            {
                return;
            }

            var lineNumber = GetLineNumber(line);
            if (_lineNumberToMargin.TryGetValue(lineNumber, out var margin))
            {
                MainCanvas.Children.Remove(margin);
                _lineNumberToMargin.Remove(lineNumber);
            }
        }

        private int GetLineNumber(ITextViewLine line)
        {
            var linesCollection = _textView.TextViewLines;
            return linesCollection.GetIndexOfTextLine(line);
        }

        public FrameworkElement VisualElement => this;

        public double MarginSize => 18;

        public bool Enabled => true;

        public ITextViewMargin? GetTextViewMargin(string marginName)
            => marginName == nameof(InheritanceMarginViewMargin) ? this : null;

        public void Dispose()
        {
            _tagAggregator.BatchedTagsChanged -= OnTagsChanged;
            _tagAggregator.Dispose();
        }
    }
}
