// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.WorkspaceServices
{
    internal interface IWorkspaceServiceProviderFactory
    {
        /// <summary>
        /// Only use this API if you are constructing a Workspace! 
        /// Do not use it to access a service without a workspace.
        /// </summary>
        IWorkspaceServiceProvider CreateWorkspaceServiceProvider(string workspaceKind);
    }
}