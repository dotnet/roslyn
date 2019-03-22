// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        public string GetWorkingFolder() => _inProc.GetWorkingFolder();

        /// <summary>
        /// Waits for a WorkspaceChange event that happens after the code <paramref name="action"/> has ran. This should
        /// only be needed if there is asynchrony that is untracked by Roslyn waiters; for example, modifying files and waiting
        /// for file watchers or doing some action that triggers an asynchronous project system update.
        /// </summary>
        /// <param name="changeKind">The type of WorkspaceChange to look for.</param>
        /// <param name="action">The action to run.</param>
        public void WaitForWorkspaceChangeFromAction(WorkspaceChangeKind changeKind, int numberOfChanges, Action action)
        {
            _inProc.BeginWaitForWorkspaceChange(changeKind, numberOfChanges);
            action();
            _inProc.EndWaitForWorkspaceChange(Helper.HangMitigatingTimeout);

            // We observed the workspace change event, but it's possible that other subscriber are still processing.
            // Now wait until the workspace waiter is fully complete.
            _inProc.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }
    }
}
