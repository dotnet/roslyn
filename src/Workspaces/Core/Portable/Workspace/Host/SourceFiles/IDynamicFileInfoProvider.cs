// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Provider for the <see cref="DynamicFileInfo"/>
    /// 
    /// implementer of this service should be pure free-thread meaning it can't switch to UI thread underneath.
    /// otherwise, we can get into dead lock if we wait for the dynamic file info from UI thread
    /// </summary>
    internal interface IDynamicFileInfoProvider
    {
        /// <summary>
        /// return <see cref="DynamicFileInfo"/> for the context given
        /// </summary>
        /// <param name="projectId"><see cref="ProjectId"/> this file belongs to</param>
        /// <param name="projectFilePath">full path to project file (ex, csproj)</param>
        /// <param name="filePath">full path to non source file (ex, cshtml)</param>
        /// <returns>null if this provider can't handle the given file</returns>
        Task<DynamicFileInfo?> GetDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken);

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
    }
}
