// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Razor;

internal interface ILspEditorFeatureDetector
{
    /// <summary>
    /// Determines whether the LSP editor is supported by the given document.
    /// </summary>
    bool IsLspEditorSupported(string documentFilePath);

    /// <summary>
    /// Determines whether the project containing the given document is a .NET Core project.
    /// </summary>
    CapabilityCheckResult IsDotNetCoreProject(string documentFilePath);

    /// <summary>
    /// A remote client is a LiveShare guest or a Codespaces instance
    /// </summary>
    bool IsRemoteClient();

    bool IsLiveShareHost();
}
