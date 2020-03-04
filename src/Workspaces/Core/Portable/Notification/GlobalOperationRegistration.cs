﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Notification
{
    internal class GlobalOperationRegistration : IDisposable
    {
        private readonly AbstractGlobalOperationNotificationService _service;
        private readonly CancellationTokenSource _source;
        private readonly IDisposable _logging;

        private bool _done;

        public GlobalOperationRegistration(AbstractGlobalOperationNotificationService service, string operation)
        {
            _service = service;
            _done = false;
            this.Operation = operation;

            _source = new CancellationTokenSource();
            _logging = Logger.LogBlock(FunctionId.GlobalOperationRegistration, operation, _source.Token);
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

                _logging.Dispose();
            }
            else
            {
                _service.Cancel(this);

                _source.Cancel();
                _logging.Dispose();
            }
        }
    }
}
