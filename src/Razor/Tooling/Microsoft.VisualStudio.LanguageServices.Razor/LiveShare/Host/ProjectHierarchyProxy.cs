// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.LiveShare.Host;

internal class ProjectHierarchyProxy(
    CollaborationSession session,
    IServiceProvider serviceProvider,
    JoinableTaskFactory jtf) : IProjectHierarchyProxy, ICollaborationService
{
    private readonly CollaborationSession _session = session;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly JoinableTaskFactory _jtf = jtf;

    private IVsUIShellOpenDocument? _openDocumentShell;

    public async Task<Uri?> GetProjectPathAsync(Uri documentFilePath, CancellationToken cancellationToken)
    {
        if (documentFilePath is null)
        {
            throw new ArgumentNullException(nameof(documentFilePath));
        }

        await _jtf.SwitchToMainThreadAsync(cancellationToken);

        _openDocumentShell ??= _serviceProvider.GetService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
        Assumes.Present(_openDocumentShell);

        var hostDocumentFilePath = _session.ConvertSharedUriToLocalPath(documentFilePath);
        var hr = _openDocumentShell.IsDocumentInAProject(hostDocumentFilePath, out var hierarchy, out _, out _, out _);
        if (ErrorHandler.Succeeded(hr) && hierarchy != null)
        {
            ErrorHandler.ThrowOnFailure(((IVsProject)hierarchy).GetMkDocument((uint)VSConstants.VSITEMID.Root, out var path), VSConstants.E_NOTIMPL);

            return _session.ConvertLocalPathToSharedUri(path);
        }

        return null;
    }
}
