// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Host
{
#if MEF
    [ExportWorkspaceServiceFactory(typeof(IWorkspaceTaskSchedulerFactory), WorkspaceKind.Any)]
#endif
    internal class WorkspaceTaskSchedulerFactoryFactory : IWorkspaceServiceFactory
    {
        private readonly WorkspaceTaskSchedulerFactory singleton = new WorkspaceTaskSchedulerFactory();

        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return singleton;
        }
    }
}
