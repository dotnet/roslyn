// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class VisualStudioWorkspace_OutOfProc : OutOfProcComponent
    {
        private readonly VisualStudioWorkspace_InProc _inProc;
        private readonly VisualStudioInstance _instance;

        internal VisualStudioWorkspace_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _instance = visualStudioInstance;
            _inProc = CreateInProcComponent<VisualStudioWorkspace_InProc>(visualStudioInstance);
        }

        public bool IsUseSuggestionModeOn()
            => _inProc.IsUseSuggestionModeOn();

        public void SetUseSuggestionMode(bool value)
            => _inProc.SetUseSuggestionMode(value);

        public void SetOptionInfer(string projectName, bool value)
        {
            _inProc.SetOptionInfer(projectName, value);
            WaitForAsyncOperations(FeatureAttribute.Workspace);
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

        public void WaitForAsyncOperations(string featuresToWaitFor, bool waitForWorkspaceFirst = true)
            => _inProc.WaitForAsyncOperations(featuresToWaitFor, waitForWorkspaceFirst);

        public void WaitForAllAsyncOperations(params string[] featureNames)
            => _inProc.WaitForAllAsyncOperations(featureNames);

        public void CleanUpWorkspace()
            => _inProc.CleanUpWorkspace();

        public void CleanUpWaitingService()
            => _inProc.CleanUpWaitingService();

        public void SetQuickInfo(bool value)
            => _inProc.EnableQuickInfo(value);

        public void SetFullSolutionAnalysis(bool value)
        {
            SetPerLanguageOption(
                optionName: "Closed File Diagnostic",
                feature: "ServiceFeaturesOnOff",
                language: LanguageNames.CSharp,
                value: value ? "true" : "false");

            SetPerLanguageOption(
                optionName: "Closed File Diagnostic",
                feature: "ServiceFeaturesOnOff",
                language: LanguageNames.VisualBasic,
                value: value ? "true" : "false");
        }

        public void SetFeatureOption(string feature, string optionName, string language, string valueString)
            => _inProc.SetFeatureOption(feature, optionName, language, valueString);
    }
}
