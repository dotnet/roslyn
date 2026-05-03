// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Utilities.ServiceBroker;
using Nerdbank.Streams;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;

[Export, Shared]
internal sealed class BrokeredServiceBridgeProvider
{
    private const string ServiceBrokerChannelName = "serviceBroker";

    private readonly ILogger _logger;
    private readonly TraceSource _brokeredServiceTraceSource;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public BrokeredServiceBridgeProvider(ILoggerFactory loggerFactory, BrokeredServiceTraceListener brokeredServiceTraceListener)
    {
        _logger = loggerFactory.CreateLogger<BrokeredServiceBridgeProvider>();
        _brokeredServiceTraceSource = brokeredServiceTraceListener.Source;
    }

    /// <summary>
    /// Creates the brokered service bridge to the remote process.
    /// We expose the services from our container to the remote and consume services
    /// from the remote by proffering them into our container.
    /// </summary>
    /// <param name="brokeredServicePipeName">the pipe name we use for the connection.</param>
    /// <param name="container">our local container.</param>
    /// <param name="cancellationToken">a cancellation token.</param>
    /// <returns>a task that represents the lifetime of the bridge.  It will complete when the bridge closes.</returns>
    public async Task SetupBrokeredServicesBridgeAsync(string brokeredServicePipeName, BrokeredServiceContainer container, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Setting up brokered service bridge");
        using var bridgeStream = await ServerFactory.ConnectAsync(brokeredServicePipeName, cancellationToken);
        using var bridgeMxStream = await MultiplexingStream.CreateAsync(bridgeStream, cancellationToken);

        // Wait until the connection ends (so we don't dispose of the stream before it ends).
        await Task.WhenAll(ProfferServicesToRemoteAsync(), ConsumeServicesFromRemoteAsync());

        async Task ProfferServicesToRemoteAsync()
        {
            using var profferedServiceBrokerChannel = await bridgeMxStream.OfferChannelAsync(ServiceBrokerChannelName, cancellationToken);
            var serviceBroker = container.GetLimitedAccessServiceBroker(ServiceAudience.Local, ImmutableDictionary<string, string>.Empty, ClientCredentialsPolicy.RequestOverridesDefault);
            using IpcRelayServiceBroker relayServiceBroker = new(serviceBroker);

            FrameworkServices.RemoteServiceBroker
                .WithTraceSource(_brokeredServiceTraceSource)
                .ConstructRpc(relayServiceBroker, profferedServiceBrokerChannel);

            await relayServiceBroker.Completion.WaitAsync(cancellationToken);
        }

        async Task ConsumeServicesFromRemoteAsync()
        {
            using var consumingServiceBrokerChannel = await bridgeMxStream.AcceptChannelAsync(ServiceBrokerChannelName, cancellationToken);
            var remoteClient = FrameworkServices.RemoteServiceBroker
                .WithTraceSource(_brokeredServiceTraceSource)
                .ConstructRpc<IRemoteServiceBroker>(consumingServiceBrokerChannel);

            using (container.ProfferRemoteBroker(remoteClient, bridgeMxStream, ServiceSource.OtherProcessOnSameMachine, [.. Descriptors.RemoteServicesToRegister.Keys]))
            {
                await consumingServiceBrokerChannel.Completion.WaitAsync(cancellationToken);
            }
        }
    }
}
