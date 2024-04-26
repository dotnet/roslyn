// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo;

/// <summary>
/// Interaction logic for OnTheFlyDocsView.xaml.
/// </summary>
internal sealed partial class OnTheFlyDocsView : UserControl, INotifyPropertyChanged
{
    private readonly ITextView _textView;
    private readonly IViewElementFactoryService _viewElementFactoryService;
    private readonly IThreadingContext _threadingContext;
    private readonly Document _document;
    private readonly OnTheFlyDocsElement _onTheFlyDocsElement;
    private readonly ContentControl _responseControl = new();
    private OnTheFlyDocsState _currentState = OnTheFlyDocsState.OnDemandLink;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Event that fires when the user requests results.
    /// </summary>
    public event EventHandler ResultsRequested;

#pragma warning disable CA1822 // Mark members as static
    /// <summary>
    /// Used to display the "On the fly documentation" directly in the associated XAML file.
    /// </summary>
    public string OnTheFlyDocumentation => EditorFeaturesResources.On_the_fly_documentation;
#pragma warning restore CA1822 // Mark members as static

    public OnTheFlyDocsView(ITextView textView, IViewElementFactoryService viewElementFactoryService, IThreadingContext threadingContext, EditorFeaturesOnTheFlyDocsElement editorFeaturesOnTheFlyDocsElement)
    {
        _textView = textView;
        _viewElementFactoryService = viewElementFactoryService;
        _threadingContext = threadingContext;
        _onTheFlyDocsElement = editorFeaturesOnTheFlyDocsElement.OnTheFlyDocsElement;
        _document = editorFeaturesOnTheFlyDocsElement.Document;

        var sparkle = new ImageElement(new VisualStudio.Core.Imaging.ImageId(CopilotConstants.CopilotIconMonikerGuid, CopilotConstants.CopilotIconSparkleId));

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
                            ClassifiedTextElement.CreatePlainText(EditorFeaturesResources.GitHub_Copilot),
                        }),
                    new ThematicBreakElement(),
                    _responseControl,
                    new ThematicBreakElement(),
                    new ClassifiedTextElement(new ClassifiedTextRun(
                        ClassificationTypeDefinitions.ReducedEmphasisText, EditorFeaturesResources.AI_generated_content_may_be_inaccurate)),
                }));

        ResultsRequested += (_, _) => PopulateAIDocumentationElements(_threadingContext.DisposalToken);
        InitializeComponent();
    }

    /// <summary>
    /// Retrieves the documentation for the given symbol from the Copilot service and displays it in the view.
    /// </summary>
    private async void PopulateAIDocumentationElements(CancellationToken cancellationToken)
    {
        var copilotRequestTime = TimeSpan.Zero;
        var copilotService = _document.GetRequiredLanguageService<ICopilotCodeAnalysisService>();
        var stopwatch = Stopwatch.StartNew();
        var response = await copilotService.GetOnTheFlyDocsAsync(_onTheFlyDocsElement.DescriptionText, _onTheFlyDocsElement.SymbolReferences, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        copilotRequestTime = stopwatch.Elapsed;

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        if (response is null || response.Length == 0)
        {
            SetResultText(EditorFeaturesResources.An_error_occurred_while_generating_documentation_for_this_code);
            CurrentState = OnTheFlyDocsState.Finished;
            Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Error_Displayed, KeyValueLogMessage.Create(m =>
            {
                m["ElapsedTime"] = copilotRequestTime;
            }, LogLevel.Information));
        }
        else
        {
            SetResultText(response);
            CurrentState = OnTheFlyDocsState.Finished;

            Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Error_Displayed, KeyValueLogMessage.Create(m =>
            {
                m["ElapsedTime"] = copilotRequestTime;
                m["ResponseLength"] = response.Length;
            }, LogLevel.Information));
        }
    }

    /// <summary>
    /// Gets or sets the current state of the view.
    /// </summary>
    public OnTheFlyDocsState CurrentState
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
        CurrentState = OnTheFlyDocsState.Loading;
        Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Loading_State_Entered, KeyValueLogMessage.Create(m =>
        {
            m["SymbolHeaderText"] = _onTheFlyDocsElement.DescriptionText;
        }, LogLevel.Information));

        ResultsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the text of the AI-generated response.
    /// </summary>
    /// <param name="text">Response text to display.</param>
    public void SetResultText(string text)
    {
        _responseControl.Content = ToUIElement(
            new ContainerElement(ContainerElementStyle.Wrapped, new ClassifiedTextElement([new ClassifiedTextRun(ClassificationTypeNames.Text, text)])));
    }

    private void OnPropertyChanged<T>(ref T member, T value, [CallerMemberName] string? name = null)
    {
        member = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private UIElement ToUIElement(object model)
        => _viewElementFactoryService.CreateViewElement<UIElement>(_textView, model);
}
