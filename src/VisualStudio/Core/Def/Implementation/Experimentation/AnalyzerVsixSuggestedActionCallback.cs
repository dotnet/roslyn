// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Experimentation
{
    [Export(typeof(ISuggestedActionCallback))]
    internal class AnalyzerVsixSuggestedActionCallback : ForegroundThreadAffinitizedObject, ISuggestedActionCallback
    {
        private const string AnalyzerEnabledFlight = @"LiveCA/LiveCAcf";
        private const string AnalyzerVsixHyperlink = @"https://go.microsoft.com/fwlink/?linkid=849061";
        private static readonly Guid FxCopAnalyzersPackageGuid = Guid.Parse("{4A41D270-A97F-4639-A352-28732FC410E4}");

        private readonly VisualStudioWorkspace _workspace;
        private readonly SVsServiceProvider _serviceProvider;

        /// <summary>
        /// Tracks when the bar is shown so we don't show to the user more than once per session
        /// </summary>
        private bool _infoBarShown = false;

        /// <summary>
        /// This service is initialzed by <see cref="OnSuggestedActionExecuted(SuggestedAction)"/>
        /// </summary>
        private IExperimentationService _experimentationService;

        private LiveCodeAnalysisInstallStatus _installStatus = LiveCodeAnalysisInstallStatus.Unknown;

        [ImportingConstructor]
        public AnalyzerVsixSuggestedActionCallback(
            VisualStudioWorkspace workspace,
            SVsServiceProvider serviceProvider)
        {
            _workspace = workspace;
            _serviceProvider = serviceProvider;
        }

        public void OnSuggestedActionExecuted(SuggestedAction action)
        {
            // If the experimentation service hasn't been retrieved, we'll need to be on the UI
            // thread to get it
            AssertIsForeground();

            // If the user has previously clicked don't show again, then we bail out immediately
            if (_workspace.Options.GetOption(AnalyzerABTestOptions.NeverShowAgain))
            {
                return;
            }

            // Initialize the experimentation service if it hasn't yet been fetched
            if (_experimentationService == null)
            {
                _experimentationService = _workspace.Services.GetRequiredService<IExperimentationService>();
            }

            // If we haven't yet checked to see if the VSIX is installed, check now
            if (_installStatus == LiveCodeAnalysisInstallStatus.Unknown)
            {
                var vsShell = _serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
                var hr = vsShell.IsPackageInstalled(FxCopAnalyzersPackageGuid, out int installed);
                if (ErrorHandler.Failed(hr))
                {
                    FatalError.ReportWithoutCrash(Marshal.GetExceptionForHR(hr));

                    // We set installed to ensure we don't go through this again next time a
                    // suggested action is called, and we don't want to continue if the shell
                    // is busted.
                    _installStatus = LiveCodeAnalysisInstallStatus.Installed;
                    return;
                }
                _installStatus = installed != 0 ? LiveCodeAnalysisInstallStatus.Installed : LiveCodeAnalysisInstallStatus.NotInstalled;
            }

            // Only proceed if the VSIX isn't installed
            if (_installStatus == LiveCodeAnalysisInstallStatus.Installed)
            {
                return;
            }

            if (!_infoBarShown)
            {
                SuggestedActionOccurred();
            }
        }

        private void SuggestedActionOccurred()
        {
            var options = _workspace.Options;

            // Filter for valid A/B test candidates. Candidates fill the following critera:
            //     1: Are a Dotnet user (as evidenced by the fact that this code is being run)
            //     2: Have triggered a lightbulb on 3 separate days

            // If the user hasn't met candidacy conditions, then we check them. Otherwise, proceed
            // to info bar check
            var isCandidate = options.GetOption(AnalyzerABTestOptions.HasMetCandidacyRequirements);
            if (!isCandidate)
            {
                // We store in UTC to avoid any timezone offset weirdness
                var lastTriggeredTime = DateTime.FromBinary(options.GetOption(AnalyzerABTestOptions.LastDateTimeUsedSuggestionAction));
                var currentTime = DateTime.UtcNow;
                var span = currentTime - lastTriggeredTime;
                if (span.TotalDays >= 1)
                {
                    options = options.WithChangedOption(AnalyzerABTestOptions.LastDateTimeUsedSuggestionAction, currentTime.ToBinary());
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

            // If the user still isn't a candidate, then return. Otherwise, we move to checking if
            // the infobar should be shown
            if (!isCandidate)
            {
                return;
            }

            ShowInfoBarIfNecessary();
        }

        private void ShowInfoBarIfNecessary()
        {
            if (_experimentationService.IsExperimentEnabled(AnalyzerEnabledFlight))
            {
                // If we got true from the experimentation service, then we're in the treatment
                // group, and the experiment is enabled. We determine if the infobar has been
                // displayed in the past 24 hours. If it hasn't been displayed, then we do so now.
                var lastDayInfoBarShown = DateTime.FromBinary(_workspace.Options.GetOption(AnalyzerABTestOptions.LastDateTimeInfoBarShown));
                var utcNow = DateTime.UtcNow;
                var timeSinceLastShown = utcNow - lastDayInfoBarShown;

                if (timeSinceLastShown.TotalDays >= 1)
                {
                    _workspace.Options = _workspace.Options.WithChangedOption(AnalyzerABTestOptions.LastDateTimeInfoBarShown, utcNow.ToBinary());

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

                    _infoBarShown = true;
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
    }
}
