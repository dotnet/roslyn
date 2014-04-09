// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Notification
{
#if MEF
    using Microsoft.CodeAnalysis.Host.Mef;

    [ExportWorkspaceServiceFactory(typeof(IGlobalOperationNotificationService), ServiceLayer.Default)]
#endif
    internal class GlobalOperationNotificationServiceFactory : IWorkspaceServiceFactory
    {
        private static readonly NoOpService singleton = new NoOpService();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            // all different workspace kinds will share same service
            return singleton;
        }

        /// <summary>
        /// a service which will never raise start event
        /// </summary>
        private class NoOpService : AbstractGlobalOperationNotificationService
        {
            private readonly GlobalOperationRegistration noOpRegistration;

            public NoOpService()
            {
                this.noOpRegistration = new GlobalOperationRegistration(this, "NoOp");

                // here to shut up never used warnings.
                var started = Started;
                var stopped = Stopped;
            }

            public override event EventHandler Started;
            public override event EventHandler<GlobalOperationEventArgs> Stopped;

            public override GlobalOperationRegistration Start(string reason)
            {
                return this.noOpRegistration;
            }

            public override void Cancel(GlobalOperationRegistration registration)
            {
                // do nothing
            }

            public override void Done(GlobalOperationRegistration registration)
            {
                // do nothing
            }
        }
    }
}
