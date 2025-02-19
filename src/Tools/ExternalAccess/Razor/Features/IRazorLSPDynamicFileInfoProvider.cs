// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

internal interface IRazorLSPDynamicFileInfoProvider
{
    /// <summary>
    /// return <see cref="DynamicFileInfo"/> for the context given
    /// </summary>
    /// <param name="projectId"><see cref="ProjectId"/> this file belongs to</param>
    /// <param name="projectFilePath">full path to project file (ex, csproj)</param>
    /// <param name="filePath">full path to non source file (ex, cshtml)</param>
    Task<IRazorDynamicFileInfo?> GetDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken);

    /// <summary>
    /// let provider know certain file has been removed
    /// </summary>
    /// <param name="projectId"><see cref="ProjectId"/> this file belongs to</param>
    /// <param name="projectFilePath">full path to project file (ex, csproj)</param>
    /// <param name="filePath">full path to non source file (ex, cshtml)</param>
    Task RemoveDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken);

    /// <summary>
    /// indicate content of a file has updated. the event argument "string" should be same as "filepath" given to <see cref="GetDynamicFileInfoAsync(ProjectId, string, string, CancellationToken)"/>
    /// </summary>
    event EventHandler<string> Updated;

    /// <summary>
    /// Ensures the dynamic file info provider is initialized and can send messages with the <see cref="IRazorClientLanguageServerManager"/>. as needed
    /// </summary>
    public Task EnsureInitializedAsync(Func<CancellationToken, Task<IRazorClientLanguageServerManager>> getLanguageServerManager, CancellationToken cancellationToken);
}
