// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Experimentation;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Experimentation
{
    [Export(typeof(ISuggestedActionCallback))]
    internal class AnalyzerVsixSuggestedActionCallback : ForegroundThreadAffinitizedObject, ISuggestedActionCallback
    {
        private const string AnalyzerEnabledFlight = @"LiveCA";
        private const string AnalyzerVsixHyperlink = @"https://go.microsoft.com/fwlink/?linkid=849061";
        private static readonly Guid FxCopAnalyzersPackageGuid = Guid.Parse("{4A41D270-A97F-4639-A352-28732FC410E4}");

        private readonly VisualStudioWorkspace _workspace;
        private readonly SVsServiceProvider _serviceProvider;

        /// <summary>
        /// Tracks when the bar is shown so we don't show to the user more than once per session
        /// </summary>
        private bool _infoBarChecked = false;

        /// <summary>
        /// This service is initialzed by <see cref="OnSuggestedActionExecuted(SuggestedAction)"/>
        /// </summary>
        private IExperimentationService _experimentationService;

        private LiveCodeAnalysisInstallStatus _installStatus = LiveCodeAnalysisInstallStatus.Unknown;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AnalyzerVsixSuggestedActionCallback(
            IThreadingContext threadingContext,
            VisualStudioWorkspace workspace,
            SVsServiceProvider serviceProvider)
            : base(threadingContext)
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

            EnsureInitialized();

            // Only show if the VSIX is not installed, the info bar hasn't been shown this session,
            // and the user is an A/B test candidate
            if (!_infoBarChecked &&
                !IsVsixInstalled() &&
                IsCandidate())
            {
                ShowInfoBarIfNecessary();
            }
        }

        private void EnsureInitialized()
        {
            // Initialize the experimentation service if it hasn't yet been fetched
            if (_experimentationService == null)
            {
                _experimentationService = _workspace.Services.GetRequiredService<IExperimentationService>();
            }
        }

        private bool IsVsixInstalled()
        {
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
                }
                else
                {
                    _installStatus = installed != 0 ? LiveCodeAnalysisInstallStatus.Installed : LiveCodeAnalysisInstallStatus.NotInstalled;
                    AnalyzerABTestLogger.LogInstallationStatus(_workspace, _installStatus);
                }
            }

            return _installStatus == LiveCodeAnalysisInstallStatus.Installed;
        }

        private bool IsCandidate()
        {
            // if this user ever participated in the experiment and then uninstall the vsix, then
            // this user will never be candidate again.
            if (_workspace.Options.GetOption(AnalyzerABTestOptions.ParticipatedInExperiment))
            {
                return false;
            }

            // Filter for valid A/B test candidates. Candidates fill the following critera:
            //     1: Are a Dotnet user (as evidenced by the fact that this code is being run)
            //     2: Have triggered a lightbulb on 3 separate days

            // If the user hasn't met candidacy conditions, then we check them. Otherwise, proceed
            // to info bar check
            var options = _workspace.Options;
            var isCandidate = options.GetOption(AnalyzerABTestOptions.HasMetCandidacyRequirements);
            if (!isCandidate)
            {
                // We store in UTC to avoid any timezone offset weirdness
                var lastTriggeredTimeBinary = options.GetOption(AnalyzerABTestOptions.LastDateTimeUsedSuggestionAction);
                AnalyzerABTestLogger.LogCandidacyRequirementsTracking(lastTriggeredTimeBinary);

                var lastTriggeredTime = DateTime.FromBinary(lastTriggeredTimeBinary);
                var currentTime = DateTime.UtcNow;
                var span = currentTime - lastTriggeredTime;
                if (span.TotalDays >= 1)
                {
                    options = options.WithChangedOption(AnalyzerABTestOptions.LastDateTimeUsedSuggestionAction, currentTime.ToBinary());

                    var usageCount = options.GetOption(AnalyzerABTestOptions.UsedSuggestedActionCount);
                    options = options.WithChangedOption(AnalyzerABTestOptions.UsedSuggestedActionCount, ++usageCount);

                    if (usageCount >= 3)
                    {
                        isCandidate = true;
                        options = options.WithChangedOption(AnalyzerABTestOptions.HasMetCandidacyRequirements, true);
                        AnalyzerABTestLogger.Log(nameof(AnalyzerABTestOptions.HasMetCandidacyRequirements));
                    }

                    _workspace.Options = options;
                }
            }

            return isCandidate;
        }

        private void ShowInfoBarIfNecessary()
        {
            // Only check for whether we should show an info bar once per session.
            _infoBarChecked = true;
            if (_experimentationService.IsExperimentEnabled(AnalyzerEnabledFlight))
            {
                AnalyzerABTestLogger.Log(nameof(AnalyzerEnabledFlight));

                // If we got true from the experimentation service, then we're in the treatment
                // group, and the experiment is enabled. We determine if the infobar has been
                // displayed in the past 24 hours. If it hasn't been displayed, then we do so now.
                var lastTimeInfoBarShown = DateTime.FromBinary(_workspace.Options.GetOption(AnalyzerABTestOptions.LastDateTimeInfoBarShown));
                var utcNow = DateTime.UtcNow;
                var timeSinceLastShown = utcNow - lastTimeInfoBarShown;

                if (timeSinceLastShown.TotalDays >= 1)
                {
                    _workspace.Options = _workspace.Options.WithChangedOption(AnalyzerABTestOptions.LastDateTimeInfoBarShown, utcNow.ToBinary());
                    AnalyzerABTestLogger.Log("InfoBarShown");

                    var infoBarService = _workspace.Services.GetRequiredService<IInfoBarService>();
                    infoBarService.ShowInfoBarInGlobalView(
                        ServicesVSResources.Try_the_preview_version_of_our_live_code_analysis_extension_which_provides_more_fixes_for_common_API_design_naming_performance_and_reliability_issues,
                        // Install link
                        new InfoBarUI(title: ServicesVSResources.Learn_more,
                                      kind: InfoBarUI.UIKind.HyperLink,
                                      action: OpenInstallHyperlink),
                        // Don't show the InfoBar again link
                        new InfoBarUI(title: ServicesVSResources.Never_show_this_again,
                                      kind: InfoBarUI.UIKind.Button,
                                      action: DoNotShowAgain));
                }
            }
        }

        private void OpenInstallHyperlink()
        {
            System.Diagnostics.Process.Start(AnalyzerVsixHyperlink);
            AnalyzerABTestLogger.Log(nameof(AnalyzerVsixHyperlink));
        }

        private void DoNotShowAgain()
        {
            _workspace.Options = _workspace.Options.WithChangedOption(AnalyzerABTestOptions.NeverShowAgain, true);
            AnalyzerABTestLogger.Log(nameof(AnalyzerABTestOptions.NeverShowAgain));
        }
    }
}
