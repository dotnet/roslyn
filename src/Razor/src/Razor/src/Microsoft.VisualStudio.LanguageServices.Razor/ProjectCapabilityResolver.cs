// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.Razor.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor;

[Export(typeof(IProjectCapabilityResolver))]
internal sealed class ProjectCapabilityResolver : IProjectCapabilityResolver, IDisposable
{
    private readonly IEnumerable<IProjectCapabilityListener> _projectCapabilityListeners;
    private readonly AsyncLazy<IVsUIShellOpenDocument> _lazyVsUIShellOpenDocument;
    private readonly ILogger _logger;
    private readonly JoinableTaskFactory _jtf;
    private readonly CancellationTokenSource _disposeTokenSource;

    [ImportingConstructor]
    public ProjectCapabilityResolver(
        IVsService<SVsUIShellOpenDocument, IVsUIShellOpenDocument> vsUIShellOpenDocumentService,
        [ImportMany] IEnumerable<IProjectCapabilityListener> projectCapabilityListeners,
        ILoggerFactory loggerFactory,
        JoinableTaskContext joinableTaskContext)
    {
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
