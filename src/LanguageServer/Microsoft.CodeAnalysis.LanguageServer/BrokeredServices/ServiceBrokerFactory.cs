// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.BrokeredServiceBridgeManifest;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Utilities.ServiceBroker;
using ExportProvider = Microsoft.VisualStudio.Composition.ExportProvider;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;

[ExportCSharpVisualBasicLspService(typeof(ServiceBrokerFactory)), Shared(LspServiceComposition.SharingBoundary)]
internal sealed class ServiceBrokerFactory : ILspService
{
    private readonly ExportProvider _exportProvider;
    private Task _bridgeCompletionTask;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ImmutableArray<IServiceBrokerInitializer> _serviceBrokerInitializers;
    private readonly ILoggerFactory _loggerFactory;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ServiceBrokerFactory(
        LspService<LspServices> lspServices,
        ExportProvider exportProvider,
        LspService<ILoggerFactory> loggerFactory)
    {
        var serviceBrokerInitializers = lspServices.Value.GetRequiredServices<IServiceBrokerInitializer>();
        var loggerFactoryValue = loggerFactory.Value;
        _exportProvider = exportProvider;
        _loggerFactory = loggerFactoryValue;
        _bridgeCompletionTask = Task.CompletedTask;
        _serviceBrokerInitializers = [.. serviceBrokerInitializers];
    }

    /// <summary>
    /// Creates a service broker instance without connecting via a pipe to another process.
    /// </summary>
    public async Task<BrokeredServiceContainer> CreateAsync(Workspace workspace)
    {
        var container = await BrokeredServiceContainer.CreateAsync(_exportProvider, _serviceBrokerInitializers, _loggerFactory, _cancellationTokenSource.Token);

        // Proffer the manifest service that describes the services proffered by this process across the bridge, so the other side can know what services to expect.
        ProfferBridgeManifest(container, _loggerFactory);

        // Make the container available to workspace services.
        var provider = (ServiceBrokerProvider)workspace.Services.GetRequiredService<IServiceBrokerProvider>();
        provider.SetContainer(container);

        foreach (var onInitialized in _serviceBrokerInitializers)
        {
            try
            {
                onInitialized.OnServiceBrokerInitialized(container.GetFullAccessServiceBroker(), _cancellationTokenSource.Token);
            }
            catch (Exception)
            {
            }
        }

        return container;

        static void ProfferBridgeManifest(BrokeredServiceContainer container, ILoggerFactory loggerFactory)
        {
            container.RegisterServices(new Dictionary<ServiceMoniker, ServiceRegistration>
            {
                { BrokeredServiceBridgeManifest.ServiceDescriptor.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) }
            });
            container.Proffer(
                BrokeredServiceBridgeManifest.ServiceDescriptor,
                (moniker, options, innerServiceBroker, cancellationToken) =>
                {
                    var bridgeManifestService = new BrokeredServiceBridgeManifest(container, loggerFactory);
                    return new ValueTask<object?>(bridgeManifestService);
                });
        }
    }

    public async Task CreateAndConnectAsync(string brokeredServicePipeName, Workspace workspace)
    {
        var container = await CreateAsync(workspace);

        var bridgeProvider = _exportProvider.GetExportedValue<BrokeredServiceBridgeProvider>();
        _bridgeCompletionTask = bridgeProvider.SetupBrokeredServicesBridgeAsync(brokeredServicePipeName, container, _loggerFactory, _cancellationTokenSource.Token);
    }

    public async Task ShutdownAndWaitForCompletionAsync()
    {
        _cancellationTokenSource.Cancel();

        // Await the task we created when we created the bridge; if we never started it in the first place, we'll just return the
        // completed task set in the constructor, so the waiter no-ops.
        try
        {
            await _bridgeCompletionTask;
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown, swallow.
        }
    }
}
