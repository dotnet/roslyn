// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host;

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
    /// </summary>
    event EventHandler StatusChanged;

    /// <summary>
    /// Wait until workspace is fully loaded
    /// 
    /// unfortunately, some hosts, such as VS, use services (ex, IVsOperationProgressStatusService) that require UI thread to let project system to proceed to next stages.
    /// what that means is that this method should only be used with either await or JTF.Run, it should be never used with Task.Wait otherwise, it can
    /// deadlock
    /// </summary>
    Task WaitUntilFullyLoadedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Indicates whether workspace is fully loaded
    /// <para>
    /// Unfortunately, some hosts, such as VS, use services (ex, IVsOperationProgressStatusService) that require UI
    /// thread to let project system to proceed to next stages. what that means is that this method should only be
    /// used with either await or JTF.Run, it should be never used with Task.Wait otherwise, it can deadlock.
    /// </para>
    /// </summary>
    Task<bool> IsFullyLoadedAsync(CancellationToken cancellationToken);
}
