// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Host
{
#if MEF
    [ExportWorkspaceServiceFactory(typeof(IBackgroundCompilerFactory), WorkspaceKind.Any)]
#endif
    internal class BackgroundCompilerFactoryFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new BackgroundCompilerFactory(workspaceServices.GetService<IWorkspaceTaskSchedulerFactory>());
        }

        public class BackgroundCompilerFactory : IBackgroundCompilerFactory
        {
            private readonly IWorkspaceTaskSchedulerFactory taskSchedulerFactory;

            public BackgroundCompilerFactory(IWorkspaceTaskSchedulerFactory taskSchedulerFactory)
            {
                this.taskSchedulerFactory = taskSchedulerFactory;
            }

            public IBackgroundCompiler CreateBackgroundCompiler(Workspace workspace)
            {
                return new BackgroundCompiler(this.taskSchedulerFactory, workspace);
            }
        }
    }
}