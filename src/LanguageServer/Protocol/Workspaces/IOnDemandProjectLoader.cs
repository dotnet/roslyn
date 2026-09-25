// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal interface IOnDemandProjectLoader : ILspService
{
    /// <summary>
    /// Returns whether this demand discovered project candidates (or failed before ruling them out).
    /// A completed task does not imply that no projects were loaded.
    /// </summary>
    Task<bool> StartLoadingAsync(DocumentUri uri);

    /// <summary>
    /// Captures active on-demand discovery and its project loads when on-demand loading is enabled.
    /// </summary>
    /// <remarks>
    /// Await inner <see cref="Task"/> outside dispatch.
    /// </remarks>
    ValueTask<Task> CaptureWorkspaceLoadSnapshotAsync();
}
