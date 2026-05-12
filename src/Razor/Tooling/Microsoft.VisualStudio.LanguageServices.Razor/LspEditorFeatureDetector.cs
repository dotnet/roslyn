// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.VisualStudio.Razor.Logging;

namespace Microsoft.VisualStudio.Razor;

[Export(typeof(ILspEditorFeatureDetector))]
[method: ImportingConstructor]
internal sealed class LspEditorFeatureDetector(
    IUIContextService uiContextService,
    IProjectCapabilityResolver projectCapabilityResolver,
    RazorActivityLog activityLog) : ILspEditorFeatureDetector, IDisposable
{
    private readonly IUIContextService _uiContextService = uiContextService;
    private readonly IProjectCapabilityResolver _projectCapabilityResolver = projectCapabilityResolver;
    private readonly RazorActivityLog _activityLog = activityLog;
    private readonly CancellationTokenSource _disposeTokenSource = new();

    public void Dispose()
    {
        if (_disposeTokenSource.IsCancellationRequested)
        {
            return;
        }

        _disposeTokenSource.Cancel();
        _disposeTokenSource.Dispose();
    }

    public bool IsLspEditorSupported(string documentFilePath)
    {
        // .NET Framework projects don't support the LSP Razor editor.
        if (!IsDotNetCoreProject(documentFilePath).HasCapability)
        {
            _activityLog.LogInfo($"'{documentFilePath}' does not support the LSP editor because it is not associated with the '{WellKnownProjectCapabilities.DotNetCoreCSharp}' capability.");
            return false;
        }

        _activityLog.LogInfo($"LSP editor is supported for '{documentFilePath}'.");
        return true;
    }

    public CapabilityCheckResult IsDotNetCoreProject(string documentFilePath)
        => _projectCapabilityResolver.CheckCapability(WellKnownProjectCapabilities.DotNetCoreCSharp, documentFilePath);

    public bool IsRemoteClient()
        => _uiContextService.IsActive(Guids.LiveShareGuestUIContextGuid) ||
           _uiContextService.IsActive(VSConstants.UICONTEXT.CloudEnvironmentConnected_guid);

    public bool IsLiveShareHost()
        => _uiContextService.IsActive(Guids.LiveShareHostUIContextGuid);
}
