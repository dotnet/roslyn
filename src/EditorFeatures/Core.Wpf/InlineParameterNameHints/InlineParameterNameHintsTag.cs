// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
{
    /// <summary>
    /// This is the tag which implements the IntraTextAdornmentTag and is meant to create the UIElements that get shown
    /// in the editor
    /// </summary>
    internal class InlineParameterNameHintsTag : IntraTextAdornmentTag
    {
        public const string TagId = "inline parameter name hints";
        private readonly IToolTipService _toolTipService;
        private readonly ITextView _textView;
        private readonly SnapshotSpan _span;
        private readonly SymbolKey _key;
        private readonly IThreadingContext _threadingContext;
        private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;

        private InlineParameterNameHintsTag(FrameworkElement adornment, ITextView textView, SnapshotSpan span,
                                            SymbolKey key, InlineParameterNameHintsTaggerProvider taggerProvider)
            : base(adornment, removalCallback: null, PositionAffinity.Predecessor)
        {
            _textView = textView;
            _span = span;
            _key = key;
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
        /// <param name="key">The symbolkey associated with each parameter</param>
        public static InlineParameterNameHintsTag Create(string text, TextFormattingRunProperties format,
                                                         IWpfTextView textView, SnapshotSpan span, SymbolKey key,
                                                         InlineParameterNameHintsTaggerProvider taggerProvider)
        {
            return new InlineParameterNameHintsTag(CreateElement(text, textView, format), textView,
                                                   span, key, taggerProvider);
        }

        public async Task<IReadOnlyCollection<object>> CreateDescriptionAsync(CancellationToken cancellationToken)
        {
            var document = _textView.TextBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var textContentBuilder = new List<TaggedText>();

            if (document != null)
            {
                var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var symbol = _key.Resolve(compilation, cancellationToken: cancellationToken).Symbol;

                if (symbol != null)
                {
                    var workspace = document.Project.Solution.Workspace;
                    var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var symbolDisplayService = document.Project.LanguageServices.GetRequiredService<ISymbolDisplayService>();
                    var formatter = document.Project.LanguageServices.GetService<IDocumentationCommentFormattingService>();
                    var sections = await symbolDisplayService.ToDescriptionGroupsAsync(workspace, semanticModel, _span.Start, ImmutableArray.Create(symbol), cancellationToken).ConfigureAwait(false);
                    textContentBuilder.AddRange(sections[SymbolDescriptionGroups.MainDescription]);
                    if (formatter != null)
                    {
                        var documentation = symbol.GetDocumentationParts(semanticModel, _span.Start, formatter, cancellationToken);

                        if (documentation.Any())
                        {
                            textContentBuilder.AddLineBreak();
                            textContentBuilder.AddRange(documentation);
                        }
                    }

                    if (sections.TryGetValue(SymbolDescriptionGroups.AnonymousTypes, out var parts))
                    {
                        if (!parts.IsDefaultOrEmpty)
                        {
                            textContentBuilder.AddLineBreak();
                            textContentBuilder.AddLineBreak();
                            textContentBuilder.AddRange(parts);
                        }
                    }
                }

                var uiCollection = Implementation.IntelliSense.Helpers.BuildInteractiveTextElements(textContentBuilder.ToImmutableArray<TaggedText>(),
                    document, _threadingContext, _streamingPresenter);
                return uiCollection;
            }

            return Array.Empty<object>();
        }

        private static FrameworkElement CreateElement(string text, IWpfTextView textView, TextFormattingRunProperties format)
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
                Padding = new Thickness(left: 1, top: 0, right: 0, bottom: 0),
                Text = text + ":",
                VerticalAlignment = VerticalAlignment.Center,
            };

            // Encapsulates the textblock within a border. Sets the height of the border to be 3/4 of the original 
            // height. Gets foreground/background colors from the options menu. The margin is the distance from the 
            // adornment to the text and pushing the adornment upwards to create a separation when on a specific line
            var border = new Border
            {
                Background = format.BackgroundBrush,
                Child = block,
                CornerRadius = new CornerRadius(2),
                Height = textView.LineHeight - (0.25 * textView.LineHeight),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(left: 0, top: -0.20 * textView.LineHeight, right: 5, bottom: 0),
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
