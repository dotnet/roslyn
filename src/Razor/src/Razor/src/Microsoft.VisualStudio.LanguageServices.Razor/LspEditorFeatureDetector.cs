// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Razor.Logging;

namespace Microsoft.VisualStudio.Razor;

[Export(typeof(ILspEditorFeatureDetector))]
[method: ImportingConstructor]
internal sealed class LspEditorFeatureDetector(
    IProjectCapabilityResolver projectCapabilityResolver,
    RazorActivityLog activityLog) : ILspEditorFeatureDetector
{
    private readonly IProjectCapabilityResolver _projectCapabilityResolver = projectCapabilityResolver;
    private readonly RazorActivityLog _activityLog = activityLog;

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
}
