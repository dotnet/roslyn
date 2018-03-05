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
    [ExportWorkspaceService(typeof(IWorkspaceTaskSchedulerFactory), ServiceLayer.Editor), Shared]
    internal class EditorTaskSchedulerFactory : WorkspaceTaskSchedulerFactory
    {
        private readonly IAsynchronousOperationListener _aggregateListener;

        [ImportingConstructor]
        public EditorTaskSchedulerFactory([ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _aggregateListener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.Workspace);
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
