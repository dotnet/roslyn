// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Experimentation
{
    [Export(typeof(ISuggestedActionCallback))]
    internal class AnalyzerVsixSuggestedActionCallback : ForegroundThreadAffinitizedObject, ISuggestedActionCallback
    {
        private const string AnalyzerEnabledFlight = @"LiveCA/LiveCAcf";
        private const string AnalyzerVsixHyperlink = @"https://aka.ms/livecodeanalysis";

        private readonly VisualStudioWorkspace _workspace;
        private readonly IForegroundNotificationService _foregroundNotificationService;
        private readonly IAsynchronousOperationListener _operationListener;

        private readonly object _scheduleLock = new object();
        private bool _checksRunning = false;

        [ImportingConstructor]
        public AnalyzerVsixSuggestedActionCallback(VisualStudioWorkspace workspace,
            IForegroundNotificationService foregroundNotificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _workspace = workspace;
            _foregroundNotificationService = foregroundNotificationService;
            _operationListener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.ABTest);
        }

        public void OnSuggestedActionExecuted(SuggestedAction action)
        {
            AssertIsForeground();

            lock (_scheduleLock)
            {
                if (!_checksRunning)
                {
                    _checksRunning = true;
                    Task.Run((Action)SuggestedActionOccurred);
                }
            }
        }

        private void SuggestedActionOccurred()
        {
            // We don't need to do this work on the UI thread
            AssertIsBackground();

            var options = _workspace.Options;

            // If the user has previously clicked don't show again, then we bail out immediately
            if (options.GetOption(AnalyzerABTestOptions.NeverShowAgain))
            {
                // We purposefully do not set _checksRunning back to false. That way, we'll never check again this session
                return;
            }

            // Filter for valid A/B test candidates. Candidates fill the following critera:
            //     1: Are a Dotnet user (as evidenced by the fact that this code is being run)
            //     2: Have triggered a lightbulb on 3 separate days

            // If the user hasn't met candidacy conditions, then we check them. Otherwise, proceed to info bar check
            var isCandidate = options.GetOption(AnalyzerABTestOptions.HasMetCandidacyRequirements);
            if (!isCandidate)
            {
                // We store in UTC to avoid any timezone offset weirdness
                var lastTriggeredTime = DateTime.FromBinary(options.GetOption(AnalyzerABTestOptions.LastDayUsedSuggestionAction));
                var currentTime = DateTime.UtcNow;
                var span = currentTime - lastTriggeredTime;
                if (span.TotalDays >= 1)
                {
                    options = options.WithChangedOption(AnalyzerABTestOptions.LastDayUsedSuggestionAction, currentTime.ToBinary());
                    var usageCount = options.GetOption(AnalyzerABTestOptions.UsedSuggestedActionCount);
                    usageCount++;
                    options = options.WithChangedOption(AnalyzerABTestOptions.UsedSuggestedActionCount, usageCount);

                    if (usageCount >= 3)
                    {
                        isCandidate = true;
                        options = options.WithChangedOption(AnalyzerABTestOptions.HasMetCandidacyRequirements, true);
                    }
                    _workspace.Options = options;
                }
            }

            // If the user still isn't a candidate, then return. Otherwise, we move to checking if the infobar should be shown
            if (!isCandidate)
            {
                lock (_scheduleLock)
                {
                    _checksRunning = false;
                }

                return;
            }

            // Queue the show notification bar to the UI thread. This will set _checksRunning back to true after it has shown the infobar
            _foregroundNotificationService.RegisterNotification((Action)ShowInfoBarIfNecessary,
                _operationListener.BeginAsyncOperation(nameof(ShowInfoBarIfNecessary)));
        }

        private void ShowInfoBarIfNecessary()
        {
            // We can only get the experimentation service on the UI thread
            AssertIsForeground();

            var experimentationService = _workspace.Services.GetRequiredService<IExperimentationService>();
            if (experimentationService.IsExperimentEnabled(AnalyzerEnabledFlight))
            {
                // If we got true from the experimentation service, then we're in the treatment group, and the experiment is enabled.
                // We determine if the infobar has been displayed in the past 24 hours. If it hasn't been displayed, then we do so now.
                var lastDayInfoBarShown = DateTime.FromBinary(_workspace.Options.GetOption(AnalyzerABTestOptions.LastDayInfoBarShown));
                var utcNow = DateTime.UtcNow;
                var timeSinceLastShown = utcNow - lastDayInfoBarShown;

                if (timeSinceLastShown.TotalDays >= 1)
                {
                    _workspace.Options = _workspace.Options.WithChangedOption(AnalyzerABTestOptions.LastDayInfoBarShown, utcNow.ToBinary());

                    var infoBarService = _workspace.Services.GetRequiredService<IInfoBarService>();
                    infoBarService.ShowInfoBarInGlobalView(
                        ServicesVSResources.Analyzer_vsix_try_description,
                        // Install link
                        new InfoBarUI(title: ServicesVSResources.Analyzer_vsix_hyperlink,
                                      kind: InfoBarUI.UIKind.HyperLink,
                                      action: OpenInstallHyperlink),
                        // Don't show the InfoBar again link
                        new InfoBarUI(title: ServicesVSResources.Analyzer_vsix_do_not_show_again,
                                      kind: InfoBarUI.UIKind.Button,
                                      action: DoNotShowAgain),
                        // This element isn't shown, it defines a callback that will be run on InfoBar close
                        new InfoBarUI(title: string.Empty,
                                      kind: InfoBarUI.UIKind.Close,
                                      action: ReleaseChecks));
                }
            }
            else
            {
                lock(_scheduleLock)
                {
                    _checksRunning = false;
                }
            }
        }

        private void ReleaseChecks()
        {
            lock (_scheduleLock)
            {
                _checksRunning = false;
            }
        }

        private void OpenInstallHyperlink()
        {
            System.Diagnostics.Process.Start(AnalyzerVsixHyperlink);
        }

        private void DoNotShowAgain()
        {
            _workspace.Options = _workspace.Options.WithChangedOption(AnalyzerABTestOptions.NeverShowAgain, true);
        }
    }

    internal static class AnalyzerABTestOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\Analyzers\AB\Vsix\";

        [ExportOption]
        public static readonly Option<long> LastDayUsedSuggestionAction = new Option<long>(nameof(AnalyzerABTestOptions), nameof(LastDayUsedSuggestionAction),
            defaultValue: DateTime.MinValue.ToBinary(),
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(LastDayUsedSuggestionAction)));

        [ExportOption]
        public static readonly Option<uint> UsedSuggestedActionCount = new Option<uint>(nameof(AnalyzerABTestOptions), nameof(UsedSuggestedActionCount),
            defaultValue: 0, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(UsedSuggestedActionCount)));

        [ExportOption]
        public static readonly Option<bool> NeverShowAgain = new Option<bool>(nameof(AnalyzerABTestOptions), nameof(NeverShowAgain),
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(NeverShowAgain)));

        [ExportOption]
        public static readonly Option<bool> HasMetCandidacyRequirements = new Option<bool>(nameof(AnalyzerABTestOptions), nameof(HasMetCandidacyRequirements),
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(HasMetCandidacyRequirements)));

        [ExportOption]
        public static readonly Option<long> LastDayInfoBarShown = new Option<long>(nameof(AnalyzerABTestOptions), nameof(LastDayInfoBarShown),
            defaultValue: DateTime.MinValue.ToBinary(),
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(LastDayInfoBarShown)));
    }
}
