// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Provides solution status
    /// </summary>
    internal interface ISolutionStatusService : IWorkspaceService
    {
        /// <summary>
        /// Wait until given solution and its children are fully loaded.
        /// </summary>
        Task WaitForAsync(Solution solution, CancellationToken cancellationToken);

        /// <summary>
        /// Wait until given project and its children are fully loaded.
        /// </summary>
        Task WaitForAsync(Project project, CancellationToken cancellationToken);

        /// <summary>
        /// Indicates whether given solution and its children are fully loaded or not. 
        /// </summary>
        Task<bool> IsFullyLoadedAsync(Solution solution, CancellationToken cancellationToken);

        /// <summary>
        /// Indicates whether given project and its children are fully loaded or not.
        /// </summary>
        Task<bool> IsFullyLoadedAsync(Project project, CancellationToken cancellationToken);
    }
}
