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
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        public EditorTaskSchedulerFactory(IAsynchronousOperationListenerProvider listenerProvider)
        {
            _listener = listenerProvider.GetListener(FeatureAttribute.Workspace);
        }

        protected override object BeginAsyncOperation(string taskName)
        {
            return _listener.BeginAsyncOperation(taskName);
        }

        protected override void CompleteAsyncOperation(object asyncToken, Task task)
        {
            task.CompletesAsyncOperation((IAsyncToken)asyncToken);
        }
    }
}
