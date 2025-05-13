// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Interactive;

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
        => VSConstants.S_OK;

    public int UpdateSolution_Begin(ref int cancelUpdate)
        => VSConstants.S_OK;

    public int UpdateSolution_Cancel()
        => VSConstants.S_OK;

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
        => VSConstants.S_OK;
}
