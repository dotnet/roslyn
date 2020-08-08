// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class VisualStudioWorkspace_OutOfProc : OutOfProcComponent
    {
        private readonly VisualStudioWorkspace_InProc _inProc;

        internal VisualStudioWorkspace_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<VisualStudioWorkspace_InProc>(visualStudioInstance);
        }
        public void SetOptionInfer(string projectName, bool value)
        {
            _inProc.SetOptionInfer(projectName, value);
            WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        public void SetPersistenceOption(bool value)
            => SetOption("Enabled", PersistentStorageOptions.OptionName, value);

        public bool IsPrettyListingOn(string languageName)
            => _inProc.IsPrettyListingOn(languageName);

        public void SetPrettyListing(string languageName, bool value)
            => _inProc.SetPrettyListing(languageName, value);

        public void SetPerLanguageOption(string optionName, string feature, string language, object value)
            => _inProc.SetPerLanguageOption(optionName, feature, language, value);

        public void SetOption(string optionName, string feature, object value)
            => _inProc.SetOption(optionName, feature, value);

        public void WaitForAsyncOperations(TimeSpan timeout, string featuresToWaitFor, bool waitForWorkspaceFirst = true)
            => _inProc.WaitForAsyncOperations(timeout, featuresToWaitFor, waitForWorkspaceFirst);

        public void WaitForAllAsyncOperations(TimeSpan timeout, params string[] featureNames)
            => _inProc.WaitForAllAsyncOperations(timeout, featureNames);

        public void CleanUpWorkspace()
            => _inProc.CleanUpWorkspace();

        public void CleanUpWaitingService()
            => _inProc.CleanUpWaitingService();

        public void SetQuickInfo(bool value)
            => _inProc.EnableQuickInfo(value);

        public void SetImportCompletionOption(bool value)
        {
            SetPerLanguageOption(
                optionName: "ShowItemsFromUnimportedNamespaces",
                feature: "CompletionOptions",
                language: LanguageNames.CSharp,
                value: value);

            SetPerLanguageOption(
                optionName: "ShowItemsFromUnimportedNamespaces",
                feature: "CompletionOptions",
                language: LanguageNames.VisualBasic,
                value: value);
        }

        public void SetTriggerCompletionInArgumentLists(bool value)
        {
            SetPerLanguageOption(
                optionName: CompletionOptions.TriggerInArgumentLists.Name,
                feature: CompletionOptions.TriggerInArgumentLists.Feature,
                language: LanguageNames.CSharp,
                value: value);
        }

        public void SetFullSolutionAnalysis(bool value)
        {
            SetPerLanguageOption(
                optionName: SolutionCrawlerOptions.BackgroundAnalysisScopeOption.Name,
                feature: SolutionCrawlerOptions.BackgroundAnalysisScopeOption.Feature,
                language: LanguageNames.CSharp,
                value: value ? BackgroundAnalysisScope.FullSolution : BackgroundAnalysisScope.Default);

            SetPerLanguageOption(
                optionName: SolutionCrawlerOptions.BackgroundAnalysisScopeOption.Name,
                feature: SolutionCrawlerOptions.BackgroundAnalysisScopeOption.Feature,
                language: LanguageNames.VisualBasic,
                value: value ? BackgroundAnalysisScope.FullSolution : BackgroundAnalysisScope.Default);
        }

        public void SetFeatureOption(string feature, string optionName, string language, string valueString)
            => _inProc.SetFeatureOption(feature, optionName, language, valueString);

        public string GetWorkingFolder() => _inProc.GetWorkingFolder();
    }
}
