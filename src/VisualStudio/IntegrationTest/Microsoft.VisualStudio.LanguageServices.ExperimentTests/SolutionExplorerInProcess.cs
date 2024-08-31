// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    internal partial class SolutionExplorerInProcess
    {
        public async Task OpenSolutionAsync(string solutionPath, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
            dte.Solution.Open(solutionPath);
        }

        public async Task<bool> BuildSolutionAndWaitAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var buildManager = await GetRequiredGlobalServiceAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>(cancellationToken);
            using var solutionEvents = new UpdateSolutionEvents(buildManager);
            var buildCompleteTaskCompletionSource = new TaskCompletionSource<bool>();

            void HandleUpdateSolutionDone(bool buildSucceed) => buildCompleteTaskCompletionSource.SetResult(buildSucceed);
            solutionEvents.OnUpdateSolutionDone += HandleUpdateSolutionDone;
            try
            {
                ErrorHandler.ThrowOnFailure(buildManager.StartSimpleUpdateSolutionConfiguration((uint)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD, 0, 0));
                return await buildCompleteTaskCompletionSource.Task;
            }
            finally
            {
                solutionEvents.OnUpdateSolutionDone -= HandleUpdateSolutionDone;
            }
        }

        public async Task<bool> DeploySolutionAsync(bool attachingDebugger, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var buildManager = await GetRequiredGlobalServiceAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>(cancellationToken);
            using var solutionEvents = new UpdateSolutionEvents(buildManager);
            var buildCompleteTaskCompletionSource = new TaskCompletionSource<bool>();

            void HandleUpdateSolutionDone(bool buildSucceed) => buildCompleteTaskCompletionSource.SetResult(buildSucceed);
            solutionEvents.OnUpdateSolutionDone += HandleUpdateSolutionDone;
            try
            {
                var operation = attachingDebugger ? VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_LAUNCHDEBUG : VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_LAUNCH;
                ErrorHandler.ThrowOnFailure(buildManager.StartSimpleUpdateSolutionConfiguration(
                    (uint)operation, 0, 0));
                return await buildCompleteTaskCompletionSource.Task;
            }
            finally
            {
                solutionEvents.OnUpdateSolutionDone -= HandleUpdateSolutionDone;
            }
        }

        public async Task<string> GetBuildOutputContentAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var buildOutputWindowPane = await GetBuildOutputWindowPaneAsync(cancellationToken);
            var textView = (IVsTextView)buildOutputWindowPane;
            var wpfTextViewHost = await textView.GetTextViewHostAsync(JoinableTaskFactory, cancellationToken);
            return wpfTextViewHost.TextView.TextSnapshot.GetText();
        }

        private async Task<IVsOutputWindowPane> GetBuildOutputWindowPaneAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var outputWindow = await GetRequiredGlobalServiceAsync<SVsOutputWindow, IVsOutputWindow>(cancellationToken);
            ErrorHandler.ThrowOnFailure(outputWindow.GetPane(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid, out var pane));
            return pane;
        }

        internal sealed class UpdateSolutionEvents : IVsUpdateSolutionEvents, IDisposable
        {
            private uint _cookie;
            private readonly IVsSolutionBuildManager2 _solutionBuildManager;

            public event Action<bool>? OnUpdateSolutionDone;

            internal UpdateSolutionEvents(IVsSolutionBuildManager2 solutionBuildManager)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                _solutionBuildManager = solutionBuildManager;
                ErrorHandler.ThrowOnFailure(solutionBuildManager.AdviseUpdateSolutionEvents(this, out _cookie));
            }

            int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate) => VSConstants.E_NOTIMPL;
            int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.E_NOTIMPL;
            int IVsUpdateSolutionEvents.UpdateSolution_Cancel() => VSConstants.E_NOTIMPL;
            int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => VSConstants.E_NOTIMPL;

            int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
            {
                var buildSucceeded = fSucceeded == 1;
                OnUpdateSolutionDone?.Invoke(buildSucceeded);
                return VSConstants.S_OK;
            }

            void IDisposable.Dispose()
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                OnUpdateSolutionDone = null;

                if (_cookie != 0)
                {
                    var tempCookie = _cookie;
                    _cookie = 0;
                    ErrorHandler.ThrowOnFailure(_solutionBuildManager.UnadviseUpdateSolutionEvents(tempCookie));
                }
            }
        }

    }
}
