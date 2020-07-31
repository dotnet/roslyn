// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.DocumentationComments;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Host;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Windows.Input;

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
        private SnapshotSpan _span;
        private readonly SymbolKey _key;
        private readonly IThreadingContext _threadingContext;
        public Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;

        /// <summary>
        /// Creates the UIElement on call
        /// Uses PositionAffinity.Successor because we want the tag to be associated with the following character
        /// </summary>
        /// <param name="adornment">The adornment that we are creating</param>
        /// <param name="toolTipService">The service being used to create the tooltip and match the UI</param>
        /// <param name="textView">The view of the editor</param>
        /// <param name="span">The span that has the location of the hint</param>
        /// <param name="key">The symbolkey associated with each parameter</param>
        private InlineParameterNameHintsTag(FrameworkElement adornment,
                                           IToolTipService toolTipService, ITextView textView,
                                           SnapshotSpan span, SymbolKey key, IThreadingContext threadingContext,
                                           Lazy<IStreamingFindUsagesPresenter> streamingPresenter)
            : base(adornment, removalCallback: null, PositionAffinity.Successor)
        {
            _streamingPresenter = streamingPresenter;
            _textView = textView;
            _key = key;
            _toolTipService = toolTipService;
            _span = span;
            _threadingContext = threadingContext;
            adornment.ToolTip = "Test";
            adornment.ToolTipOpening += Border_ToolTipOpening;
        }

        public static InlineParameterNameHintsTag Create(string text, double lineHeight, TextFormattingRunProperties format, IToolTipService toolTipService, ITextView textView,
                                          SnapshotSpan span, SymbolKey key, IThreadingContext threadingContext, Lazy<IStreamingFindUsagesPresenter> streamingPresenter)
        {
            return new InlineParameterNameHintsTag(CreateElement(text, lineHeight, format), toolTipService, textView, span, key, threadingContext, streamingPresenter);
        }

        public async Task<IReadOnlyCollection<object>> CreateDescriptionAsync()
        {
            var cancellationToken = new CancellationToken();
            var document = _textView.TextBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var compilation = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var symbol = _key.Resolve(compilation).Symbol;
            var workspace = document.Project.Solution.Workspace;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbolDisplayService = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<ISymbolDisplayService>();
            var formatter = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<IDocumentationCommentFormattingService>();
            var sections = await symbolDisplayService.ToDescriptionGroupsAsync(workspace, semanticModel, _span.Start, ImmutableArray.Create(symbol), cancellationToken).ConfigureAwait(false);

            var textContentBuilder = new List<TaggedText>();
            textContentBuilder.AddRange(sections[SymbolDescriptionGroups.MainDescription]);
            AddDocumentationPart(textContentBuilder, symbol, semanticModel, _span.Start, formatter, cancellationToken);

            if (sections.TryGetValue(SymbolDescriptionGroups.AnonymousTypes, out var parts))
            {
                if (!parts.IsDefaultOrEmpty)
                {
                    textContentBuilder.AddLineBreak();
                    textContentBuilder.AddLineBreak();
                    textContentBuilder.AddRange(parts);
                }
            }

            var uiCollection = Implementation.IntelliSense.Helpers.BuildInteractiveTextElements(textContentBuilder.ToImmutableArray<TaggedText>(), document, _threadingContext, _streamingPresenter);
            return uiCollection;
        }

        private static void AddDocumentationPart(
            List<TaggedText> textContentBuilder, ISymbol symbol, SemanticModel semanticModel, int position, IDocumentationCommentFormattingService formatter, CancellationToken cancellationToken)
        {
            var documentation = symbol.GetDocumentationParts(semanticModel, position, formatter, cancellationToken);

            if (documentation.Any())
            {
                textContentBuilder.AddLineBreak();
                textContentBuilder.AddRange(documentation);
            }
        }

        private static FrameworkElement CreateElement(string text, double lineHeight, TextFormattingRunProperties format)
        {
            // Constructs the hint block which gets assigned parameter name and fontstyles according to the options
            // page. Calculates a font size 1/4 smaller than the font size of the rest of the editor
            var block = new TextBlock
            {
                FontFamily = format.Typeface.FontFamily,
                FontSize = format.FontRenderingEmSize - (0.25 * format.FontRenderingEmSize),
                FontStyle = FontStyles.Normal,
                Foreground = format.ForegroundBrush,
                Padding = new Thickness(0),
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
                Height = lineHeight - (0.25 * lineHeight),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, -0.20 * lineHeight, 5, 0),
                Padding = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
            };
            border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return border;
        }

        /// <summary>
        /// Determines if the border is being moused over and shows the info accordingly
        /// </summary>
        private void Border_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            var border = sender as Border;
            var uiList = CreateDescriptionAsync().Result;

            bool KeepOpen()
            {
                var mousePoint = Mouse.GetPosition(border);
                if (mousePoint.X > border.ActualWidth || mousePoint.X < 0 || mousePoint.Y > border.ActualHeight || mousePoint.Y < 0)
                {
                    return false;
                }
                return true;
            }

            _toolTipService.CreatePresenter(_textView, new ToolTipParameters(true, false, KeepOpen)).StartOrUpdate(_textView.TextSnapshot.CreateTrackingSpan(_span.Start, _span.Length, SpanTrackingMode.EdgeInclusive), uiList);
            e.Handled = true;
        }
    }
}
