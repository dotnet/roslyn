// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.SourceGeneratorTelemetry;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Exports a <see cref="ISourceGeneratorTelemetryCollectorWorkspaceService"/> which is watched across all workspaces. This lets us collect
    /// statistics for all workspaces (including things like interactive, preview, etc.) so we can get the overall counts to report.
    /// </summary>
    [Export]
    [ExportWorkspaceServiceFactory(typeof(ISourceGeneratorTelemetryCollectorWorkspaceService)), Shared]
    internal class VisualStudioSourceGeneratorTelemetryCollectorWorkspaceServiceFactory : IWorkspaceServiceFactory, IVsSolutionEvents
    {
        /// <summary>
        /// The collector that's used to collect all the telemetry for operations within <see
        /// cref="VisualStudioWorkspace"/>. We'll report this when the solution is closed, so the telemetry is linked to
        /// that.
        /// </summary>
        private readonly SourceGeneratorTelemetryCollectorWorkspaceService _visualStudioWorkspaceInstance = new();

        /// <summary>
        /// The collector used to collect telemetry for any other workspaces that might be created; we'll report this at
        /// the end of the session since nothing here is necessarily linked to a specific solution. The expectation is
        /// this may be empty for many/most sessions, but we don't want a hole in our reporting and discover that the
        /// hard way.
        /// </summary>
        private readonly SourceGeneratorTelemetryCollectorWorkspaceService _otherWorkspacesInstance = new();

        private readonly IThreadingContext _threadingContext;
        private readonly IAsyncServiceProvider _serviceProvider;
        private volatile int _subscribedToSolutionEvents;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioSourceGeneratorTelemetryCollectorWorkspaceServiceFactory(IThreadingContext threadingContext, SVsServiceProvider serviceProvider)
        {
            _threadingContext = threadingContext;
            _serviceProvider = (IAsyncServiceProvider)serviceProvider;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            // We will record all generators for the main workspace in one bucket, and any other generators running in other
            // workspaces (interactive, for example) will be put in a different bucket. This allows us to report the telemetry
            // from the primary workspace on solution closed, while not letting the unrelated runs pollute those numbers.
            if (workspaceServices.Workspace is VisualStudioWorkspace)
            {
                EnsureSubscribedToSolutionEvents();
                return _visualStudioWorkspaceInstance;
            }
            else
            {
                return _otherWorkspacesInstance;
            }
        }

        private void EnsureSubscribedToSolutionEvents()
        {
            if (Interlocked.CompareExchange(ref _subscribedToSolutionEvents, 1, 0) == 0)
            {
                Task.Run(async () =>
                {
                    var shellService = await _serviceProvider.GetServiceAsync<SVsSolution, IVsSolution>(_threadingContext.JoinableTaskFactory).ConfigureAwait(true);
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(_threadingContext.DisposalToken);
                    shellService.AdviseSolutionEvents(this, out _);
                }, _threadingContext.DisposalToken);
            }
        }

        public void ReportOtherWorkspaceTelemetry()
        {
            _otherWorkspacesInstance.ReportStatisticsAndClear(FunctionId.SourceGenerator_OtherWorkspaceSessionStatistics);
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => VSConstants.E_NOTIMPL;
        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.E_NOTIMPL;
        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.E_NOTIMPL;
        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.E_NOTIMPL;
        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.E_NOTIMPL;
        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.E_NOTIMPL;
        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution) => VSConstants.E_NOTIMPL;
        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.E_NOTIMPL;

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            // Report the telemetry now before the solution is closed; since this will be reported per solution session ID, it means
            // we can distinguish how many solutions have generators versus just overall sessions.
            _visualStudioWorkspaceInstance.ReportStatisticsAndClear(FunctionId.SourceGenerator_SolutionStatistics);

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved) => VSConstants.E_NOTIMPL;
    }
}
