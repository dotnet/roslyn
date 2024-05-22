// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

[Export(typeof(VisualStudioMetadataAsSourceFileSupportService))]
internal sealed class VisualStudioMetadataAsSourceFileSupportService : IVsSolutionEvents
{
    private readonly IThreadingContext _threadingContext;
    private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioMetadataAsSourceFileSupportService(
        IThreadingContext threadingContext,
        IMetadataAsSourceFileService metadataAsSourceFileService)
    {
        _threadingContext = threadingContext;
        _metadataAsSourceFileService = metadataAsSourceFileService;
    }

    public async Task InitializeAsync(IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var solution = await serviceProvider.GetServiceAsync<SVsSolution, IVsSolution>(_threadingContext.JoinableTaskFactory).ConfigureAwait(false);
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        // Intentionally ignore the event-cookie we get back out.  We never stop listening to solution events.
        ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out _));
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
