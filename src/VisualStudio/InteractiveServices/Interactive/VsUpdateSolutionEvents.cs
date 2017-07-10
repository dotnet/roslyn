// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Interactive
{
    /// <summary>
    /// Allows us to watch solution events so we can track the build when we run the
    /// "ResetInteractiveFromProject".
    /// </summary>
    internal sealed class VsUpdateSolutionEvents : IVsUpdateSolutionEvents
    {
        private readonly TaskCompletionSource<bool> _taskSource;
        private readonly IVsSolutionBuildManager _buildManager;
        private readonly uint _cookie;

        public VsUpdateSolutionEvents(
            IVsSolutionBuildManager buildManager,
            TaskCompletionSource<bool> taskSource)
        {
            _taskSource = taskSource;
            _buildManager = buildManager;

            Marshal.ThrowExceptionForHR(
                buildManager.AdviseUpdateSolutionEvents(this, out _cookie));
        }

        public int OnActiveProjectCfgChange(IVsHierarchy hierarchy)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Begin(ref int cancelUpdate)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Done(int succeeded, int modified, int cancelCommand)
        {
            if (cancelCommand != 0)
            {
                _taskSource.SetCanceled();
            }
            else if (succeeded != 0)
            {
                _taskSource.SetResult(true);
            }
            else
            {
                _taskSource.SetCanceled();
            }

            Marshal.ThrowExceptionForHR(_buildManager.UnadviseUpdateSolutionEvents(_cookie));
            return VSConstants.S_OK;
        }

        public int UpdateSolution_StartUpdate(ref int cancelUpdate)
        {
            return VSConstants.S_OK;
        }
    }
}
