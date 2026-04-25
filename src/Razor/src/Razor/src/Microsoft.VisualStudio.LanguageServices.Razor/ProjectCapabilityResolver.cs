// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.Razor.Extensions;
using Microsoft.VisualStudio.Razor.LiveShare;
using Microsoft.VisualStudio.Razor.LiveShare.Guest;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor;

[Export(typeof(IProjectCapabilityResolver))]
internal sealed class ProjectCapabilityResolver : IProjectCapabilityResolver, IDisposable
{
    private readonly ILiveShareSessionAccessor _liveShareSessionAccessor;
    private readonly IEnumerable<IProjectCapabilityListener> _projectCapabilityListeners;
    private readonly AsyncLazy<IVsUIShellOpenDocument> _lazyVsUIShellOpenDocument;
    private readonly ILogger _logger;
    private readonly JoinableTaskFactory _jtf;
    private readonly CancellationTokenSource _disposeTokenSource;

    [ImportingConstructor]
    public ProjectCapabilityResolver(
        ILiveShareSessionAccessor liveShareSessionAccessor,
        IVsService<SVsUIShellOpenDocument, IVsUIShellOpenDocument> vsUIShellOpenDocumentService,
        [ImportMany] IEnumerable<IProjectCapabilityListener> projectCapabilityListeners,
        ILoggerFactory loggerFactory,
        JoinableTaskContext joinableTaskContext)
    {
        _liveShareSessionAccessor = liveShareSessionAccessor;
        _projectCapabilityListeners = projectCapabilityListeners;
        _jtf = joinableTaskContext.Factory;
        _logger = loggerFactory.GetOrCreateLogger<ProjectCapabilityResolver>();
        _disposeTokenSource = new();

        // IVsService<,> doesn't provide a synchronous GetValue(...) method, so we wrap it in an AsyncLazy<>.
        _lazyVsUIShellOpenDocument = new(
            () => vsUIShellOpenDocumentService.GetValueAsync(_disposeTokenSource.Token),
            _jtf);
    }

    public void Dispose()
    {
        if (_disposeTokenSource.IsCancellationRequested)
        {
            return;
        }

        _disposeTokenSource.Cancel();
        _disposeTokenSource.Dispose();
    }

    public CapabilityCheckResult CheckCapability(string capability, string documentFilePath)
    {
        // If a LiveShare is currently active, we call into the host to resolve project capabilities.
        // Otherwise, we use the project that contains documentFilePath to resolve capabilities.

        return _liveShareSessionAccessor.IsGuestSessionActive
            ? LiveShareHostHasCapability(capability, documentFilePath)
            : ContainingProjectHasCapability(capability, documentFilePath);
    }

    private CapabilityCheckResult LiveShareHostHasCapability(string capability, string documentFilePath)
    {
        Debug.Assert(_liveShareSessionAccessor.IsGuestSessionActive);

        // Using JTF.Run(...) here isn't great, but this is how Razor's LiveShare implementation has
        // always worked. It won't be called unless a LiveShare collaboration session is active.
        return _jtf.Run(() => LiveShareHostHasCapabilityAsync(capability, documentFilePath, _disposeTokenSource.Token));

        async Task<CapabilityCheckResult> LiveShareHostHasCapabilityAsync(string capability, string documentFilePath, CancellationToken cancellationToken)
        {
            // On a guest box. The project hierarchy is not fully populated. We need to ask the host machine
            // questions about hierarchy capabilities.

            var session = _liveShareSessionAccessor.Session.AssumeNotNull();

            var remoteHierarchyService = await session
                .GetRemoteServiceAsync<IRemoteHierarchyService>(nameof(IRemoteHierarchyService), cancellationToken)
                .ConfigureAwait(false);

            var documentFilePathUri = session.ConvertLocalPathToSharedUri(documentFilePath);

            var isMatch = await remoteHierarchyService
                .HasCapabilityAsync(documentFilePathUri, capability, cancellationToken)
                .ConfigureAwait(false);

            return new(IsInProject: true, HasCapability: isMatch);
        }
    }

    private CapabilityCheckResult ContainingProjectHasCapability(string capability, string documentFilePath)
    {
        // This method is only ever called by our IFilePathToContentTypeProvider.TryGetContentTypeForFilePath(...) implementations.
        // We call AsyncLazy<T>.GetValue() below to get the value. If the work hasn't yet completed, we guard against a hidden
        // JTF.Run(...) on a background thread by asserting the UI thread.

        _jtf.AssertUIThread();

        var vsUIShellOpenDocument = _lazyVsUIShellOpenDocument.GetValue(_disposeTokenSource.Token);

        var result = vsUIShellOpenDocument.IsDocumentInAProject(documentFilePath, out var vsHierarchy, out _, out _, out var docInProj);

        if (!ErrorHandler.Succeeded(result))
        {
            _logger.LogWarning($"Project does not support LSP Editor because {nameof(IVsUIShellOpenDocument.IsDocumentInAProject)} failed with error code: {result:x8}");
            return new(IsInProject: false, HasCapability: false);
        }

        // vsHierarchy can be null here if the document is not included in a project.
        // In this scenario, the IVsUIShellOpenDocument.IsDocumentInAProject(..., ..., ..., ..., out int pDocInProj) call succeeds,
        // but pDocInProj == __VSDOCINPROJECT.DOCINPROJ_DocNotInProject.
        if (vsHierarchy is null)
        {
            _logger.LogWarning($"LSP Editor is not supported for file because it is not in a project: {documentFilePath}");
            return new(IsInProject: false, HasCapability: false);
        }

        if (((__VSDOCINPROJECT)docInProj) != __VSDOCINPROJECT.DOCINPROJ_DocInProject)
        {
            _logger.LogWarning($"LSP Editor is not supported for file because it is not in a project: {documentFilePath}");
            return new(IsInProject: false, HasCapability: false);
        }

        var isMatch = false;
        try
        {
            isMatch = vsHierarchy.IsCapabilityMatch(capability);

            if (vsHierarchy.GetProjectFilePath(_jtf) is { } projectFilePath)
            {
                foreach (var listener in _projectCapabilityListeners)
                {
                    // Notify all listeners of the capability match.
                    listener.OnProjectCapabilityMatched(projectFilePath, capability, isMatch);
                }
            }
        }
        catch (NotSupportedException)
        {
            // IsCapabilityMatch throws a NotSupportedException if it can't create a
            // BooleanSymbolExpressionEvaluator COM object
        }
        catch (ObjectDisposedException)
        {
            // IsCapabilityMatch throws an ObjectDisposedException if the underlying hierarchy has been disposed
        }

        return new(IsInProject: true, HasCapability: isMatch);
    }
}
