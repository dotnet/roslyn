// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioWorkspaceImpl : IVsSolutionLoadEvents
    {
        int IVsSolutionLoadEvents.OnBeforeOpenSolution(string pszSolutionFilename)
        {
            GetProjectTrackerAndInitializeIfNecessary(ServiceProvider.GlobalProvider).OnBeforeOpenSolution();
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnBeforeBackgroundSolutionLoadBegins()
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            _foregroundObject.AssertIsForeground();
            GetProjectTrackerAndInitializeIfNecessary(ServiceProvider.GlobalProvider).OnBeforeLoadProjectBatch(fIsBackgroundIdleBatch);
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            _foregroundObject.AssertIsForeground();
            GetProjectTrackerAndInitializeIfNecessary(ServiceProvider.GlobalProvider).OnAfterLoadProjectBatch(fIsBackgroundIdleBatch);
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnAfterBackgroundSolutionLoadComplete()
        {
            _foregroundObject.AssertIsForeground();
            GetProjectTrackerAndInitializeIfNecessary(ServiceProvider.GlobalProvider).OnAfterBackgroundSolutionLoadComplete();
            return VSConstants.S_OK;
        }
    }
}
