// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Provides workspace status
    /// 
    /// this is an work in-progress interface, subject to be changed as we work on prototype.
    /// 
    /// it can completely removed at the end or new APIs can added and removed as prototype going on
    /// no one except one in the prototype group should use this interface.
    /// 
    /// tracking issue - https://github.com/dotnet/roslyn/issues/34415
    /// </summary>
    internal interface IWorkspaceStatusService : IWorkspaceService
    {
        /// <summary>
        /// Indicate that status has changed
        /// 
        /// event argument, true, means solution is fully loaded.
        /// 
        /// but right now, bool doesn't mean much but having it since platform API we decide to start with bool rather than
        /// more richer information
        /// </summary>
        event EventHandler<bool> StatusChanged;

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
