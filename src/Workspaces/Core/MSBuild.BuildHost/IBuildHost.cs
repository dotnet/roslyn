// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;

/// <summary>
/// The RPC interface implemented by this host; called via JSON-RPC.
/// </summary>
internal interface IBuildHost
{
    /// <summary>
    /// Returns whether this project's language is supported.
    /// </summary>
    Task<bool> IsProjectFileSupportedAsync(string projectFilePath, CancellationToken cancellationToken);

    Task<IRemoteProjectFile> LoadProjectFileAsync(string projectFilePath, CancellationToken cancellationToken);

    Task ShutdownAsync();
}
