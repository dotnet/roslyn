// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public sealed partial class AsyncQueue<TElement>
    {
        private class TaskCompletionSourceWithCancellation<T> : TaskCompletionSource<T>
        {
            private readonly Action<object> OnCancelled = 
                (tcs) => ((TaskCompletionSourceWithCancellation<T>)tcs).SetCanceled();

            private CancellationTokenRegistration cancellationTokenRegistration;

            public void RegisterForCancellation(CancellationToken cancellationToken)
            {
                cancellationTokenRegistration = cancellationToken.Register(OnCancelled, this);
            }

            public new void SetCanceled()
            {
                if (base.TrySetCanceled())
                {
                    cancellationTokenRegistration.Dispose();
                }
            }

            public new void SetResult(T value)
            {
                if (base.TrySetResult(value))
                {
                    cancellationTokenRegistration.Dispose();
                }
            }

            public new void SetException(Exception ex)
            {
                if (base.TrySetException(ex))
                {
                    cancellationTokenRegistration.Dispose();
                }
            }
        }
    }
}
