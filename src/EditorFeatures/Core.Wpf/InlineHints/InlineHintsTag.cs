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
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
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
    internal class InlineHintsTag : IntraTextAdornmentTag
    {
        public const string TagId = "inline hints";

        private readonly IToolTipService _toolTipService;
        private readonly ITextView _textView;
        private readonly SnapshotSpan _span;
        private readonly InlineHint _hint;
        private readonly IThreadingContext _threadingContext;
        private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;

        private InlineHintsTag(
            FrameworkElement adornment,
            ITextView textView,
            SnapshotSpan span,
            InlineHint hint,
            InlineHintsTaggerProvider taggerProvider)
            : base(adornment, removalCallback: null, PositionAffinity.Predecessor)
        {
            _textView = textView;
            _span = span;
            _hint = hint;
            _streamingPresenter = taggerProvider.StreamingFindUsagesPresenter;
            _threadingContext = taggerProvider.ThreadingContext;
            _toolTipService = taggerProvider.ToolTipService;

            // Sets the tooltip to a string so that the tool tip opening event can be triggered
            // Tooltip value does not matter at this point because it immediately gets overwritten by the correct
            // information in the Border_ToolTipOpening event handler
            adornment.ToolTip = "Quick info";
            adornment.ToolTipOpening += Border_ToolTipOpening;
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
                CreateElement(hint.DisplayParts, textView, span, format, formatMap, taggerProvider.TypeMap, classify),
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
                    return Implementation.IntelliSense.Helpers.BuildInteractiveTextElements(
                        taggedText, document, _threadingContext, _streamingPresenter);
                }
            }

            return Array.Empty<object>();
        }

        private static FrameworkElement CreateElement(
            ImmutableArray<TaggedText> taggedTexts,
            IWpfTextView textView,
            SnapshotSpan span,
            TextFormattingRunProperties format,
            IClassificationFormatMap formatMap,
            ClassificationTypeMap typeMap,
            bool classify)
        {
            // Constructs the hint block which gets assigned parameter name and fontstyles according to the options
            // page. Calculates a font size 1/4 smaller than the font size of the rest of the editor
            var block = new TextBlock
            {
                FontFamily = format.Typeface.FontFamily,
                FontSize = format.FontRenderingEmSize - (0.25 * format.FontRenderingEmSize),
                FontStyle = FontStyles.Normal,
                Foreground = format.ForegroundBrush,

                // Adds a little bit of padding to the left of the text relative to the border
                // to make the text seem more balanced in the border
                Padding = new Thickness(left: 1, top: 0, right: 1, bottom: 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            foreach (var taggedText in taggedTexts)
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

            // Encapsulates the textblock within a border. Sets the height of the border to be 3/4 of the original 
            // height. Gets foreground/background colors from the options menu. The margin is the distance from the 
            // adornment to the text and pushing the adornment upwards to create a separation when on a specific line

            // If the tag is followed by a space, just create a normal border (as there will already be a buffer to the right).
            // If not, then pad the right a little so the tag doesn't feel too cramped with the following text.
            var right = span.End < span.Snapshot.Length && char.IsWhiteSpace(span.End.GetChar()) ? 0 : 5;

            var border = new Border
            {
                Background = format.BackgroundBrush,
                Child = block,
                CornerRadius = new CornerRadius(2),
                Height = textView.LineHeight - (0.25 * textView.LineHeight),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(left: 0, top: -0.20 * textView.LineHeight, right, bottom: 0),
                Padding = new Thickness(1),

                // Need to set SnapsToDevicePixels and UseLayoutRounding to avoid unnecessary reformatting
                SnapsToDevicePixels = textView.VisualElement.SnapsToDevicePixels,
                UseLayoutRounding = textView.VisualElement.UseLayoutRounding,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Need to set these properties to avoid unnecessary reformatting because some dependancy properties
            // affect layout
            TextOptions.SetTextFormattingMode(border, TextOptions.GetTextFormattingMode(textView.VisualElement));
            TextOptions.SetTextHintingMode(border, TextOptions.GetTextHintingMode(textView.VisualElement));
            TextOptions.SetTextRenderingMode(border, TextOptions.GetTextRenderingMode(textView.VisualElement));

            border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return border;
        }

        /// <summary>
        /// Determines if the border is being moused over and shows the info accordingly
        /// </summary>
        private void Border_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            var border = (Border)sender;
            e.Handled = true;

            bool KeepOpen()
            {
                var mousePoint = Mouse.GetPosition(border);
                return !(mousePoint.X > border.ActualWidth || mousePoint.X < 0 || mousePoint.Y > border.ActualHeight || mousePoint.Y < 0);
            }

            var toolTipPresenter = _toolTipService.CreatePresenter(_textView, new ToolTipParameters(trackMouse: true, ignoreBufferChange: false, KeepOpen));
            _ = StartToolTipServiceAsync(toolTipPresenter);
        }

        /// <summary>
        /// Waits for the description to be created and updates the tooltip with the associated information
        /// </summary>
        private async Task StartToolTipServiceAsync(IToolTipPresenter toolTipPresenter)
        {
            var uiList = await Task.Run(() => CreateDescriptionAsync(_threadingContext.DisposalToken)).ConfigureAwait(false);
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(_threadingContext.DisposalToken);

            toolTipPresenter.StartOrUpdate(_textView.TextSnapshot.CreateTrackingSpan(_span.Start, _span.Length, SpanTrackingMode.EdgeInclusive), uiList);
        }
    }
}
