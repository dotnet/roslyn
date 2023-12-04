// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.Definitions;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

#pragma warning disable RS0030 // This is intentionally using System.ComponentModel.Composition for compatibility with MEF service broker.
[Export]
internal class ProjectInitializationHandler : IDisposable
{
    private const string ProjectInitializationCompleteName = "workspace/projectInitializationComplete";

    private readonly IServiceBroker _serviceBroker;
    private readonly ServiceBrokerClient _serviceBrokerClient;
    private readonly ILogger _logger;

    private readonly TaskCompletionSource _serviceAvailable = new();
    private readonly ProjectInitializationCompleteObserver _projectInitializationCompleteObserver;

    private IDisposable? _subscription;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ProjectInitializationHandler([Import(typeof(SVsFullAccessServiceBroker))] IServiceBroker serviceBroker, ILoggerFactory loggerFactory)
    {
        _serviceBroker = serviceBroker;
        _serviceBroker.AvailabilityChanged += AvailabilityChanged;
        _serviceBrokerClient = new ServiceBrokerClient(serviceBroker, joinableTaskFactory: null);

        _logger = loggerFactory.CreateLogger<ProjectInitializationHandler>();
        _projectInitializationCompleteObserver = new ProjectInitializationCompleteObserver(_logger);
    }

    public static async Task SendProjectInitializationCompleteNotificationAsync()
    {
        Contract.ThrowIfNull(LanguageServerHost.Instance, "We don't have an LSP channel yet to send this request through.");
        var languageServerManager = LanguageServerHost.Instance.GetRequiredLspService<IClientLanguageServerManager>();
        await languageServerManager.SendNotificationAsync(ProjectInitializationCompleteName, CancellationToken.None);
    }

    public async Task SubscribeToInitializationCompleteAsync(CancellationToken cancellationToken)
    {
        // Use the ServiceBrokerClient so that we actually hold onto the instance of the service to prevent it from being disposed of until we're shutting down.
        var didSubscribe = await TrySubscribeAsync(cancellationToken);
        if (!didSubscribe)
        {
            // Service might be null the first time we try to access it - wait for it to become available on the remote side.
            await _serviceAvailable.Task;
            didSubscribe = await TrySubscribeAsync(cancellationToken);
            Contract.ThrowIfFalse(didSubscribe, $"Unable to subscribe to {Descriptors.RemoteProjectInitializationStatusService.Moniker}");
        }
    }

    private async Task<bool> TrySubscribeAsync(CancellationToken cancellationToken)
    {
        using var rental = await _serviceBrokerClient.GetProxyAsync<IProjectInitializationStatusService>(Descriptors.RemoteProjectInitializationStatusService, cancellationToken);
        if (rental.Proxy is not null)
        {
            _subscription = await rental.Proxy.SubscribeInitializationCompletionAsync(_projectInitializationCompleteObserver, cancellationToken);
            return true;
        }

        return false;
    }

    private void AvailabilityChanged(object? sender, BrokeredServicesChangedEventArgs e)
    {
        if (e.ImpactedServices.Contains(Descriptors.RemoteProjectInitializationStatusService.Moniker))
            _serviceAvailable.SetResult();
    }

    public void Dispose()
    {
        _serviceBroker.AvailabilityChanged -= AvailabilityChanged;
        _subscription?.Dispose();
        _serviceBrokerClient.Dispose();
    }

    internal class ProjectInitializationCompleteObserver : IObserver<ProjectInitializationCompletionState>
    {
        private readonly ILogger _logger;

        public ProjectInitializationCompleteObserver(ILogger logger)
        {
            _logger = logger;
        }

        [JsonRpcMethod("onCompleted")]
        public void OnCompleted()
        {
            // NoOp - OnNext is the only method that will be called upon completion of initial project load.
        }

        [JsonRpcMethod("onError", UseSingleObjectParameterDeserialization = true)]
        public void OnError(Exception error)
        {
            _logger.LogError(error, "Devkit project initialization observer failed");
        }

        [JsonRpcMethod("onNext", UseSingleObjectParameterDeserialization = true)]
        public void OnNext(ProjectInitializationCompletionState value)
        {
            _logger.LogDebug("Devkit project initialization completed");
            _ = SendProjectInitializationCompleteNotificationAsync().ReportNonFatalErrorAsync();
        }
    }
}
#pragma warning restore RS0030 // Do not used banned APIs
