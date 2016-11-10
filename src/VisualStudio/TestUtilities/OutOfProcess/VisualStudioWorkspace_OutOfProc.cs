// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities.InProcess;

namespace Roslyn.VisualStudio.Test.Utilities.OutOfProcess
{
    public class VisualStudioWorkspace_OutOfProc : OutOfProcComponent
    {
        private readonly VisualStudioWorkspace_InProc _inProc;

        internal VisualStudioWorkspace_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            this._inProc = CreateInProcComponent<VisualStudioWorkspace_InProc>(visualStudioInstance);
        }

        public bool IsUseSuggestionModeOn()
        {
            return _inProc.IsUseSuggestionModeOn();
        }

        public void SetUseSuggestionMode(bool value)
        {
            _inProc.SetUseSuggestionMode(value);
        }

        public bool IsPrettyListingOn(string languageName)
        {
            return _inProc.IsPrettyListingOn(languageName);
        }

        public void SetPrettyListing(string languageName, bool value)
        {
            _inProc.SetPrettyListing(languageName, value);
        }

        public void WaitForAsyncOperations(string featuresToWaitFor, bool waitForWorkspaceFirst = true)
        {
            _inProc.WaitForAsyncOperations(featuresToWaitFor, waitForWorkspaceFirst);
        }

        public void WaitForAllAsyncOperations()
        {
            _inProc.WaitForAllAsyncOperations();
        }

        public void CleanUpWorkspace()
        {
            _inProc.CleanUpWorkspace();
        }

        public void CleanUpWaitingService()
        {
            _inProc.CleanUpWaitingService();
        }
    }
}
