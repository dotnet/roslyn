// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
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
    private readonly ImmutableArray<IServiceBrokerInitializer> _initializers;
    private readonly ILogger _logger;
    private readonly TraceSource _traceSource;

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
        _logger = loggerFactoryValue.CreateLogger<ServiceBrokerFactory>();
        _traceSource = BrokeredServiceTraceListener.CreateTraceSource(loggerFactoryValue);
        _bridgeCompletionTask = Task.CompletedTask;
        _initializers = [.. serviceBrokerInitializers];
    }

    /// <summary>
    /// Creates a service broker instance without connecting via a pipe to another process.
    /// </summary>
    internal async Task<BrokeredServiceContainer> CreateAsync(Workspace workspace)
    {
        var cancellationToken = _cancellationTokenSource.Token;

        var container = await BrokeredServiceContainer.CreateAsync(_exportProvider, _traceSource, cancellationToken);

        // Register and proffer all services that come from service broker manual initialization
        var servicesToRegister = _initializers.SelectMany(s => s.ServicesToRegister).ToDictionary(a => a.Key, a => a.Value);
        container.RegisterServices(servicesToRegister);

        // Proffer the manifest service that describes the services proffered by this process across the bridge, so the other side can know what services to expect.
        ProfferBridgeManifest(container, _logger);

        // Make the container available to workspace services.
        var provider = (ServiceBrokerProvider)workspace.Services.GetRequiredService<IServiceBrokerProvider>();
        provider.SetContainer(container);

        // Proffer might request dependent remote services from the container,
        // so we need to proffer after the container is set up and services registered.
        foreach (var initializer in _initializers)
        {
            initializer.Proffer(container);
        }

        return container;

        static void ProfferBridgeManifest(BrokeredServiceContainer container, ILogger logger)
        {
            container.RegisterServices(new Dictionary<ServiceMoniker, ServiceRegistration>
            {
                { BrokeredServiceBridgeManifest.ServiceDescriptor.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) }
            });

            container.Proffer(
                BrokeredServiceBridgeManifest.ServiceDescriptor,
                (moniker, options, innerServiceBroker, cancellationToken) =>
                {
                    var bridgeManifestService = new BrokeredServiceBridgeManifest(container, logger);
                    return new ValueTask<object?>(bridgeManifestService);
                });
        }
    }

    public async Task CreateAndConnectAsync(string brokeredServicePipeName, Workspace workspace)
    {
        var container = await CreateAsync(workspace);
        var serviceBroker = container.GetFullAccessServiceBroker();
        var bridgeProvider = _exportProvider.GetExportedValue<BrokeredServiceBridgeProvider>();

        _bridgeCompletionTask = bridgeProvider.SetupBrokeredServicesBridgeAsync(
            brokeredServicePipeName,
            container,
            _logger,
            _traceSource,
            onServicesAvailable: async cancellationToken =>
            {
                foreach (var initializer in _initializers)
                {
                    try
                    {
                        await initializer.OnServiceBrokerInitializedAsync(serviceBroker, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Unexpected exception while initializing brokered services: {e}");
                    }
                }
            },
            _cancellationTokenSource.Token);
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

        _cancellationTokenSource.Dispose();
    }
}
