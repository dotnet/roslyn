// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal readonly struct RazorRemoteServiceConnectionWrapper<TService> : IDisposable
        where TService : class
    {
        internal RemoteServiceConnection<TService> UnderlyingObject { get; }

        internal RazorRemoteServiceConnectionWrapper(RemoteServiceConnection<TService> underlyingObject)
            => UnderlyingObject = underlyingObject;

        public void Dispose()
            => UnderlyingObject.Dispose();

        // no solution, no callback

        public ValueTask<bool> TryInvokeAsync(Func<TService, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(invocation, cancellationToken);

        public ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Func<TService, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(invocation, cancellationToken);

        // no solution, callback

        public ValueTask<bool> TryInvokeAsync(Func<TService, RazorRemoteServiceCallbackIdWrapper, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(
                (service, callbackId, cancellationToken) => invocation(service, callbackId, cancellationToken),
                cancellationToken);

        public ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Func<TService, RazorRemoteServiceCallbackIdWrapper, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(
                (service, callbackId, cancellationToken) => invocation(service, callbackId, cancellationToken),
                cancellationToken);

        // solution, no callback

        public ValueTask<bool> TryInvokeAsync(Solution solution, Func<TService, RazorPinnedSolutionInfoWrapper, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(
                solution,
                (service, solutionInfo, cancellationToken) => invocation(service, solutionInfo, cancellationToken),
                cancellationToken);

        public ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Solution solution, Func<TService, RazorPinnedSolutionInfoWrapper, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(
                solution,
                (service, solutionInfo, cancellationToken) => invocation(service, solutionInfo, cancellationToken),
                cancellationToken);

        // solution, callback

        public ValueTask<bool> TryInvokeAsync(Solution solution, Func<TService, RazorPinnedSolutionInfoWrapper, RazorRemoteServiceCallbackIdWrapper, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(
                solution,
                (service, solutionInfo, callbackId, cancellationToken) => invocation(service, solutionInfo, callbackId, cancellationToken),
                cancellationToken);

        public ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Solution solution, Func<TService, RazorPinnedSolutionInfoWrapper, RazorRemoteServiceCallbackIdWrapper, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(
                solution,
                (service, solutionInfo, callbackId, cancellationToken) => invocation(service, solutionInfo, callbackId, cancellationToken),
                cancellationToken);
    }
}
