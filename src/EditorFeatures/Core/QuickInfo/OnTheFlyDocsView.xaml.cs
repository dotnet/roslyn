// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Editor.Copilot;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.QuickInfo.Presentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Telemetry;
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
    private readonly OnTheFlyDocsInfo _onTheFlyDocsInfo;
    private readonly ContentControl _responseControl = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly List<ClassifiedTextRun> quotaExceededContent;
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Event that fires when the user requests results.
    /// </summary>
    public event EventHandler ResultsRequested;

    /// <summary>
    /// Event that fires when the user requests to upgrade their Copilot plan.
    /// </summary>
    public event EventHandler? PlanUpgradeRequested;

#pragma warning disable CA1822 // Mark members as static
    /// <summary>
    /// Used to display the "On the fly documentation" directly in the associated XAML file.
    /// </summary>
    public string OnTheFlyDocumentation => EditorFeaturesResources.On_the_fly_documentation;
#pragma warning restore CA1822 // Mark members as static

    public OnTheFlyDocsView(
        ITextView textView,
        IViewElementFactoryService viewElementFactoryService,
        IAsynchronousOperationListenerProvider listenerProvider,
        IAsyncQuickInfoSession asyncQuickInfoSession,
        IThreadingContext threadingContext,
        QuickInfoOnTheFlyDocsElement onTheFlyDocsElement,
        IServiceProvider serviceProvider)
    {
        _textView = textView;
        _viewElementFactoryService = viewElementFactoryService;
        _asyncListener = listenerProvider.GetListener(FeatureAttribute.OnTheFlyDocs);
        _asyncQuickInfoSession = asyncQuickInfoSession;
        _threadingContext = threadingContext;
        _onTheFlyDocsInfo = onTheFlyDocsElement.Info;
        _document = onTheFlyDocsElement.Document;
        _serviceProvider = serviceProvider;

        var sparkle = new ImageElement(new VisualStudio.Core.Imaging.ImageId(CopilotConstants.CopilotIconMonikerGuid, CopilotConstants.CopilotIconSparkleId));
        object onDemandLinkText = _onTheFlyDocsInfo.IsContentExcluded
            ? ToUIElement(new ContainerElement(ContainerElementStyle.Wrapped, new ClassifiedTextElement([new ClassifiedTextRun(ClassificationTypeNames.Text, EditorFeaturesResources.Describe_is_unavailable_since_the_referenced_document_is_excluded_by_your_organization)])))
            : ClassifiedTextElement.CreateHyperlink(EditorFeaturesResources.Describe, EditorFeaturesResources.Generate_summary_with_Copilot, () => RequestResults());

        OnDemandLinkContent = ToUIElement(
            new ContainerElement(
                ContainerElementStyle.Wrapped,
                [
                    sparkle,
                    onDemandLinkText,
                ]));

        LoadingContent = ToUIElement(
            new ContainerElement(
                ContainerElementStyle.Stacked,
                [
                    new ClassifiedTextElement(new ClassifiedTextRun(
                        ClassificationTypeDefinitions.ReducedEmphasisText, EditorFeaturesResources.Copilot_thinking)),
                    new SmoothProgressBar { IsIndeterminate = true, Height = 2, Margin = new Thickness { Top = 2 } },
                ]));

        // Ensure the loading content stretches so that the progress bar
        // takes the entire width of the quick info tooltip.
        if (LoadingContent is FrameworkElement element)
        {
            element.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        ResultsContent = ToUIElement(
            new ContainerElement(
                ContainerElementStyle.Stacked,
                [
                    new ContainerElement(
                        ContainerElementStyle.Wrapped,
                        [
                            sparkle,
                            ClassifiedTextElement.CreatePlainText(EditorFeaturesResources.Copilot),
                        ]),
                    new ThematicBreakElement(),
                    _responseControl,
                ]));

        // Locates the "upgrade now" link in the localized text, surrounded by square brackets.
        var quotaExceededMatch = Regex.Match(
                EditorFeaturesResources.Chat_limit_reached_upgrade_now_or_wait_for_the_limit_to_reset,
                @"^(.*)\[(.*)\](.*)$");
        if (quotaExceededMatch == null)
        {
            // The text wasn't localized correctly. Assert and fallback to showing it verbatim.
            Debug.Fail("Copilot Hover quota exceeded message was not correctly localized.");
            quotaExceededContent = [new ClassifiedTextRun(
                ClassifiedTextElement.TextClassificationTypeName,
                EditorFeaturesResources.Chat_limit_reached_upgrade_now_or_wait_for_the_limit_to_reset)];
        }
        else
        {
            quotaExceededContent = [
                new ClassifiedTextRun(
                    ClassifiedTextElement.TextClassificationTypeName,
                    quotaExceededMatch.Groups[1].Value),
                    new ClassifiedTextRun(
                        ClassifiedTextElement.TextClassificationTypeName,
                        quotaExceededMatch.Groups[2].Value,
                        () => this.PlanUpgradeRequested?.Invoke(this, EventArgs.Empty)),
                    new ClassifiedTextRun(
                        ClassifiedTextElement.TextClassificationTypeName,
                        quotaExceededMatch.Groups[3].Value)];
        }

        ResultsRequested += (_, _) => PopulateAIDocumentationElements(_cancellationTokenSource.Token);
        _asyncQuickInfoSession.StateChanged += (_, _) => OnQuickInfoSessionChanged();
        InitializeComponent();
    }

    /// <summary>
    /// Retrieves the documentation for the given symbol from the Copilot service and displays it in the view.
    /// </summary>
    private void PopulateAIDocumentationElements(CancellationToken cancellationToken)
    {
        try
        {
            var token = _asyncListener.BeginAsyncOperation(nameof(SetResultTextAsync));
            var copilotService = _document.GetLanguageService<ICopilotCodeAnalysisService>();
            if (copilotService is not null)
            {
                _ = SetResultTextAsync(copilotService, cancellationToken).CompletesAsyncOperation(token);
            }
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex))
        {
        }
    }

    private async Task SetResultTextAsync(ICopilotCodeAnalysisService copilotService, CancellationToken cancellationToken)
    {
        var stopwatch = SharedStopwatch.StartNew();

        try
        {
            var prompt = await copilotService.GetOnTheFlyDocsPromptAsync(_onTheFlyDocsInfo, cancellationToken).ConfigureAwait(false);
            var (responseString, isQuotaExceeded) = await copilotService.GetOnTheFlyDocsResponseAsync(prompt, cancellationToken).ConfigureAwait(false);
            var copilotRequestTime = stopwatch.Elapsed;

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(responseString))
            {
                // If the responseStatus is 8, then that means the quota has been exceeded.
                if (isQuotaExceeded)
                {
                    this.PlanUpgradeRequested += (_, _) =>
                    {
                        // GUID and command ID from
                        // https://dev.azure.com/devdiv/DevDiv/_wiki/wikis/DevDiv.wiki/45121/Free-SKU-Handling-Guidance-and-Recommendations
                        var uiShell = _serviceProvider.GetServiceOnMainThread<SVsUIShell, IVsUIShell>();
                        uiShell.PostExecCommand(
                            CopilotConstants.CopilotQuotaExceededGuid,
                            0x3901,
                            nCmdexecopt: 0,
                            pvaIn: null);

                        _asyncQuickInfoSession.DismissAsync();

                        // Telemetry to track when users reach the quota of the Copilot Free plan.
                        var telemetryEvent = new OperationEvent(
                            "vs/copilot/showcopilotfreestatus",
                            TelemetryResult.Success);
                        telemetryEvent.Properties["vs.copilot.source"] = "CSharpOnTheFlyDocs";
                        TelemetryService.DefaultSession.PostEvent(telemetryEvent);
                    };

                    ShowQuotaExceededResult();
                }
                else
                {
                    SetResultText(EditorFeaturesResources.An_error_occurred_while_generating_documentation_for_this_code);
                    Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Error_Displayed, KeyValueLogMessage.Create(static (m, copilotRequestTime) =>
                    {
                        m["ElapsedTime"] = copilotRequestTime;
                    }, copilotRequestTime, LogLevel.Information));
                }

                CurrentState = OnTheFlyDocsState.Finished;
            }
            else
            {
                SetResultText(responseString);
                CurrentState = OnTheFlyDocsState.Finished;

                Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Results_Displayed, KeyValueLogMessage.Create(static (m, args) =>
                {
                    var (copilotRequestTime, responseString) = args;
                    m["ElapsedTime"] = copilotRequestTime;
                    m["ResponseLength"] = responseString.Length;
                }, (copilotRequestTime, responseString), LogLevel.Information));
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Results_Canceled, KeyValueLogMessage.Create(static (m, stopwatch) =>
            {
                m["ElapsedTime"] = stopwatch.Elapsed;
            }, stopwatch, LogLevel.Information));
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e))
        {
        }

        void ShowQuotaExceededResult()
        {
            _responseControl.Content = ToUIElement(new ContainerElement(ContainerElementStyle.Stacked,
                [new ContainerElement(ContainerElementStyle.Wrapped, new ClassifiedTextElement(this.quotaExceededContent))]));
        }

        void SetResultText(string text)
        {
            _responseControl.Content = ToUIElement(
                new ContainerElement(ContainerElementStyle.Wrapped, new ClassifiedTextElement([new ClassifiedTextRun(ClassificationTypeNames.Text, text)])));
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
        get;
        set => OnPropertyChanged(ref field, value);
    } = OnTheFlyDocsState.OnDemandLink;

    public UIElement OnDemandLinkContent { get; }

    public UIElement LoadingContent { get; }

    public UIElement ResultsContent { get; }

    private void RequestResults()
    {
        try
        {
            CurrentState = OnTheFlyDocsState.Loading;
            Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Loading_State_Entered, KeyValueLogMessage.Create(static (m, _onTheFlyDocsInfo) =>
            {
                m["HasDocumentationComments"] = _onTheFlyDocsInfo.HasComments;
            }, _onTheFlyDocsInfo, LogLevel.Information));

            OnTheFlyDocsLogger.LogOnTheFlyDocsResultsRequested();
            if (_onTheFlyDocsInfo.HasComments)
            {
                OnTheFlyDocsLogger.LogOnTheFlyDocsResultsRequestedWithDocComments();
            }

            ResultsRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e))
        {
        }
    }

    private void OnPropertyChanged<T>(ref T member, T value, [CallerMemberName] string? name = null)
    {
        member = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private UIElement ToUIElement(object model)
        => _viewElementFactoryService.CreateViewElement<UIElement>(_textView, model);
}
