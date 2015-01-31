// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    [ExportWorkspaceServiceFactory(typeof(IWorkspaceTaskSchedulerFactory), ServiceLayer.Editor), Shared]
    internal class WorkspaceTaskSchedulerFactoryFactory : IWorkspaceServiceFactory
    {
        private readonly Service _singleton;

        [ImportingConstructor]
        public WorkspaceTaskSchedulerFactoryFactory(
            [ImportMany]IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _singleton = new Service(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.Workspace));
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton;
        }

        private class Service : WorkspaceTaskSchedulerFactory
        {
            private readonly IAsynchronousOperationListener _aggregateListener;

            public Service(IAsynchronousOperationListener aggregateListener)
            {
                _aggregateListener = aggregateListener;
            }

            protected override object BeginAsyncOperation(string taskName)
            {
                return _aggregateListener.BeginAsyncOperation(taskName);
            }

            protected override void CompleteAsyncOperation(object asyncToken, Task task)
            {
                task.CompletesAsyncOperation((IAsyncToken)asyncToken);
            }
        }
    }
}
