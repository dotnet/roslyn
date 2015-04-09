// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Notification
{
    internal class GlobalOperationRegistration : IDisposable
    {
        private readonly AbstractGlobalOperationNotificationService _service;
        private bool _done;

        public GlobalOperationRegistration(AbstractGlobalOperationNotificationService service, string operation)
        {
            _service = service;
            this.Operation = operation;
            _done = false;
        }

        public string Operation { get; }

        public void Done()
        {
            _done = true;
        }

        public void Dispose()
        {
            if (_done)
            {
                _service.Done(this);
            }
            else
            {
                _service.Cancel(this);
            }
        }
    }
}
