﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Formatters;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorRemoteHostClient
    {
        private readonly ServiceHubRemoteHostClient _client;
        private readonly RazorServiceDescriptorsWrapper _serviceDescriptors;
        private readonly RazorRemoteServiceCallbackDispatcherRegistry _callbackDispatchers;

        internal RazorRemoteHostClient(ServiceHubRemoteHostClient client, RazorServiceDescriptorsWrapper serviceDescriptors, RazorRemoteServiceCallbackDispatcherRegistry callbackDispatchers)
        {
            _client = client;
            _serviceDescriptors = serviceDescriptors;
            _callbackDispatchers = callbackDispatchers;
        }

        [Obsolete]
        public static async Task<RazorRemoteHostClient?> CreateAsync(Workspace workspace, CancellationToken cancellationToken = default)
        {
            var client = await RemoteHostClient.TryGetClientAsync(workspace.Services, cancellationToken).ConfigureAwait(false);
            var descriptors = new RazorServiceDescriptorsWrapper("dummy", _ => throw ExceptionUtilities.Unreachable, ImmutableArray<IMessagePackFormatter>.Empty, ImmutableArray<IFormatterResolver>.Empty, Array.Empty<(Type, Type?)>());
            return client == null ? null : new RazorRemoteHostClient((ServiceHubRemoteHostClient)client, descriptors, RazorRemoteServiceCallbackDispatcherRegistry.Empty);
        }

        [Obsolete]
        public async Task<Optional<T>> TryRunRemoteAsync<T>(string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
            => await _client.RunRemoteAsync<T>(WellKnownServiceHubService.Razor, targetName, solution, arguments, callbackTarget: null, cancellationToken).ConfigureAwait(false);

        public static async Task<RazorRemoteHostClient?> TryGetClientAsync(HostWorkspaceServices services, RazorServiceDescriptorsWrapper serviceDescriptors, RazorRemoteServiceCallbackDispatcherRegistry callbackDispatchers, CancellationToken cancellationToken = default)
        {
            var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
            if (client is null)
                return null;

            return new RazorRemoteHostClient((ServiceHubRemoteHostClient)client, serviceDescriptors, callbackDispatchers);
        }

        public RazorRemoteServiceConnectionWrapper<TService> CreateConnection<TService>(object? callbackTarget) where TService : class
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

        public async ValueTask<bool> TryInvokeAsync<TService>(Func<TService, RazorRemoteServiceCallbackIdWrapper, CancellationToken, ValueTask> invocation, object callbackTarget, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(Func<TService, RazorRemoteServiceCallbackIdWrapper, CancellationToken, ValueTask<TResult>> invocation, object callbackTarget, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
        }

        // solution, no callback:

        public async ValueTask<bool> TryInvokeAsync<TService>(Solution solution, Func<TService, RazorPinnedSolutionInfoWrapper, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(solution, invocation, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(Solution solution, Func<TService, RazorPinnedSolutionInfoWrapper, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget: null);
            return await connection.TryInvokeAsync(solution, invocation, cancellationToken).ConfigureAwait(false);
        }

        // solution, callback:

        public async ValueTask<bool> TryInvokeAsync<TService>(Solution solution, Func<TService, RazorPinnedSolutionInfoWrapper, RazorRemoteServiceCallbackIdWrapper, CancellationToken, ValueTask> invocation, object callbackTarget, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(solution, invocation, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(Solution solution, Func<TService, RazorPinnedSolutionInfoWrapper, RazorRemoteServiceCallbackIdWrapper, CancellationToken, ValueTask<TResult>> invocation, object callbackTarget, CancellationToken cancellationToken) where TService : class
        {
            using var connection = CreateConnection<TService>(callbackTarget);
            return await connection.TryInvokeAsync(solution, invocation, cancellationToken).ConfigureAwait(false);
        }
    }
}
