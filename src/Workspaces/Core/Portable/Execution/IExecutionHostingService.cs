// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Service one can use to get <see cref="HostSpecificServices"/> that can run on a host
    /// 
    /// TODO: this might move into workspace as LanguageService does. <see cref="HostWorkspaceServices.GetLanguageServices(string)"/>
    ///       currently, this interface only exist to decouple this functionality from workspace
    /// </summary>
    internal interface IExecutionHostingService : IWorkspaceService
    {
        /// <summary>
        /// Return requested service with best host with current context
        /// </summary>
        HostSpecificServices GetService();

        /// <summary>
        /// Return requested service with given host
        /// </summary>
        HostSpecificServices GetService(string host);
    }
}
