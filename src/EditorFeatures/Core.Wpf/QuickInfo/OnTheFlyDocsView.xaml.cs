// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    /// <summary>
    /// Interaction logic for OnTheFlyDocsView.xaml.
    /// </summary>
    internal partial class OnTheFlyDocsView : UserControl, INotifyPropertyChanged
    {
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

        public OnTheFlyDocsView(ITextView textView, IViewElementFactoryService viewElementFactoryService, ImageId imageId)
        {
            _textView = textView;
            _viewElementFactoryService = viewElementFactoryService;
            var sparkle = new ImageElement(new );

            // Even though this control is obviously WPF-specific, we use the
            // cross-platform UI controls to get theming for free.
            this.OnDemandLinkContent = this.ToUIElement(
                new ContainerElement(
                    ContainerElementStyle.Wrapped,
                    new object[]
                    {
                        sparkle,
                        ClassifiedTextElement.CreateHyperlink(EditorFeaturesResources.Tell_me_more, EditorFeaturesResources.Show_an_AI_generated_summary_of_this_code, () =>
                        this.RequestResults()),
                    }));

            this.LoadingContent = this.ToUIElement(
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
            if (this.LoadingContent is FrameworkElement element)
            {
                element.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            this.ResultsContent = this.ToUIElement(
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
                        this._responseControl,
                        new ThematicBreakElement(),
                        new ClassifiedTextElement(new ClassifiedTextRun(
                            ClassificationTypeDefinitions.ReducedEmphasisText, EditorFeaturesResources.AI_generated_content_may_be_inaccurate)),
                    }));

            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the current state of the view.
        /// </summary>
        public State CurrentState
        {
            get => this._currentState;
            set => this.OnPropertyChanged(ref this._currentState, value);
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
            this.CurrentState = State.Loading;
            this.ResultsRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Sets the text of the AI-generated response.
        /// </summary>
        /// <param name="text">Response text to display.</param>
        public void SetResultText(string text)
        {
            List<ClassifiedTextRun> runs = new();
            new ClassifiedRunsTooltipWrapper(runs).WrapAndTruncate(text);
            this._responseControl.Content = this.ToUIElement(
                new ContainerElement(ContainerElementStyle.Wrapped, new ClassifiedTextElement(runs)));
        }

        private void OnPropertyChanged<T>(ref T member, T value, [CallerMemberName] string name = null)
        {
            member = value;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private UIElement ToUIElement(object model)
            => _viewElementFactoryService.CreateViewElement<UIElement>(this._textView, model);
    }
}
