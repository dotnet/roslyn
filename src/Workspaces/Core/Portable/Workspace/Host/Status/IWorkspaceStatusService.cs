// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Provides workspace status
    /// </summary>
    internal interface IWorkspaceStatusService : IWorkspaceService
    {
        /// <summary>
        /// Wait until workspace is fully loaded
        /// </summary>
        Task WaitUntilFullyLoadedAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Indicates whether workspace is fully loaded
        /// </summary>
        Task<bool> IsFullyLoadedAsync(CancellationToken cancellationToken);
    }
}
