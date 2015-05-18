// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioProjectTracker : IVsSolutionLoadEvents
    {
        int IVsSolutionLoadEvents.OnBeforeOpenSolution(string pszSolutionFilename)
        {
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
            _projectsLoadedThisBatch.Clear();

            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            if (!fIsBackgroundIdleBatch)
            {
                // This batch was loaded eagerly. This might be because the user is force expanding the projects in the
                // Solution Explorer, or they had some files open in an .suo we need to push.
                StartPushingToWorkspaceAndNotifyOfOpenDocuments(_projectsLoadedThisBatch);
            }

            _projectsLoadedThisBatch.Clear();

            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnAfterBackgroundSolutionLoadComplete()
        {
            // We are now completely done, so let's simply ensure all projects are added.
            StartPushingToWorkspaceAndNotifyOfOpenDocuments(_projectMap.Values);

            // Also, all remaining project adds need to immediately pushed as well, since we're now "interactive"
            _solutionLoadComplete = true;

            // Check that the set of analyzers is complete and consistent.
            GetAnalyzerDependencyCheckingService()?.CheckForConflictsAsync();

            return VSConstants.S_OK;
        }

        private AnalyzerDependencyCheckingService GetAnalyzerDependencyCheckingService()
        {
            var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));

            return componentModel.GetService<AnalyzerDependencyCheckingService>();
        }
    }
}
