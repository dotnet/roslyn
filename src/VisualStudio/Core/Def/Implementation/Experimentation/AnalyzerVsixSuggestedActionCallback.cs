// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Experimentation
{
    [Export(typeof(ISuggestedActionCallback))]
    internal class AnalyzerVsixSuggestedActionCallback : ForegroundThreadAffinitizedObject, ISuggestedActionCallback
    {
        private const string AnalyzerEnabledFlight = @"LiveCA/LiveCAcf";
        private const string AnalyzerVsixHyperlink = @"https://aka.ms/livecodeanalysis";
        private static readonly Guid FxCopAnalyzersPackageGuid = Guid.Parse("{4A41D270-A97F-4639-A352-28732FC410E4}");

        private readonly VisualStudioWorkspace _workspace;
        private readonly IForegroundNotificationService _foregroundNotificationService;
        private readonly IAsynchronousOperationListener _operationListener;
        private readonly SVsServiceProvider _serviceProvider;

        private readonly object _scheduleLock = new object();
        // If we are currently checking to see if the user fits the treatment profile, then we don't want to schedule another check
        private bool _checksRunning = false;
        // We don't show the user an infobar more than once per VS session
        private bool _infoBarShown = false;
        private FxCopInstallStatus _installStatus = FxCopInstallStatus.Unchecked;
        // Initialized the first time we're on the UI thread
        private IExperimentationService _experimentationService;

        [ImportingConstructor]
        public AnalyzerVsixSuggestedActionCallback(VisualStudioWorkspace workspace,
            IForegroundNotificationService foregroundNotificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners,
            SVsServiceProvider serviceProvider)
        {
            _workspace = workspace;
            _foregroundNotificationService = foregroundNotificationService;
            _operationListener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.ABTest);
            _serviceProvider = serviceProvider;
        }

        public void OnSuggestedActionExecuted(SuggestedAction action)
        {
            AssertIsForeground();

            // Initialize the experimentation service if it hasn't yet been fetched
            if (_experimentationService == null)
            {
                _experimentationService = _workspace.Services.GetRequiredService<IExperimentationService>();
            }

            // If we haven't yet checked to see if the VSIX is installed, check now
            if (_installStatus == FxCopInstallStatus.Unchecked)
            {
                var vsShell = _serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
                ErrorHandler.ThrowOnFailure(vsShell.IsPackageInstalled(FxCopAnalyzersPackageGuid, out int installed));
                _installStatus = installed != 0 ? FxCopInstallStatus.Installed : FxCopInstallStatus.NotInstalled;
            }

            // Only proceed if the VSIX isn't installed
            if (_installStatus == FxCopInstallStatus.Installed)
            {
                return;
            }

            lock (_scheduleLock)
            {
                if (!_checksRunning && !_infoBarShown)
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
                // We set _infoBarShown to true here to make sure that we never run these checks again
                lock(_scheduleLock)
                {
                    _checksRunning = false;
                    _infoBarShown = true;
                }

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

            ShowInfoBarIfNecessary();
        }

        private void ShowInfoBarIfNecessary()
        {
            if (_experimentationService.IsExperimentEnabled(AnalyzerEnabledFlight))
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
                                      action: DoNotShowAgain));

                    lock(_scheduleLock)
                    {
                        _checksRunning = false;
                        _infoBarShown = true;
                    }
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

        private void OpenInstallHyperlink()
        {
            System.Diagnostics.Process.Start(AnalyzerVsixHyperlink);
        }

        private void DoNotShowAgain()
        {
            _workspace.Options = _workspace.Options.WithChangedOption(AnalyzerABTestOptions.NeverShowAgain, true);
        }

        private enum FxCopInstallStatus
        {
            Unchecked,
            Installed,
            NotInstalled
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
