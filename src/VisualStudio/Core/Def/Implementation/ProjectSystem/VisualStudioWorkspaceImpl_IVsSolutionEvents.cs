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
            if (_solutionEventsCookie == null)
            {
                _vsSolution.AdviseSolutionEvents(this, out var solutionEventsCookie);
                _solutionEventsCookie = solutionEventsCookie;
            }
        }

        public void UnadviseSolutionEvents()
        {
            if (_solutionEventsCookie != null)
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

        /// <summary>
        /// Returns whether the solution overall has Lightweight solution load enabled,  either
        /// through the global option in Tools\Options, or the .suo specific option.
        /// 
        /// NOTE: Does *NOT* mean that all projects in the solution are deferred.  Project types
        /// can opt out.  Use <see cref="IVsSolution7.IsDeferredProjectLoadAllowed(string)"/> to
        /// see if a specific project can be deferred.
        /// </summary>
        internal static bool IsDeferredSolutionLoadEnabled(IServiceProvider serviceProvider)
        {
            var solution7 = (IVsSolution7)serviceProvider.GetService(typeof(SVsSolution));
            return solution7.IsSolutionLoadDeferred();
        }
    }
}
