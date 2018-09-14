// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host.Mef
{
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
}
