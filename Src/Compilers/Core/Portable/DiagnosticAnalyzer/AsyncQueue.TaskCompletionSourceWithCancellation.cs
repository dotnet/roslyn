// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public sealed partial class AsyncQueue<TElement>
    {
        private sealed class TaskCompletionSourceWithCancellation<T> : TaskCompletionSource<T>
        {
            private readonly Action<object> OnCancelled = 
                (tcs) => ((TaskCompletionSourceWithCancellation<T>)tcs).SetCanceled();

            private CancellationTokenRegistration cancellationTokenRegistration;

            public void RegisterForCancellation(CancellationToken cancellationToken)
            {
                Debug.Assert(cancellationTokenRegistration == default(CancellationTokenRegistration));
                cancellationTokenRegistration = cancellationToken.Register(OnCancelled, this);
                base.Task.ContinueWith(_ => { cancellationTokenRegistration.Dispose(); });
            }
        }
    }
}
