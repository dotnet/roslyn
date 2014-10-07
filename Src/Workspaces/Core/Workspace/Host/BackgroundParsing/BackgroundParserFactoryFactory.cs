// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Host
{
#if MEF
    [ExportWorkspaceServiceFactory(typeof(IBackgroundParserFactory), WorkspaceKind.Any)]
#endif
    internal class BackgroundParserFactoryFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new BackgroundParserFactory(workspaceServices.GetService<IWorkspaceTaskSchedulerFactory>());
        }

        public class BackgroundParserFactory : IBackgroundParserFactory
        {
            private readonly IWorkspaceTaskSchedulerFactory taskSchedulerFactory;

            public BackgroundParserFactory(IWorkspaceTaskSchedulerFactory taskSchedulerFactory)
            {
                this.taskSchedulerFactory = taskSchedulerFactory;
            }

            public IBackgroundParser CreateBackgroundParser(Workspace workspace)
            {
                return new BackgroundParser(taskSchedulerFactory, workspace);
            }
        }
    }
}