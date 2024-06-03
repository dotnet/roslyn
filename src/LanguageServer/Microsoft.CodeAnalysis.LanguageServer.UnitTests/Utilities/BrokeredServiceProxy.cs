// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Threading;
using Nerdbank.Streams;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// A wrapper which takes a service but actually sends calls to it through JsonRpc to ensure we can actually use the service across a wire.
/// </summary>
internal sealed class BrokeredServiceProxy<T> : System.IAsyncDisposable where T : class
{
    /// <summary>
    /// A task that cane awaited to assert the rest of the fields in this class being assigned and non-null.
    /// </summary>
    private readonly Task _createConnectionTask;

    private JsonRpc? _serverRpc;
    private JsonRpc? _clientRpc;
    private T? _clientFactoryProxy;

    public BrokeredServiceProxy(T service)
    {
        var (serverStream, clientStream) = FullDuplexStream.CreatePair();

        _createConnectionTask = Task.WhenAll(CreateServerAsync(), CreateClientAsync());
        return;

        async Task CreateServerAsync()
        {
            // Always yield to ensure caller can proceed.
            await TaskScheduler.Default.SwitchTo(alwaysYield: true);

            var serverMultiplexingStream = await MultiplexingStream.CreateAsync(serverStream);
            var serverChannel = await serverMultiplexingStream.AcceptChannelAsync("");

            var serverFormatter = new MessagePackFormatter() { MultiplexingStream = serverMultiplexingStream };
            _serverRpc = new JsonRpc(new LengthHeaderMessageHandler(serverChannel, serverFormatter));

            _serverRpc.AddLocalRpcTarget(service, options: null);
            _serverRpc.StartListening();
        }

        async Task CreateClientAsync()
        {
            // Always yield to ensure caller can proceed.
            await TaskScheduler.Default.SwitchTo(alwaysYield: true);

            var clientMultiplexingStream = await MultiplexingStream.CreateAsync(clientStream);
            var clientChannel = await clientMultiplexingStream.OfferChannelAsync("");

            var clientFormatter = new MessagePackFormatter() { MultiplexingStream = clientMultiplexingStream };
            _clientRpc = new JsonRpc(new LengthHeaderMessageHandler(clientChannel, clientFormatter));

            _clientFactoryProxy = _clientRpc.Attach<T>();
            _clientRpc.StartListening();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _createConnectionTask;

        _serverRpc!.Dispose();
        _clientRpc!.Dispose();
    }

    public async Task<T> GetServiceAsync()
    {
        await _createConnectionTask;
        return _clientFactoryProxy!;
    }
}
