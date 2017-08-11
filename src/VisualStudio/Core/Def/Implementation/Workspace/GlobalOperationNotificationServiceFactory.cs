// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IGlobalOperationNotificationService), ServiceLayer.Host), Shared]
    internal class GlobalOperationNotificationServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IAsynchronousOperationListener _listener;
        private readonly Service _singleton;

        [ImportingConstructor]
        public GlobalOperationNotificationServiceFactory(
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _listener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.GlobalOperation);
            _singleton = new Service(_listener);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton;
        }

        private class Service : GlobalOperationNotificationService
        {
            private readonly IAsynchronousOperationListener _listener;

            public Service(IAsynchronousOperationListener listener)
            {
                _listener = listener;
            }

            protected override Task RaiseGlobalOperationStarted()
            {
                // have to do this way since IAsynchronousOperationListener is not at workspace layer
                var eventToken = _listener.BeginAsyncOperation("GlobalOperationStarted");
                return base.RaiseGlobalOperationStarted().CompletesAsyncOperation(eventToken);
            }

            protected override Task RaiseGlobalOperationStopped(IReadOnlyList<string> operations, bool cancelled)
            {
                // have to do this way since IAsynchronousOperationListener is not at workspace layer
                var eventToken = _listener.BeginAsyncOperation("GlobalOperationStopped");
                return base.RaiseGlobalOperationStopped(operations, cancelled).CompletesAsyncOperation(eventToken);
            }
        }
    }
}
