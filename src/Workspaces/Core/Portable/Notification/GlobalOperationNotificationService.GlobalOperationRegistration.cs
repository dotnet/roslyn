// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Notification;

internal partial class GlobalOperationNotificationService
{
    private class GlobalOperationRegistration : IGlobalOperationRegistration
    {
        private readonly GlobalOperationNotificationService _service;
        private readonly CancellationTokenSource _source;
        private readonly IDisposable _logging;

        private bool _done = false;

        public GlobalOperationRegistration(GlobalOperationNotificationService service, string operation)
        {
            _service = service;

            _source = new CancellationTokenSource();
            _logging = Logger.LogBlock(FunctionId.GlobalOperationRegistration, operation, _source.Token);
        }

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
