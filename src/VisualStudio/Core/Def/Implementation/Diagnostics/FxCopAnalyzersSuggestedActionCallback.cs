// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    [Export(typeof(ISuggestedActionCallback))]
    internal class FxCopAnalyzersSuggestedActionCallback : ForegroundThreadAffinitizedObject, ISuggestedActionCallback
    {
        private const string AnalyzerVsixHyperlink = @"https://marketplace.visualstudio.com/items?itemName=VisualStudioPlatformTeam.MicrosoftCodeAnalysis2019";
        private const string AnalyzerInstallLearnMoreHyperlink = @"https://docs.microsoft.com/visualstudio/code-quality/install-fxcop-analyzers";
        private static readonly Guid FxCopAnalyzersPackageGuid = Guid.Parse("{4A41D270-A97F-4639-A352-28732FC410E4}");

        private const string AnalyzerNuGetPackageId = @"Microsoft.CodeAnalysis.FxCopAnalyzers";
        private static readonly ImmutableHashSet<string> ChildAnalyzerNuGetPackageIds = ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "Microsoft.CodeQuality.Analyzers",
            "Microsoft.NetCore.Analyzers",
            "Microsoft.NetFramework.Analyzers");

        private readonly VisualStudioWorkspace _workspace;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IDocumentTrackingService _documentTrackingService;
        private readonly IPackageInstallerService _packageInstallerService;

        /// <summary>
        /// Tracks when the bar is shown so we don't show to the user more than once per session
        /// </summary>
        private bool _infoBarChecked = false;

        private FxCopAnalyzersInstallStatus _vsixInstallStatus = FxCopAnalyzersInstallStatus.Unknown;
        private FxCopAnalyzersInstallStatus _nugetInstallStatusForCurrentSolution = FxCopAnalyzersInstallStatus.Unknown;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FxCopAnalyzersSuggestedActionCallback(
            IThreadingContext threadingContext,
            VisualStudioWorkspace workspace,
            SVsServiceProvider serviceProvider)
            : base(threadingContext)
        {
            _workspace = workspace;
            _serviceProvider = serviceProvider;
            _documentTrackingService = workspace.Services.GetService<IDocumentTrackingService>();
            _packageInstallerService = workspace.Services.GetService<IPackageInstallerService>();

            _workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs eventArgs)
        {
            switch (eventArgs.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                    _nugetInstallStatusForCurrentSolution = FxCopAnalyzersInstallStatus.Unknown;
                    _infoBarChecked = false;
                    break;
            }
        }

        public void OnSuggestedActionExecuted(SuggestedAction action)
        {
            // We'll need to be on the UI thread for the operations.
            AssertIsForeground();

            // If the user has previously clicked don't show again, then we bail out immediately
            if (_workspace.Options.GetOption(FxCopAnalyzersInstallOptions.NeverShowAgain) ||
                _workspace.Options.GetOption(FxCopAnalyzersInstallOptions.NeverShowAgain_CodeAnalysis2017))
            {
                return;
            }

            // Only show if the VSIX and NuGet are not installed, the info bar hasn't been shown this session,
            // and the user is candidate based on light bulb usage.
            if (!_infoBarChecked &&
                !IsVsixInstalled() &&
                !IsNuGetInstalled() &&
                IsCandidate(action))
            {
                ShowInfoBarIfNecessary();
            }
        }

        private bool IsVsixInstalled()
        {
            if (_vsixInstallStatus == FxCopAnalyzersInstallStatus.Unknown)
            {
                var vsShell = _serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
                var hr = vsShell.IsPackageInstalled(FxCopAnalyzersPackageGuid, out var installed);
                if (ErrorHandler.Failed(hr))
                {
                    FatalError.ReportWithoutCrash(Marshal.GetExceptionForHR(hr));

                    // We set installed to ensure we don't go through this again next time a
                    // suggested action is called, and we don't want to continue if the shell
                    // is busted.
                    _vsixInstallStatus = FxCopAnalyzersInstallStatus.Installed;
                }
                else
                {
                    _vsixInstallStatus = installed != 0 ? FxCopAnalyzersInstallStatus.Installed : FxCopAnalyzersInstallStatus.NotInstalled;
                    FxCopAnalyzersInstallLogger.LogVsixInstallationStatus(_workspace, _vsixInstallStatus);
                }
            }

            return _vsixInstallStatus == FxCopAnalyzersInstallStatus.Installed;
        }

        private bool IsNuGetInstalled()
        {
            if (_nugetInstallStatusForCurrentSolution != FxCopAnalyzersInstallStatus.Installed)
            {
                var activeDocumentId = _documentTrackingService.TryGetActiveDocument();
                if (activeDocumentId == null)
                {
                    return false;
                }

                var document = _workspace.CurrentSolution.GetTextDocument(activeDocumentId);
                if (document == null)
                {
                    return false;
                }

                foreach (var analyzerReference in document.Project.AnalyzerReferences)
                {
                    if (ChildAnalyzerNuGetPackageIds.Contains(analyzerReference.Display))
                    {
                        // We set installed to ensure we don't go through this again next time a
                        // suggested action is called for any document in current solution.
                        _nugetInstallStatusForCurrentSolution = FxCopAnalyzersInstallStatus.Installed;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsCandidate(SuggestedAction action)
        {
            // Candidates fill the following critera:
            //     1: Are a Dotnet user (as evidenced by the fact that this code is being run)
            //     2: Have triggered a lightbulb on 3 separate days or if this is a code quality suggested action.

            // If the user hasn't met candidacy conditions, then we check them. Otherwise, proceed
            // to info bar check
            var options = _workspace.Options;
            var isCandidate = options.GetOption(FxCopAnalyzersInstallOptions.HasMetCandidacyRequirements);
            if (!isCandidate)
            {
                // We store in UTC to avoid any timezone offset weirdness
                var lastTriggeredTimeBinary = options.GetOption(FxCopAnalyzersInstallOptions.LastDateTimeUsedSuggestionAction);
                FxCopAnalyzersInstallLogger.LogCandidacyRequirementsTracking(lastTriggeredTimeBinary);

                var lastTriggeredTime = DateTime.FromBinary(lastTriggeredTimeBinary);
                var currentTime = DateTime.UtcNow;
                var span = currentTime - lastTriggeredTime;
                if (span.TotalDays >= 1)
                {
                    options = options.WithChangedOption(FxCopAnalyzersInstallOptions.LastDateTimeUsedSuggestionAction, currentTime.ToBinary());

                    var usageCount = options.GetOption(FxCopAnalyzersInstallOptions.UsedSuggestedActionCount);
                    options = options.WithChangedOption(FxCopAnalyzersInstallOptions.UsedSuggestedActionCount, ++usageCount);

                    // Candidate if user has invoked the light bulb 3 times or if this is a code quality suggested action.
                    if (usageCount >= 3 || action.IsForCodeQualityImprovement)
                    {
                        isCandidate = true;
                        options = options.WithChangedOption(FxCopAnalyzersInstallOptions.HasMetCandidacyRequirements, true);
                        FxCopAnalyzersInstallLogger.Log(nameof(FxCopAnalyzersInstallOptions.HasMetCandidacyRequirements));
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

            // We determine if the infobar has been displayed in the past 24 hours.
            // If it hasn't been displayed, then we do so now.
            var lastTimeInfoBarShown = DateTime.FromBinary(_workspace.Options.GetOption(FxCopAnalyzersInstallOptions.LastDateTimeInfoBarShown));
            var utcNow = DateTime.UtcNow;
            var timeSinceLastShown = utcNow - lastTimeInfoBarShown;

            if (timeSinceLastShown.TotalDays >= 1)
            {
                _workspace.Options = _workspace.Options.WithChangedOption(FxCopAnalyzersInstallOptions.LastDateTimeInfoBarShown, utcNow.ToBinary());
                FxCopAnalyzersInstallLogger.Log("InfoBarShown");

                var infoBarService = _workspace.Services.GetRequiredService<IInfoBarService>();
                infoBarService.ShowInfoBarInGlobalView(
                    ServicesVSResources.Install_Microsoft_recommended_Roslyn_analyzers_which_provide_additional_diagnostics_and_fixes_for_common_API_design_security_performance_and_reliability_issues,
                    GetInfoBarUIItems().ToArray());
            }

            return;

            // Local functions
            IEnumerable<InfoBarUI> GetInfoBarUIItems()
            {
                if (_packageInstallerService?.CanShowManagePackagesDialog() == true)
                {
                    // Open NuGet package manager with FxCopAnalyzers search string
                    yield return new InfoBarUI(
                        title: ServicesVSResources.Build_plus_live_analysis_NuGet_package,
                        kind: InfoBarUI.UIKind.HyperLink,
                        action: OpenNuGetPackageManagerHyperlink,
                        closeAfterAction: false);
                }

                // FxCop Analyzers VSIX install link
                yield return new InfoBarUI(
                    title: ServicesVSResources.Live_analysis_VSIX_extension,
                    kind: InfoBarUI.UIKind.HyperLink,
                    action: OpenVSIXInstallHyperlink,
                    closeAfterAction: false);

                // Documentation link about FxCopAnalyzers
                yield return new InfoBarUI(title: ServicesVSResources.Learn_more,
                    kind: InfoBarUI.UIKind.HyperLink,
                    action: OpenLearnMoreHyperlink,
                    closeAfterAction: false);

                // Don't show the InfoBar again link
                yield return new InfoBarUI(title: ServicesVSResources.Never_show_this_again,
                    kind: InfoBarUI.UIKind.Button,
                    action: DoNotShowAgain,
                    closeAfterAction: true);
            }
        }

        private void OpenNuGetPackageManagerHyperlink()
        {
            Debug.Assert(_packageInstallerService != null);
            Debug.Assert(_packageInstallerService.CanShowManagePackagesDialog());

            _packageInstallerService.ShowManagePackagesDialog(AnalyzerNuGetPackageId);
            FxCopAnalyzersInstallLogger.Log(nameof(AnalyzerNuGetPackageId));
        }

        private void OpenVSIXInstallHyperlink()
        {
            System.Diagnostics.Process.Start(AnalyzerVsixHyperlink);
            FxCopAnalyzersInstallLogger.Log(nameof(AnalyzerVsixHyperlink));
        }

        private void OpenLearnMoreHyperlink()
        {
            System.Diagnostics.Process.Start(AnalyzerInstallLearnMoreHyperlink);
            FxCopAnalyzersInstallLogger.Log(nameof(AnalyzerInstallLearnMoreHyperlink));
        }

        private void DoNotShowAgain()
        {
            _workspace.Options = _workspace.Options.WithChangedOption(FxCopAnalyzersInstallOptions.NeverShowAgain, true);
            FxCopAnalyzersInstallLogger.Log(nameof(FxCopAnalyzersInstallOptions.NeverShowAgain));
        }
    }
}
