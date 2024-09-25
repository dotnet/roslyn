// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo;

/// <summary>
/// Interaction logic for OnTheFlyDocsView.xaml.
/// </summary>
internal sealed partial class OnTheFlyDocsView : UserControl, INotifyPropertyChanged
{
    private readonly ITextView _textView;
    private readonly IViewElementFactoryService _viewElementFactoryService;
    private readonly IAsynchronousOperationListener _asyncListener;
    private readonly IAsyncQuickInfoSession _asyncQuickInfoSession;
    private readonly IThreadingContext _threadingContext;
    private readonly Document _document;
    private readonly OnTheFlyDocsElement _onTheFlyDocsElement;
    private readonly ContentControl _responseControl = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

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

    public OnTheFlyDocsView(ITextView textView, IViewElementFactoryService viewElementFactoryService, IAsynchronousOperationListenerProvider listenerProvider, IAsyncQuickInfoSession asyncQuickInfoSession, IThreadingContext threadingContext, EditorFeaturesOnTheFlyDocsElement editorFeaturesOnTheFlyDocsElement)
    {
        _textView = textView;
        _viewElementFactoryService = viewElementFactoryService;
        _asyncListener = listenerProvider.GetListener(FeatureAttribute.OnTheFlyDocs);
        _asyncQuickInfoSession = asyncQuickInfoSession;
        _threadingContext = threadingContext;
        _onTheFlyDocsElement = editorFeaturesOnTheFlyDocsElement.OnTheFlyDocsElement;
        _document = editorFeaturesOnTheFlyDocsElement.Document;

        var sparkle = new ImageElement(new VisualStudio.Core.Imaging.ImageId(CopilotConstants.CopilotIconMonikerGuid, CopilotConstants.CopilotIconSparkleId));
        object onDemandLinkText = _onTheFlyDocsElement.IsContentExcluded
            ? ToUIElement(new ContainerElement(ContainerElementStyle.Wrapped, new ClassifiedTextElement([new ClassifiedTextRun(ClassificationTypeNames.Text, EditorFeaturesResources.Describe_with_Copilot_is_unavailable_since_the_referenced_document_is_excluded_by_your_organization)])))
            : ClassifiedTextElement.CreateHyperlink(EditorFeaturesResources.Describe_with_Copilot, EditorFeaturesResources.Generate_summary_with_Copilot, () => RequestResults());

        OnDemandLinkContent = ToUIElement(
            new ContainerElement(
                ContainerElementStyle.Wrapped,
                new object[]
                {
                    sparkle,
                    onDemandLinkText,
                }));

        LoadingContent = ToUIElement(
            new ContainerElement(
                ContainerElementStyle.Stacked,
                new object[]
                {
                    new ClassifiedTextElement(new ClassifiedTextRun(
                        ClassificationTypeDefinitions.ReducedEmphasisText, EditorFeaturesResources.Copilot_thinking)),
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
                            ClassifiedTextElement.CreatePlainText(EditorFeaturesResources.Copilot),
                        }),
                    new ThematicBreakElement(),
                    _responseControl,
                }));

        ResultsRequested += (_, _) => PopulateAIDocumentationElements(_cancellationTokenSource.Token);
        _asyncQuickInfoSession.StateChanged += (_, _) => OnQuickInfoSessionChanged();
        InitializeComponent();
    }

    /// <summary>
    /// Retrieves the documentation for the given symbol from the Copilot service and displays it in the view.
    /// </summary>
    private void PopulateAIDocumentationElements(CancellationToken cancellationToken)
    {
        var token = _asyncListener.BeginAsyncOperation(nameof(SetResultTextAsync));
        var copilotService = _document.GetLanguageService<ICopilotCodeAnalysisService>();
        if (copilotService is not null)
        {
            _ = SetResultTextAsync(copilotService, cancellationToken).CompletesAsyncOperation(token);
        }
    }

    private async Task SetResultTextAsync(ICopilotCodeAnalysisService copilotService, CancellationToken cancellationToken)
    {
        var stopwatch = SharedStopwatch.StartNew();

        try
        {
            var response = await copilotService.GetOnTheFlyDocsAsync(_onTheFlyDocsElement.SymbolSignature, _onTheFlyDocsElement.DeclarationCode, _onTheFlyDocsElement.Language, cancellationToken).ConfigureAwait(false);
            var copilotRequestTime = stopwatch.Elapsed;

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

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

                Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Results_Displayed, KeyValueLogMessage.Create(m =>
                {
                    m["ElapsedTime"] = copilotRequestTime;
                    m["ResponseLength"] = response.Length;
                }, LogLevel.Information));
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Results_Canceled, KeyValueLogMessage.Create(m =>
            {
                m["ElapsedTime"] = stopwatch.Elapsed;
            }, LogLevel.Information));
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e))
        {
        }
    }

    private void OnQuickInfoSessionChanged()
    {
        if (_asyncQuickInfoSession.State == QuickInfoSessionState.Dismissed)
        {
            _cancellationTokenSource.Cancel();
        }
    }

    public OnTheFlyDocsState CurrentState
    {
        get => _currentState;
        set => OnPropertyChanged(ref _currentState, value);
    }

    public UIElement OnDemandLinkContent { get; }

    public UIElement LoadingContent { get; }

    public UIElement ResultsContent { get; }

    public void RequestResults()
    {
        CurrentState = OnTheFlyDocsState.Loading;
        Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Loading_State_Entered, KeyValueLogMessage.Create(m =>
        {
            m["HasDocumentationComments"] = _onTheFlyDocsElement.HasComments;
        }, LogLevel.Information));

        OnTheFlyDocsLogger.LogOnTheFlyDocsResultsRequested();
        if (_onTheFlyDocsElement.HasComments)
        {
            OnTheFlyDocsLogger.LogOnTheFlyDocsResultsRequestedWithDocComments();
        }

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
