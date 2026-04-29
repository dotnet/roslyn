// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.VisualStudio.LiveShare;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.LiveShare;

internal sealed class RemoteHierarchyService(
    CollaborationSession session,
    IVsService<SVsUIShellOpenDocument, IVsUIShellOpenDocument> vsUIShellOpenDocumentService,
    JoinableTaskFactory jtf)
    : IRemoteHierarchyService
{
    private readonly IVsService<SVsUIShellOpenDocument, IVsUIShellOpenDocument> _vsUIShellOpenDocumentService = vsUIShellOpenDocumentService;
    private readonly CollaborationSession _session = session;
    private readonly JoinableTaskFactory _jtf = jtf;

    public async Task<bool> HasCapabilityAsync(Uri pathOfFileInProject, string capability, CancellationToken cancellationToken)
    {
        ArgHelper.ThrowIfNull(pathOfFileInProject);
        ArgHelper.ThrowIfNull(capability);

        var vsUIShellHostDocument = await _vsUIShellOpenDocumentService.GetValueOrNullAsync(cancellationToken);
        if (vsUIShellHostDocument is null)
        {
            return false;
        }

        var hostPathOfFileInProject = _session.ConvertSharedUriToLocalPath(pathOfFileInProject);

        await _jtf.SwitchToMainThreadAsync(cancellationToken);

        var hr = vsUIShellHostDocument.IsDocumentInAProject(hostPathOfFileInProject, out var hierarchy, out _, out _, out _);
        if (!ErrorHandler.Succeeded(hr) || hierarchy is null)
        {
            return false;
        }

        try
        {
            return hierarchy.IsCapabilityMatch(capability);
        }
        catch (NotSupportedException)
        {
            // IsCapabilityMatch throws a NotSupportedException if it can't create a
            // BooleanSymbolExpressionEvaluator COM object
        }
        catch (ObjectDisposedException)
        {
            // IsCapabilityMatch throws an ObjectDisposedException if the underlying
            // hierarchy has been disposed (Bug 253462)
        }

        return false;
    }
}
