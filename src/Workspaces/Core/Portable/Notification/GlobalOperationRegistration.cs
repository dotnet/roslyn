// Licensed to the .NET Foundation under one or more agreements.
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

        /// <summary>
        /// Used to indicate that the global operation completed fully.  The only effect this has is how this operation
        /// will be logged when <see cref="Dispose"/> is called. If this has been called, then <see cref="Dispose"/>
        /// will log that we completed without cancellation.  If this has not been called, then <see cref="Dispose"/>
        /// will log that we were canceled.
        /// </summary>
        public void Done()
            => _done = true;

        public void Dispose()
        {
            // Inform any listeners that we're finished.
            _service.Done(this);

            // If 'Done' wasn't called, cancel our cancellation-source so that our logging block will record that we
            // didn't finish
            if (!_done)
                _source.Cancel();

            _logging.Dispose();
        }
    }
}
