// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [Export(typeof(VisualStudioMetadataAsSourceFileSupportService))]
    internal sealed class VisualStudioMetadataAsSourceFileSupportService : IVsSolutionEvents
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

#pragma warning disable IDE0052 // Remove unread private members - Used to store the AdviseSolutionEvents cookie.
        private readonly uint _eventCookie;
#pragma warning restore IDE0052 // Remove unread private members

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioMetadataAsSourceFileSupportService(SVsServiceProvider serviceProvider, IMetadataAsSourceFileService metadataAsSourceFileService)
        {
            _metadataAsSourceFileService = metadataAsSourceFileService;

            var solution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out _eventCookie));
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            _metadataAsSourceFileService.CleanupGeneratedFiles();

            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
            => VSConstants.E_NOTIMPL;

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
            => VSConstants.E_NOTIMPL;

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
            => VSConstants.E_NOTIMPL;

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
            => VSConstants.E_NOTIMPL;

        public int OnBeforeCloseSolution(object pUnkReserved)
            => VSConstants.E_NOTIMPL;

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
            => VSConstants.E_NOTIMPL;

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
            => VSConstants.E_NOTIMPL;

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
            => VSConstants.E_NOTIMPL;

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
            => VSConstants.E_NOTIMPL;
    }
}
