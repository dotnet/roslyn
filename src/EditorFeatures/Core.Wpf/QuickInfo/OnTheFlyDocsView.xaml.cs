// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    /// <summary>
    /// Interaction logic for OnTheFlyDocsView.xaml.
    /// </summary>
    internal partial class OnTheFlyDocsView : UserControl, INotifyPropertyChanged
    {
        private readonly ITextView _textView;
        private readonly IViewElementFactoryService _viewElementFactoryService;
        private readonly ContentControl _responseControl = new ContentControl();
        private State _currentState = State.OnDemandLink;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Event that fires when the user requests results.
        /// </summary>
        public event EventHandler ResultsRequested;

#pragma warning disable CA1822 // Mark members as static
        public string OnTheFlyDocumentation => EditorFeaturesResources.On_the_fly_documentation;
#pragma warning restore CA1822 // Mark members as static

        public OnTheFlyDocsView(ITextView textView, IViewElementFactoryService viewElementFactoryService, Document document, int position, string descriptionText, CancellationToken cancellationToken)
        {
            _textView = textView;
            _viewElementFactoryService = viewElementFactoryService;
            var sparkle = new ImageElement(new VisualStudio.Core.Imaging.ImageId(CopilotConstants.CopilotIconMonikerGuid, CopilotConstants.CopilotIconSparkleId));

            // Even though this control is obviously WPF-specific, we use the
            // cross-platform UI controls to get theming for free.
            OnDemandLinkContent = ToUIElement(
                new ContainerElement(
                    ContainerElementStyle.Wrapped,
                    new object[]
                    {
                        sparkle,
                        ClassifiedTextElement.CreateHyperlink(EditorFeaturesResources.Tell_me_more, EditorFeaturesResources.Show_an_AI_generated_summary_of_this_code, () =>
                        RequestResults()),
                    }));

            LoadingContent = ToUIElement(
                new ContainerElement(
                    ContainerElementStyle.Stacked,
                    new object[]
                    {
                        new ClassifiedTextElement(new ClassifiedTextRun(
                            ClassificationTypeDefinitions.ReducedEmphasisText, EditorFeaturesResources.GitHub_Copilot_thinking)),
                        new SmoothProgressBar { IsIndeterminate = true, Height = 2, Margin = new Thickness { Top = 2 } },
                    }));

            // Ensure the loading content stretches so that the progress bar
            // takes the entire width of the quick info tooltip.
            if (LoadingContent is FrameworkElement element)
            {
                element.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            ResultsContent = ToUIElement(
                new ContainerElement(
                    ContainerElementStyle.Stacked,
                    new object[]
                    {
                        new ContainerElement(
                            ContainerElementStyle.Wrapped,
                            new object[]
                            {
                                sparkle,

                                // TODO: Ask Rohan if we can drop the header text and put the actual
                                // results (this.responseControl) directly after the sparkle. I (bemcmorr)
                                // think the header gets pretty noisy visually.
                                ClassifiedTextElement.CreatePlainText(EditorFeaturesResources.GitHub_Copilot),
                            }),
                        new ThematicBreakElement(),
                        _responseControl,
                        new ThematicBreakElement(),
                        new ClassifiedTextElement(new ClassifiedTextRun(
                            ClassificationTypeDefinitions.ReducedEmphasisText, EditorFeaturesResources.AI_generated_content_may_be_inaccurate)),
                    }));
            this.ResultsRequested += (_, _) => PopulateAIDocumentationElements(document, position, descriptionText, cancellationToken);
            InitializeComponent();
        }

        private async void PopulateAIDocumentationElements(Document document, int position, string descriptionText, CancellationToken cancellationToken)
        {
            var copilotService = document.GetRequiredLanguageService<ICopilotCodeAnalysisService>();
            var symbolService = document.GetRequiredLanguageService<IGoToDefinitionSymbolService>();
            var (symbol, _, span) = await symbolService.GetSymbolProjectAndBoundSpanAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            var symbolText = symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntaxAsync(cancellationToken).Result.ToString();
            if (symbol is null || symbolText is null)
            {
                SetResultText(EditorFeaturesResources.An_error_occurred_while_generating_documentation_for_this_code);
                CurrentState = State.Finished;
                return;
            }

            var response = await copilotService.GetOnTheFlyDocsAsync(descriptionText, symbolText, cancellationToken).ConfigureAwait(false);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            if (response is null || response.Length == 0)
            {
                SetResultText(EditorFeaturesResources.An_error_occurred_while_generating_documentation_for_this_code);
                CurrentState = State.Finished;
            }
            else
            {
                SetResultText(response);
                CurrentState = State.Finished;
            }
        }

        /// <summary>
        /// Gets or sets the current state of the view.
        /// </summary>
        public State CurrentState
        {
            get => _currentState;
            set => OnPropertyChanged(ref _currentState, value);
        }

        /// <summary>
        /// Gets the content of the on-demand link state of the view.
        /// </summary>
        public UIElement OnDemandLinkContent { get; }

        /// <summary>
        /// Gets the content of the loading state of the view.
        /// </summary>
        public UIElement LoadingContent { get; }

        /// <summary>
        /// Gets the content of the results state of the view.
        /// </summary>
        public UIElement ResultsContent { get; }

        public void RequestResults()
        {
            CurrentState = State.Loading;
            ResultsRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Sets the text of the AI-generated response.
        /// </summary>
        /// <param name="text">Response text to display.</param>
        public void SetResultText(string text)
        {
            List<ClassifiedTextRun> runs = new();
            new ClassifiedRunsTooltipWrapper(runs).WrapAndTruncate(text);
            _responseControl.Content = ToUIElement(
                new ContainerElement(ContainerElementStyle.Wrapped, new ClassifiedTextElement(runs)));
        }

        private void OnPropertyChanged<T>(ref T member, T value, [CallerMemberName] string? name = null)
        {
            member = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private UIElement ToUIElement(object model)
            => _viewElementFactoryService.CreateViewElement<UIElement>(_textView, model);
    }

    /// <summary>
    /// Represents the potential states of the view.
    /// </summary>
    public enum State
    {
        /// <summary>
        /// The view is displaying the on-demand hyperlink.
        /// </summary>
        OnDemandLink,

        /// <summary>
        /// The view is in the loading state.
        /// </summary>
        Loading,

        /// <summary>
        /// The view is displaying computed results.
        /// </summary>
        Finished,
    }

    /// <summary>
    /// Tooltip wrapper for classified runs.
    /// </summary>
    internal class ClassifiedRunsTooltipWrapper : TooltipWrapper<IList<ClassifiedTextRun>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClassifiedRunsTooltipWrapper"/> class.
        /// </summary>
        /// <param name="target">Target <see cref="IList{ClassifiedTextRun}"/> instance.</param>
        /// <param name="lineIndentSize">Set the indent size of each line on the target.</param>
        public ClassifiedRunsTooltipWrapper(IList<ClassifiedTextRun> target, uint? lineIndentSize = 0)
            : base(target, lineIndentSize)
        {
        }

        /// <inheritdoc/>
        protected override bool IsLastLineEmpty
        {
            get
            {
                return Target[Target.Count - 1].Text == Environment.NewLine;
            }
        }

        /// <inheritdoc/>
        protected override void AddEllipsis()
        {
            Target.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Other, "…", ClassifiedTextRunStyle.Plain));
        }

        /// <inheritdoc/>
        protected override void AddLine(string line)
        {
            Target.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Other, line.ToString()));
        }

        /// <inheritdoc/>
        protected override void AddNewline()
        {
            Target.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.WhiteSpace, Environment.NewLine));
        }
    }

    /// <summary>
    /// Wraps and truncates text for consumption in a tooltip.
    /// </summary>
    /// <typeparam name="TTargetType">Target tooltip type.</typeparam>
    internal abstract class TooltipWrapper<TTargetType>
    {
        private const uint DefaultMaxLines = 10;
        private const uint DefaultMaxWidth = 120;
        private const int DefaultLineIndentSpace = 0;

        private readonly TTargetType target;
        private readonly StringBuilder currentLine = new();
        private readonly StringBuilder currentWord = new();
        private readonly uint lineIndentSize;

        private uint maxLines;
        private uint maxWidth;
        private int currentLines;

        /// <summary>
        /// Gets a string of spaces of the size of lineIndentSpace.
        /// </summary>
        private string LineIndent => new(' ', (int)lineIndentSize);

        /// <summary>
        /// Initializes a new instance of the <see cref="TooltipWrapper{TTargetType}"/> class.
        /// </summary>
        /// <param name="target">Target tooltip.</param>
        /// <param name="lineIndentSize">Set the indent size of each line on the target.</param>
        public TooltipWrapper(TTargetType target, uint? lineIndentSize = 0)
        {
            this.target = target;
            this.lineIndentSize = lineIndentSize ?? DefaultLineIndentSpace;
        }

        /// <summary>
        /// Gets the target object.
        /// </summary>
        protected TTargetType Target => target;

        /// <summary>
        /// Gets a value indicating whether the last line of the target is empty.
        /// </summary>
        /// <returns>Value indicating whether the last line of the target is empty.</returns>
        protected abstract bool IsLastLineEmpty { get; }

        /// <summary>
        /// Truncates and trims a string and adds it to the collection.
        /// </summary>
        /// <param name="text">Text to evaluate.</param>
        /// <param name="maxLines">Maximum number of lines for the tooltip. Optional.</param>
        /// <param name="maxWidth">Maximum number of characters per line. Optional.</param>
        public void WrapAndTruncate(string text, uint? maxLines = null, uint? maxWidth = null)
        {
            this.maxLines = maxLines ?? DefaultMaxLines;
            this.maxWidth = maxWidth ?? DefaultMaxWidth;
            this.maxWidth -= lineIndentSize;
            // Add indentation for the first line
            currentLine.Clear();
            currentWord.Clear();
            currentLines = 0;
            var trimmedText = text.Trim();
            var inWhitespace = false;

            for (var i = 0; i < trimmedText.Length; i++)
            {
                var c = trimmedText[i];

                // Special case newlines so that we can respect current formatting
                if (IsNewlineCharacter(c))
                {
                    // First try using TryAppendLine to wrap the contents if needed
                    if (TryAppendLine(out var limitReached) && limitReached)
                    {
                        return;
                    }

                    // Write the remaining contents of the line
                    if (currentLine.Length > 0)
                    {
                        AddNewLineIfNeeded();
                        AddLine(LineIndent + currentLine.ToString());
                        currentLine.Clear();
                        currentLines++;
                    }

                    if (AppendEllipsisIfNeeded())
                    {
                        return;
                    }

                    // Move the counter forward in cases of \r\n to avoid appending 2 newlines
                    if (c == '\r' && i + 1 < trimmedText.Length && trimmedText[i + 1] == '\n')
                    {
                        i++;
                    }

                    // Continue so that the newline is not added to currentWord
                    continue;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (!inWhitespace)
                    {
                        if (TryAppendLine(out var limitReached) && limitReached)
                        {
                            return;
                        }
                    }

                    inWhitespace = true;
                }
                else
                {
                    inWhitespace = false;
                }

                currentWord.Append(c);
            }

            if (currentWord.Length > 0 || currentLine.Length > 0)
            {
                // Process the last set of data
                TryAppendLine(out var limitReached);

                // Write the last line if needed
                if (!limitReached && currentLine.Length > 0)
                {
                    if (currentLines > 0)
                    {
                        AddNewline();
                    }

                    AddLine(LineIndent + currentLine.ToString());
                }
            }
        }

        /// <summary>
        /// Adds a line of text to the target.
        /// </summary>
        /// <param name="line">Line of text to add.</param>
        protected abstract void AddLine(string line);

        /// <summary>
        /// Adds an ellipsis to the target.
        /// </summary>
        protected abstract void AddEllipsis();

        /// <summary>
        /// Adds a new line to the target.
        /// </summary>
        protected abstract void AddNewline();

        /// <summary>
        /// Appends a line to the elements if the <see name="currentWord"/> would force it to be outside the
        /// max width. Otherwise it adds the <see name="currentWord"/> to the <see name="currentLine"/>.
        /// </summary>
        /// <param name="limitReached">Value indicating whether the limit of lines has been reached.</param>
        /// <returns>Value indicating whether <see name="currentLine"/> was added to elements.</returns>
        private bool TryAppendLine(
            out bool limitReached)
        {
            limitReached = false;
            if (currentLine.Length + currentWord.Length >= maxWidth)
            {
                if (currentLine.Length == 0)
                {
                    // Special case for words bigger than maxWidth. Add the current
                    // word after trimming any whitespace
                    currentLine.Append(currentWord.ToString().Trim());
                    currentWord.Clear();
                }

                AddNewLineIfNeeded();
                AddLine(LineIndent + currentLine.ToString());
                currentLine.Clear();

                currentLine.Append(currentWord.ToString().TrimStart());
                currentLines++;
                limitReached = AppendEllipsisIfNeeded();

                currentWord.Clear();
                return true;
            }
            else
            {
                currentLine.Append(currentWord);
                currentWord.Clear();
                return false;
            }
        }

        /// <summary>
        /// Adds a new line if there are other lines in the collection and the previous line was not empty.
        /// </summary>
        private void AddNewLineIfNeeded()
        {
            if (currentLines > 0 && !IsLastLineEmpty)
            {
                AddNewline();
            }
        }

        /// <summary>
        /// Adds an ellipsis if the the line limit has been reached.
        /// </summary>
        /// <returns>Value indicating whether the ellipsis was added.</returns>
        private bool AppendEllipsisIfNeeded()
        {
            if (currentLines >= maxLines)
            {
                // Reached the limit, add the ellipsis and stop processing
                AddNewline();
                AddEllipsis();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the character is a new line character.
        /// </summary>
        /// <param name="c">Character to evaluate.</param>
        /// <returns>Value indicating whether the character is a new line character.</returns>
        private static bool IsNewlineCharacter(char c)
        {
            switch (c)
            {
                case '\n': // Line Feed
                case '\r': // Carriage Return
                case '\u0085': // Next Line
                case '\u2028': // Line Separator
                case '\u2029': // Paragraph Separator
                    return true;
                default:
                    return false;
            }
        }
    }
}
