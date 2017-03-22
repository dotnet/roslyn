// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Host;
using System;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioWorkspaceImpl : IVsSolutionEvents
    {
        private IVsSolution _vsSolution;
        private uint? _solutionEventsCookie;

        public void AdviseSolutionEvents(IVsSolution solution)
        {
            _vsSolution = solution;
            _vsSolution.AdviseSolutionEvents(this, out var solutionEventsCookie);
            _solutionEventsCookie = solutionEventsCookie;
        }

        public void UnadviseSolutionEvents()
        {
            if (_solutionEventsCookie.HasValue)
            {
                _vsSolution.UnadviseSolutionEvents(_solutionEventsCookie.Value);
                _solutionEventsCookie = null;
            }
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            _foregroundObject.Value.AssertIsForeground();

            if (IsDeferredSolutionLoadEnabled(Shell.ServiceProvider.GlobalProvider))
            {
                GetProjectTrackerAndInitializeIfNecessary(Shell.ServiceProvider.GlobalProvider).LoadSolutionFromMSBuildAsync().FireAndForget();
            }

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            DeferredState?.ProjectTracker.OnBeforeCloseSolution();
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            DeferredState?.ProjectTracker.OnAfterCloseSolution();
            return VSConstants.S_OK;
        }

        internal static bool IsDeferredSolutionLoadEnabled(IServiceProvider serviceProvider)
        {
            // NOTE: It is expected that the "as" will fail on Dev14, as IVsSolution7 was
            // introduced in Dev15.  Be sure to handle the null result here.
            var solution7 = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution7;
            return solution7?.IsSolutionLoadDeferred() == true;
        }

    }
}
