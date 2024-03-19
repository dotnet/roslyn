// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal readonly struct PythiaRemoteHostClient
    {
        private readonly ServiceHubRemoteHostClient _client;
        private readonly PythiaServiceDescriptorsWrapper _serviceDescriptors;
        private readonly PythiaRemoteServiceCallbackDispatcherRegistry _callbackDispatchers;

        internal PythiaRemoteHostClient(ServiceHubRemoteHostClient client, PythiaServiceDescriptorsWrapper serviceDescriptors, PythiaRemoteServiceCallbackDispatcherRegistry callbackDispatchers)
        {
            _client = client;
            _serviceDescriptors = serviceDescriptors;
            _callbackDispatchers = callbackDispatchers;
        }

        public static async Task<PythiaRemoteHostClient?> TryGetClientAsync(HostWorkspaceServices services, PythiaServiceDescriptorsWrapper serviceDescriptors, PythiaRemoteServiceCallbackDispatcherRegistry callbackDispatchers, CancellationToken cancellationToken = default)
        {
            var client = await RemoteHostClient.TryGetClientAsync(services.SolutionServices, cancellationToken).ConfigureAwait(false);
            if (client is null)
                return null;

            return new PythiaRemoteHostClient((ServiceHubRemoteHostClient)client, serviceDescriptors, callbackDispatchers);
        }

        private PythiaRemoteServiceConnectionWrapper<TService> CreateConnection<TService>(object? callbackTarget) where TService : class
            => new(_client.CreateConnection<TService>(_serviceDescriptors.UnderlyingObject, _callbackDispatchers, callbackTarget));

        // no solution, no callback:

        public async ValueTask<bool> TryInvokeAsync<TService>(Func<TService, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(Func<TService, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
        }

        // no solution, callback:

        public async ValueTask<bool> TryInvokeAsync<TService>(Func<TService, PythiaRemoteServiceCallbackIdWrapper, CancellationToken, ValueTask> invocation, object callbackTarget, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(Func<TService, PythiaRemoteServiceCallbackIdWrapper, CancellationToken, ValueTask<TResult>> invocation, object callbackTarget, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
        }

        // solution, no callback:

        public async ValueTask<bool> TryInvokeAsync<TService>(Solution solution, Func<TService, PythiaPinnedSolutionInfoWrapper, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(solution, invocation, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(Solution solution, Func<TService, PythiaPinnedSolutionInfoWrapper, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(solution, invocation, cancellationToken).ConfigureAwait(false);
        }

        // solution, callback:

        public async ValueTask<bool> TryInvokeAsync<TService>(Solution solution, Func<TService, PythiaPinnedSolutionInfoWrapper, PythiaRemoteServiceCallbackIdWrapper, CancellationToken, ValueTask> invocation, object callbackTarget, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(solution, invocation, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(Solution solution, Func<TService, PythiaPinnedSolutionInfoWrapper, PythiaRemoteServiceCallbackIdWrapper, CancellationToken, ValueTask<TResult>> invocation, object callbackTarget, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(solution, invocation, cancellationToken).ConfigureAwait(false);
        }
    }
}
