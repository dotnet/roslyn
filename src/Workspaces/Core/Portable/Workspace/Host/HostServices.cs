// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Services provided by the host environment.
    /// </summary>
    public abstract class HostServices
    {
        /// <summary>
        /// Creates a new workspace service. 
        /// </summary>
        protected internal abstract HostWorkspaceServices CreateWorkspaceServices(Workspace workspace);
    }
}
