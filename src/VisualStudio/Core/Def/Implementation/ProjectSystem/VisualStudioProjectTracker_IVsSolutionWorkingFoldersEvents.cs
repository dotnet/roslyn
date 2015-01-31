// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Internal.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioProjectTracker : IVsSolutionWorkingFoldersEvents
    {
        void IVsSolutionWorkingFoldersEvents.OnAfterLocationChange(uint location, bool contentMoved)
        {
            if (location != (uint)__SolutionWorkingFolder.SlnWF_StatePersistence)
            {
                return;
            }

            // notify the working folder change
            NotifyWorkspaceHosts(host =>
            {
                var workingFolder = host as IVisualStudioWorkingFolder;
                if (workingFolder == null)
                {
                    return;
                }

                workingFolder.OnAfterWorkingFolderChange();
            });
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
            NotifyWorkspaceHosts(host =>
            {
                var workingFolder = host as IVisualStudioWorkingFolder;
                if (workingFolder == null)
                {
                    return;
                }

                workingFolder.OnBeforeWorkingFolderChange();
            });
        }
    }
}
