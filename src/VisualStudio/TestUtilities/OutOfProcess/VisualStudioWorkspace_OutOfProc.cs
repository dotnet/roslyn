// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities.InProcess;

namespace Roslyn.VisualStudio.Test.Utilities.OutOfProcess
{
    public class VisualStudioWorkspace_OutOfProc : OutOfProcComponent<VisualStudioWorkspace_InProc>
    {
        internal VisualStudioWorkspace_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
        }

        public bool UseSuggestionMode
        {
            get
            {
                return InProc.UseSuggestionMode;
            }

            set
            {
                InProc.UseSuggestionMode = value;
            }
        }

        public void WaitForAsyncOperations(string featuresToWaitFor, bool waitForWorkspaceFirst = true)
        {
            InProc.WaitForAsyncOperations(featuresToWaitFor, waitForWorkspaceFirst);
        }

        public void WaitForAllAsyncOperations()
        {
            InProc.WaitForAllAsyncOperations();
        }

        public void CleanUpWorkspace()
        {
            InProc.CleanUpWorkspace();
        }

        public void CleanUpWaitingService()
        {
            InProc.CleanUpWaitingService();
        }
    }
}
