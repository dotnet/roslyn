// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host.Mef;

/// <summary>
/// A factory that creates instances of a specific <see cref="IWorkspaceService"/>.
/// 
/// Implement a <see cref="IWorkspaceServiceFactory"/> when you want to provide <see cref="IWorkspaceService"/> instances that use other services.
/// </summary>
public interface IWorkspaceServiceFactory
{
    /// <summary>
    /// Creates a new <see cref="IWorkspaceService"/> instance.
    /// Returns <c>null</c> if the service is not applicable to the given workspace.
    /// </summary>
    /// <param name="workspaceServices">The <see cref="HostWorkspaceServices"/> that can be used to access other services.</param>
    IWorkspaceService CreateService(HostWorkspaceServices workspaceServices);
}
