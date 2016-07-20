// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Per host specific services provided by the host environment.
    /// </summary>
    internal abstract class HostSpecificServices
    {
        /// <summary>
        /// The <see cref="HostWorkspaceServices"/> that originated this language service.
        /// </summary>
        public abstract HostWorkspaceServices WorkspaceServices { get; }

        /// <summary>
        /// The name of the host
        /// </summary>
        public abstract string Host { get; }

        /// <summary>
        /// Gets a host specific service provided by the host identified by the service type. 
        /// If the host does not provide the service, this method returns null.
        /// </summary>
        public abstract THostSpecificService GetService<THostSpecificService>() where THostSpecificService : IHostSpecificService;

        /// <summary>
        /// Gets a host specific service provided by the host identified by the service type. 
        /// If the host does not provide the service, this method returns throws <see cref="InvalidOperationException"/>.
        /// </summary>
        public THostSpecificService GetRequiredService<THostSpecificService>() where THostSpecificService : IHostSpecificService
        {
            var service = GetService<THostSpecificService>();
            if (service == null)
            {
                throw new InvalidOperationException(WorkspacesResources.Service_of_type_0_is_required_to_accomplish_the_task_but_is_not_available_from_the_workspace);
            }

            return service;
        }
    }
}
