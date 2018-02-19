// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioWorkspaceImpl : IVsSolutionWorkingFoldersEvents
    {
        void IVsSolutionWorkingFoldersEvents.OnAfterLocationChange(uint location, bool contentMoved)
        {
            if (location != (uint)__SolutionWorkingFolder.SlnWF_StatePersistence)
            {
                return;
            }

            // notify the working folder change
            GetProjectTrackerAndInitializeIfNecessary(ServiceProvider.GlobalProvider).NotifyWorkspaceHosts(
                host => (host as IVisualStudioWorkingFolder)?.OnAfterWorkingFolderChange());
        }

        void IVsSolutionWorkingFoldersEvents.OnQueryLocationChange(uint location, out bool pfCanMoveContent)
        {
            if (location != (uint)__SolutionWorkingFolder.SlnWF_StatePersistence)
            {
                pfCanMoveContent = true;
                return;
            }

            // notify the working folder change
            pfCanMoveContent = true;
            GetProjectTrackerAndInitializeIfNecessary(ServiceProvider.GlobalProvider).NotifyWorkspaceHosts(
                host => (host as IVisualStudioWorkingFolder)?.OnBeforeWorkingFolderChange());
        }
    }
}
