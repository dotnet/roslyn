// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [Export(typeof(VisualStudioMetadataAsSourceFileSupportService))]
    internal sealed class VisualStudioMetadataAsSourceFileSupportService : IVsSolutionEvents
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
        private readonly uint _eventCookie;

        [ImportingConstructor]
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
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }
    }
}
