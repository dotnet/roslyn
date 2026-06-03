// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal sealed class RazorRemoteHostClient
{
    // Razor's remote services do not register client callbacks, so an empty registry is sufficient.
    private static readonly RemoteServiceCallbackDispatcherRegistry s_emptyCallbackDispatchers = new(
        Array.Empty<Lazy<IRemoteServiceCallbackDispatcher, RemoteServiceCallbackDispatcherRegistry.ExportMetadata>>());

    private readonly ServiceHubRemoteHostClient _client;
    private readonly RazorServiceDescriptors _serviceDescriptors;

    internal RazorRemoteHostClient(ServiceHubRemoteHostClient client, RazorServiceDescriptors serviceDescriptors)
    {
        _client = client;
        _serviceDescriptors = serviceDescriptors;
    }

    public static async Task<RazorRemoteHostClient?> TryGetClientAsync(HostWorkspaceServices services, RazorServiceDescriptors serviceDescriptors, CancellationToken cancellationToken = default)
    {
        var client = await RemoteHostClient.TryGetClientAsync(services.SolutionServices, cancellationToken).ConfigureAwait(false);
        if (client is null)
            return null;

        return new RazorRemoteHostClient((ServiceHubRemoteHostClient)client, serviceDescriptors);
    }

    private RemoteServiceConnection<TService> CreateConnection<TService>() where TService : class
        => _client.CreateConnection<TService>(_serviceDescriptors.UnderlyingObject, s_emptyCallbackDispatchers, callbackTarget: null);

    // no solution

    public async ValueTask<bool> TryInvokeAsync<TService>(Func<TService, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken) where TService : class
    {
        using var connection = CreateConnection<TService>();
        return await connection.TryInvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(Func<TService, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken) where TService : class
    {
        using var connection = CreateConnection<TService>();
        return await connection.TryInvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
    }

    // solution

    public async ValueTask<bool> TryInvokeAsync<TService>(Solution solution, Func<TService, RazorSolutionWrapper, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken) where TService : class
    {
        using var connection = CreateConnection<TService>();
        return await connection.TryInvokeAsync(
            solution,
            (service, solutionInfo, cancellationToken) => invocation(service, solutionInfo, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<Optional<TResult>> TryInvokeAsync<TService, TResult>(Solution solution, Func<TService, RazorSolutionWrapper, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken) where TService : class
    {
        using var connection = CreateConnection<TService>();
        return await connection.TryInvokeAsync(
            solution,
            (service, solutionInfo, cancellationToken) => invocation(service, solutionInfo, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }
}
