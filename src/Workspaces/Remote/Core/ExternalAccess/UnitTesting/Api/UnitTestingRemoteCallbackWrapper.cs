// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingRemoteCallbackWrapper<T>
        where T : class
    {
        internal readonly RemoteCallback<T> UnderlyingObject;

        public UnitTestingRemoteCallbackWrapper(T callback)
            => UnderlyingObject = new RemoteCallback<T>(callback);

        public ValueTask InvokeAsync(Func<T, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
            => UnderlyingObject.InvokeAsync(invocation, cancellationToken);

        public ValueTask<TResult> InvokeAsync<TResult>(Func<T, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
            => UnderlyingObject.InvokeAsync(invocation, cancellationToken);
    }
}
