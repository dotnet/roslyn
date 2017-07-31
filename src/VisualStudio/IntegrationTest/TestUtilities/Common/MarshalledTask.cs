// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common
{
    public class MarshalledTask : MarshalByRefObject
    {
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();

        public Task Task => _taskCompletionSource.Task;

        public void OnRanToCompletion()
            => _taskCompletionSource.TrySetResult(true);

        public void OnCanceled()
            => _taskCompletionSource.TrySetCanceled();

        public void OnFaulted(Exception ex)
            => _taskCompletionSource.TrySetException(ex);
    }
}
