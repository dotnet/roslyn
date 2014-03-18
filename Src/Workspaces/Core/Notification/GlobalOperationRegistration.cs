// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Notification
{
    internal class GlobalOperationRegistration : IDisposable
    {
        private readonly AbstractGlobalOperationNotificationService service;
        private bool done;

        public GlobalOperationRegistration(AbstractGlobalOperationNotificationService service, string operation)
        {
            this.service = service;
            this.Operation = operation;
            this.done = false;
        }

        public string Operation { get; private set; }

        public void Done()
        {
            done = true;
        }

        public void Dispose()
        {
            if (this.done)
            {
                this.service.Done(this);
            }
            else
            {
                this.service.Cancel(this);
            }
        }
    }
}
