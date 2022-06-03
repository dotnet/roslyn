// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    /// <summary>
    /// This is the tag which implements the IntraTextAdornmentTag and is meant to create the UIElements that get shown
    /// in the editor
    /// </summary>
    internal sealed class InlineHintsTag : IntraTextAdornmentTag
    {
        public const string TagId = "inline hints";

        private readonly ITextView _textView;
        private readonly SnapshotSpan _span;
        private readonly InlineHint _hint;
        private readonly InlineHintsTaggerProvider _taggerProvider;

        private InlineHintsTag(
            FrameworkElement adornment,
            ITextView textView,
            SnapshotSpan span,
            InlineHint hint,
            InlineHintsTaggerProvider taggerProvider)
            : base(adornment,
                   removalCallback: null,
                   PositionAffinity.Predecessor)
        {
            _textView = textView;
            _span = span;
            _hint = hint;
            _taggerProvider = taggerProvider;

            // Sets the tooltip to a string so that the tool tip opening event can be triggered
            // Tooltip value does not matter at this point because it immediately gets overwritten by the correct
            // information in the Border_ToolTipOpening event handler
            adornment.ToolTip = "Quick info";
            adornment.ToolTipOpening += Border_ToolTipOpening;

            if (_hint.ReplacementTextChange is not null)
            {
                adornment.MouseLeftButtonDown += Adornment_MouseLeftButtonDown;
            }
        }

        /// <summary>
        /// Creates the UIElement on call
        /// Uses PositionAffinity.Predecessor because we want the tag to be associated with the preceding character
        /// </summary>
        /// <param name="textView">The view of the editor</param>
        /// <param name="span">The span that has the location of the hint</param>
        public static InlineHintsTag Create(
            InlineHint hint,
            TextFormattingRunProperties format,
            IWpfTextView textView,
            SnapshotSpan span,
            InlineHintsTaggerProvider taggerProvider,
            IClassificationFormatMap formatMap,
            bool classify)
        {
            return new InlineHintsTag(
                CreateElement(hint.DisplayParts, textView, format, formatMap, taggerProvider.TypeMap, classify),
                textView, span, hint, taggerProvider);
        }

        public async Task<IReadOnlyCollection<object>> CreateDescriptionAsync(CancellationToken cancellationToken)
        {
            var document = _span.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var taggedText = await _hint.GetDescriptionAsync(document, cancellationToken).ConfigureAwait(false);
                if (!taggedText.IsDefaultOrEmpty)
                {
                    var context = new IntellisenseQuickInfoBuilderContext(
                        document,
                        _taggerProvider.GlobalOptions.GetClassificationOptions(document.Project.Language),
                        _taggerProvider.ThreadingContext,
                        _taggerProvider.OperationExecutor,
                        _taggerProvider.AsynchronousOperationListener,
                        _taggerProvider.StreamingFindUsagesPresenter);
                    return Implementation.IntelliSense.Helpers.BuildInteractiveTextElements(taggedText, context);
                }
            }

            return Array.Empty<object>();
        }

        private static FrameworkElement CreateElement(
            ImmutableArray<TaggedText> taggedTexts,
            IWpfTextView textView,
            TextFormattingRunProperties format,
            IClassificationFormatMap formatMap,
            ClassificationTypeMap typeMap,
            bool classify)
        {
            // Constructs the hint block which gets assigned parameter name and fontstyles according to the options
            // page. Calculates a inline tag that will be 3/4s the size of a normal line. This shrink size tends to work
            // well with VS at any zoom level or font size.
            var block = new TextBlock
            {
                FontFamily = format.Typeface.FontFamily,
                FontSize = 0.75 * format.FontRenderingEmSize,
                FontStyle = FontStyles.Normal,
                Foreground = format.ForegroundBrush,
                // Adds a little bit of padding to the left of the text relative to the border to make the text seem
                // more balanced in the border
                Padding = new Thickness(left: 2, top: 0, right: 2, bottom: 0)
            };

            var (trimmedTexts, leftPadding, rightPadding) = Trim(taggedTexts);

            foreach (var taggedText in trimmedTexts)
            {
                var run = new Run(taggedText.ToVisibleDisplayString(includeLeftToRightMarker: true));

                if (classify && taggedText.Tag != TextTags.Text)
                {
                    var properties = formatMap.GetTextProperties(typeMap.GetClassificationType(taggedText.Tag.ToClassificationTypeName()));
                    var brush = properties.ForegroundBrush.Clone();
                    run.Foreground = brush;
                }

                block.Inlines.Add(run);
            }

            block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            // Encapsulates the textblock within a border. Gets foreground/background colors from the options menu.
            // If the tag is started or followed by a space, we trim that off but represent the space as buffer on hte
            // left or right side.
            var left = leftPadding * 5;
            var right = rightPadding * 5;

            var border = new Border
            {
                Background = format.BackgroundBrush,
                Child = block,
                CornerRadius = new CornerRadius(2),
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(left, top: 0, right, bottom: 0),
            };

            // gets pixel distance of baseline to top of the font height
            var dockPanelHeight = format.Typeface.FontFamily.Baseline * format.FontRenderingEmSize;
            var dockPanel = new DockPanel
            {
                Height = dockPanelHeight,
                LastChildFill = false,
                // VerticalAlignment is set to Top because it will rest to the top relative to the stackpanel
                VerticalAlignment = VerticalAlignment.Top
            };

            dockPanel.Children.Add(border);
            DockPanel.SetDock(border, Dock.Bottom);

            var stackPanel = new StackPanel
            {
                // Height set to align the baseline of the text within the TextBlock with the baseline of text in the editor
                Height = dockPanelHeight + (block.DesiredSize.Height - (block.FontFamily.Baseline * block.FontSize)),
                Orientation = Orientation.Vertical
            };

            stackPanel.Children.Add(dockPanel);
            // Need to set these properties to avoid unnecessary reformatting because some dependancy properties
            // affect layout
            TextOptions.SetTextFormattingMode(stackPanel, TextOptions.GetTextFormattingMode(textView.VisualElement));
            TextOptions.SetTextHintingMode(stackPanel, TextOptions.GetTextHintingMode(textView.VisualElement));
            TextOptions.SetTextRenderingMode(stackPanel, TextOptions.GetTextRenderingMode(textView.VisualElement));

            return stackPanel;
        }

        private static (ImmutableArray<TaggedText> texts, int leftPadding, int rightPadding) Trim(ImmutableArray<TaggedText> taggedTexts)
        {
            using var _ = ArrayBuilder<TaggedText>.GetInstance(out var result);
            var leftPadding = 0;
            var rightPadding = 0;

            if (taggedTexts.Length == 1)
            {
                var first = taggedTexts.First();

                var trimStart = first.Text.TrimStart();
                var trimBoth = trimStart.TrimEnd();
                result.Add(new TaggedText(first.Tag, trimBoth));
                leftPadding = first.Text.Length - trimStart.Length;
                rightPadding = trimStart.Length - trimBoth.Length;
            }
            else if (taggedTexts.Length >= 2)
            {
                var first = taggedTexts.First();
                var trimStart = first.Text.TrimStart();
                result.Add(new TaggedText(first.Tag, trimStart));
                leftPadding = first.Text.Length - trimStart.Length;

                for (var i = 1; i < taggedTexts.Length - 1; i++)
                    result.Add(taggedTexts[i]);

                var last = taggedTexts.Last();
                var trimEnd = last.Text.TrimEnd();
                result.Add(new TaggedText(last.Tag, trimEnd));
                rightPadding = last.Text.Length - trimEnd.Length;
            }

            return (result.ToImmutable(), leftPadding, rightPadding);
        }

        /// <summary>
        /// Determines if the border is being moused over and shows the info accordingly
        /// </summary>
        private void Border_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            var hintUIElement = (FrameworkElement)sender;
            e.Handled = true;

            bool KeepOpen()
            {
                var mousePoint = Mouse.GetPosition(hintUIElement);
                return !(mousePoint.X > hintUIElement.ActualWidth || mousePoint.X < 0 || mousePoint.Y > hintUIElement.ActualHeight || mousePoint.Y < 0);
            }

            var toolTipPresenter = _taggerProvider.ToolTipService.CreatePresenter(_textView, new ToolTipParameters(trackMouse: true, ignoreBufferChange: false, KeepOpen));
            _ = StartToolTipServiceAsync(toolTipPresenter);
        }

        /// <summary>
        /// Waits for the description to be created and updates the tooltip with the associated information
        /// </summary>
        private async Task StartToolTipServiceAsync(IToolTipPresenter toolTipPresenter)
        {
            var threadingContext = _taggerProvider.ThreadingContext;
            var uiList = await Task.Run(() => CreateDescriptionAsync(threadingContext.DisposalToken)).ConfigureAwait(false);
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(threadingContext.DisposalToken);

            toolTipPresenter.StartOrUpdate(_textView.TextSnapshot.CreateTrackingSpan(_span.Start, _span.Length, SpanTrackingMode.EdgeInclusive), uiList);
        }

        private void Adornment_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                e.Handled = true;
                var textChange = _hint.ReplacementTextChange!.Value;

                var snapshot = _span.Snapshot;
                var subjectBuffer = snapshot.TextBuffer;

                // Selected SpanTrackingMode to be EdgeExclusive by default.
                // Will revise if there are some scenarios we did not think of that produce undesirable behavior.
                subjectBuffer.Replace(
                    textChange.Span.ToSnapshotSpan(snapshot).TranslateTo(subjectBuffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive),
                    textChange.NewText);
            }
        }
    }
}
