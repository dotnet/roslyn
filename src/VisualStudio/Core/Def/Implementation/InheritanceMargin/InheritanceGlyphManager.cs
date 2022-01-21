// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin
{
    /// <summary>
    /// Manager controls all the glyphs of Inheritance Margin in <see cref="InheritanceMarginViewMargin"/>.
    /// </summary>
    internal partial class InheritanceGlyphManager : ForegroundThreadAffinitizedObject, IDisposable
    {
        // We want to our glyphs to have the same background color as the glyphs in GlyphMargin.
        private const string GlyphMarginName = "Indicator Margin";

        private readonly double _heightAndWidthOfTheGlyph;
        private readonly IWpfTextView _textView;
        private readonly IThreadingContext _threadingContext;
        private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly IUIThreadOperationExecutor _operationExecutor;
        private readonly IEditorFormatMap _editorFormatMap;
        private readonly IAsynchronousOperationListener _listener;
        private readonly Canvas _glyphsContainer;
        private readonly SimpleIntervalTree<GlyphData, GlyphDataIntrospector> _glyphDataTree;

        public InheritanceGlyphManager(
            IWpfTextView textView,
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            IClassificationFormatMap classificationFormatMap,
            ClassificationTypeMap classificationTypeMap,
            IUIThreadOperationExecutor operationExecutor,
            IEditorFormatMap editorFormatMap,
            IAsynchronousOperationListener listener,
            Canvas canvas,
            double heightAndWidthOfTheGlyph) : base(threadingContext)
        {
            _textView = textView;
            _threadingContext = threadingContext;
            _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
            _classificationTypeMap = classificationTypeMap;
            _classificationFormatMap = classificationFormatMap;
            _operationExecutor = operationExecutor;
            _editorFormatMap = editorFormatMap;
            _glyphsContainer = canvas;
            _listener = listener;
            _heightAndWidthOfTheGlyph = heightAndWidthOfTheGlyph;
            _editorFormatMap.FormatMappingChanged += FormatMappingChanged;

            _glyphDataTree = new SimpleIntervalTree<GlyphData, GlyphDataIntrospector>(new GlyphDataIntrospector(), values: null);
            UpdateBackgroundColor();
        }

        void IDisposable.Dispose()
        {
            _editorFormatMap.FormatMappingChanged -= FormatMappingChanged;
        }

        /// <summary>
        /// Generate the glyph by the given <paramref name="tag"/>, and add it to the margin.
        /// It should only be called by UI thread because UI elements are manipulated by this method.
        /// </summary>
        public void AddGlyph(InheritanceMarginTag tag, SnapshotSpan span)
        {
            AssertIsForeground();
            var lines = _textView.TextViewLines;
            if (lines.IntersectsBufferSpan(span) && GetStartingLine(lines, span) is IWpfTextViewLine line)
            {
                var glyph = CreateNewGlyph(tag);
                SetTop(line, glyph);
                _glyphDataTree.AddIntervalInPlace(new GlyphData(span, glyph));
                _glyphsContainer.Children.Add(glyph);
            }
        }

        /// <summary>
        /// Remove the glyphs covered by <paramref name="snapshotSpan"/>.
        /// It should only be called by UI thread because UI elements are manipulated by this method.
        /// </summary>
        public void RemoveGlyphs(SnapshotSpan snapshotSpan)
        {
            AssertIsForeground();
            var glyphDataToRemove = _glyphDataTree.GetIntervalsThatIntersectWith(snapshotSpan.Start, snapshotSpan.Length);
            foreach (var (_, glyph) in glyphDataToRemove)
            {
                _glyphsContainer.Children.Remove(glyph);
            }

            var remainingGlyphData = _glyphDataTree.Except(glyphDataToRemove).ToImmutableArray();
            _glyphDataTree.ClearInPlace();
            foreach (var glyphData in remainingGlyphData)
            {
                _glyphDataTree.AddIntervalInPlace(glyphData);
            }
        }

        /// <summary>
        /// Remove the glyphs that are no long visible or covered by the <paramref name="newOrReformattedLines"/>.
        /// Refresh all the other the existing glyphs with the <paramref name="snapshot"/>.
        /// It should only be called by UI thread because UI elements are manipulated by this method.
        /// </summary>
        public void SetSnapshotAndUpdate(ITextSnapshot snapshot, IList<ITextViewLine> newOrReformattedLines, IList<ITextViewLine> translatedLines)
        {
            AssertIsForeground();
            if (!_glyphDataTree.IsEmpty())
            {
                // Go through all the existing visuals and invalidate or transform as appropriate.
                var allGlyphData = _glyphDataTree.ToImmutableArray();
                _glyphDataTree.ClearInPlace();
                foreach (var (span, glyph) in allGlyphData)
                {
                    var newSpan = span.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive);
                    if (!_textView.TextViewLines.IntersectsBufferSpan(newSpan) || GetStartingLine(newOrReformattedLines, newSpan) != null)
                    {
                        //Either visual is no longer visible or it crosses a line
                        //that was reformatted.
                        _glyphsContainer.Children.Remove(glyph);
                    }
                    else
                    {
                        _glyphDataTree.AddIntervalInPlace(new GlyphData(newSpan, glyph));
                        var line = GetStartingLine(translatedLines, newSpan);
                        if (line != null)
                        {
                            SetTop(line, glyph);
                        }
                    }
                }
            }
        }

        private void SetTop(ITextViewLine line, InheritanceMarginGlyph glyph)
            => Canvas.SetTop(glyph, line.TextTop - _textView.ViewportTop);

        private static ITextViewLine? GetStartingLine(IList<ITextViewLine> lines, Span span)
        {
            if (lines.Count > 0)
            {
                var index = lines.ToImmutableArray().BinarySearch(span.Start, CompareWithLineStartAndEnd);
                if (index >= 0)
                {
                    return lines[index];
                }

                var lastLine = lines[^1];
                if (lastLine.EndIncludingLineBreak == lastLine.Snapshot.Length && span.Start == lastLine.EndIncludingLineBreak)
                {
                    // As a special case, if the last line ends at the end of the buffer and the span starts at the end of the buffer
                    // as well, treat is as crossing the last line in the buffer.
                    return lastLine;
                }
            }

            return null;
        }

        private static int CompareWithLineStartAndEnd(ITextViewLine line, int value)
        {
            if (value < line.Start)
            {
                return 1;
            }

            // EndIncludingLineBreak usually equals the start of next line (the exclusion is if this is the last line, which will be handled separately),
            // and we always prefer to use the line start, so still return -1 when value == line.EndIncludingLineBreak.
            if (value >= line.EndIncludingLineBreak)
            {
                return -1;
            }

            return 0;
        }

        private InheritanceMarginGlyph CreateNewGlyph(InheritanceMarginTag tag)
            => new(
                _threadingContext,
                _streamingFindUsagesPresenter,
                _classificationTypeMap,
                _classificationFormatMap,
                _operationExecutor,
                tag,
                _textView,
                _listener)
            { Height = _heightAndWidthOfTheGlyph, Width = _heightAndWidthOfTheGlyph };

        private void FormatMappingChanged(object sender, FormatItemsEventArgs e)
            => UpdateBackgroundColor();

        private void UpdateBackgroundColor()
        {
            AssertIsForeground();
            var resourceDictionary = _editorFormatMap.GetProperties(GlyphMarginName);
            if (resourceDictionary.Contains(EditorFormatDefinition.BackgroundColorId))
            {
                var backgroundColor = (Color)resourceDictionary[EditorFormatDefinition.BackgroundColorId];
                // Set background color for all the glyphs
                ImageThemingUtilities.SetImageBackgroundColor(_glyphsContainer, backgroundColor);
            }
        }
    }
}
