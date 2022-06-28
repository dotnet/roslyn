// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
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

        public bool IsPrettyListingOn(string languageName)
            => _inProc.IsPrettyListingOn(languageName);

        public void SetPrettyListing(string languageName, bool value)
            => _inProc.SetPrettyListing(languageName, value);

        public void WaitForAsyncOperations(TimeSpan timeout, string featuresToWaitFor, bool waitForWorkspaceFirst = true)
            => _inProc.WaitForAsyncOperations(timeout, featuresToWaitFor, waitForWorkspaceFirst);

        public void WaitForAllAsyncOperations(TimeSpan timeout, params string[] featureNames)
            => _inProc.WaitForAllAsyncOperations(timeout, featureNames);

        public void WaitForAllAsyncOperationsOrFail(TimeSpan timeout, params string[] featureNames)
            => _inProc.WaitForAllAsyncOperationsOrFail(timeout, featureNames);

        public void CleanUpWorkspace()
            => _inProc.CleanUpWorkspace();

        public void ResetOptions()
            => _inProc.ResetOptions();

        public void CleanUpWaitingService()
            => _inProc.CleanUpWaitingService();

        public void SetImportCompletionOption(bool value)
        {
            SetGlobalOption(WellKnownGlobalOption.CompletionOptions_ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, value);
            SetGlobalOption(WellKnownGlobalOption.CompletionOptions_ShowItemsFromUnimportedNamespaces, LanguageNames.VisualBasic, value);
        }

        public void SetEnableDecompilationOption(bool value)
        {
            SetGlobalOption(WellKnownGlobalOption.MetadataAsSourceOptions_NavigateToDecompiledSources, language: null, value);
        }

        public void SetArgumentCompletionSnippetsOption(bool value)
        {
            SetGlobalOption(WellKnownGlobalOption.CompletionViewOptions_EnableArgumentCompletionSnippets, LanguageNames.CSharp, value);
            SetGlobalOption(WellKnownGlobalOption.CompletionViewOptions_EnableArgumentCompletionSnippets, LanguageNames.VisualBasic, value);
        }

        public void SetTriggerCompletionInArgumentLists(bool value)
            => SetGlobalOption(WellKnownGlobalOption.CompletionOptions_TriggerInArgumentLists, LanguageNames.CSharp, value);

        public void SetFullSolutionAnalysis(bool value)
        {
            var scope = value ? BackgroundAnalysisScope.FullSolution : BackgroundAnalysisScope.Default;
            SetGlobalOption(WellKnownGlobalOption.SolutionCrawlerOptions_BackgroundAnalysisScopeOption, LanguageNames.CSharp, scope);
            SetGlobalOption(WellKnownGlobalOption.SolutionCrawlerOptions_BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, scope);

            var compilerScope = value ? CompilerDiagnosticsScope.FullSolution : CompilerDiagnosticsScope.OpenFiles;
            SetGlobalOption(WellKnownGlobalOption.SolutionCrawlerOptions_CompilerDiagnosticsScopeOption, LanguageNames.CSharp, compilerScope);
            SetGlobalOption(WellKnownGlobalOption.SolutionCrawlerOptions_CompilerDiagnosticsScopeOption, LanguageNames.VisualBasic, compilerScope);
        }

        public void SetFileScopedNamespaces(bool value)
            => _inProc.SetFileScopedNamespaces(value);

        public void SetGlobalOption(WellKnownGlobalOption option, string? language, object? value)
            => _inProc.SetGlobalOption(option, language, value);
    }
}
