// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        public bool IsUseSuggestionModeOn()
            => _inProc.IsUseSuggestionModeOn();

        public void SetUseSuggestionMode(bool value)
            => _inProc.SetUseSuggestionMode(value);

        public void SetOptionInfer(bool value)
            => _inProc.SetOptionInfer(value);

        public void SetPersistenceOption(bool value)
            => SetPerLanguageOption("Enabled", "FeatureManager/Persistence", null, value ? "true" : "false");

        public bool IsPrettyListingOn(string languageName)
            => _inProc.IsPrettyListingOn(languageName);

        public void SetPrettyListing(string languageName, bool value)
            => _inProc.SetPrettyListing(languageName, value);

        public void SetPerLanguageOption(string optionName, string feature, string language, string value)
            => _inProc.SetPerLanguageOption(optionName, feature, language, value);

        public void WaitForAsyncOperations(string featuresToWaitFor, bool waitForWorkspaceFirst = true)
            => _inProc.WaitForAsyncOperations(featuresToWaitFor, waitForWorkspaceFirst);

        public void WaitForAllAsyncOperations()
            => _inProc.WaitForAllAsyncOperations();

        public void CleanUpWorkspace()
            => _inProc.CleanUpWorkspace();

        public void CleanUpWaitingService()
            => _inProc.CleanUpWaitingService();

        public void EnableQuickInfo(bool value)
            => _inProc.EnableQuickInfo(value);
    }
}
