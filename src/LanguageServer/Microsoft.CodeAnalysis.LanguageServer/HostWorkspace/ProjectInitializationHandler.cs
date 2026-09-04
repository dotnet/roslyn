// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.Definitions;
using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed class ProjectInitializationHandler : IDisposable
{
    internal const string ProjectInitializationCompleteName = "workspace/projectInitializationComplete";

    private readonly IServiceBroker _serviceBroker;
    private readonly ServiceBrokerClient _serviceBrokerClient;
    private readonly ILogger _logger;
    private readonly TaskCompletionSource _serviceAvailable = new();
    private readonly ProjectInitializationCompleteObserver _projectInitializationCompleteObserver;
    private readonly RoslynTelemetry _telemetry;

    private IDisposable? _subscription;

    public ProjectInitializationHandler(
        IClientLanguageServerManager clientLanguageServerManager,
        IServiceBroker serviceBroker,
        ILoggerFactory loggerFactory,
        VSCodeRequestTelemetryLogger requestTelemetryLogger)
    {
        _serviceBroker = serviceBroker;
        _serviceBroker.AvailabilityChanged += AvailabilityChanged;
        _serviceBrokerClient = new ServiceBrokerClient(_serviceBroker, joinableTaskFactory: null);

        _logger = loggerFactory.CreateLogger<ProjectInitializationHandler>();
        _projectInitializationCompleteObserver = new ProjectInitializationCompleteObserver(
            clientLanguageServerManager, _logger, requestTelemetryLogger);
        _telemetry = RoslynTelemetry.Current;
    }

    public static async ValueTask SendProjectInitializationCompleteNotificationAsync(IClientLanguageServerManager clientLanguageServerManager)
    {
        await clientLanguageServerManager.SendNotificationAsync(ProjectInitializationCompleteName, CancellationToken.None);
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
        // Raised by the service broker, not the LSP queue, so the ambient must be re-established.
        using var _ = RoslynTelemetry.SetCurrent(_telemetry);

        if (e.ImpactedServices.Contains(Descriptors.RemoteProjectInitializationStatusService.Moniker))
            _serviceAvailable.SetResult();
    }

    public void Dispose()
    {
        using var _ = RoslynTelemetry.SetCurrent(_telemetry);

        _serviceBroker.AvailabilityChanged -= AvailabilityChanged;
        _subscription?.Dispose();
        _serviceBrokerClient.Dispose();
    }

    internal sealed class ProjectInitializationCompleteObserver(
        IClientLanguageServerManager clientLanguageServerManager,
        ILogger logger,
        VSCodeRequestTelemetryLogger requestTelemetryLogger) : IObserver<ProjectInitializationCompletionState>
    {
        // These callbacks are dispatched by StreamJsonRpc from Dev Kit, so they carry no ambient.
        private readonly RoslynTelemetry _telemetry = RoslynTelemetry.Current;

        [JsonRpcMethod("onCompleted")]
        public void OnCompleted()
        {
            // NoOp - OnNext is the only method that will be called upon completion of initial project load.
        }

        [JsonRpcMethod("onError", UseSingleObjectParameterDeserialization = true)]
        public void OnError(Exception error)
        {
            using var _ = RoslynTelemetry.SetCurrent(_telemetry);
            logger.LogError(error, "Devkit project initialization observer failed");
        }

        [JsonRpcMethod("onNext", UseSingleObjectParameterDeserialization = true)]
        public void OnNext(ProjectInitializationCompletionState value)
        {
            using var telemetryScope = RoslynTelemetry.SetCurrent(_telemetry);
            logger.LogDebug("Devkit project initialization completed");
            requestTelemetryLogger.ReportProjectInitializationComplete();
            _ = SendProjectInitializationCompleteNotificationAsync(clientLanguageServerManager).AsTask().ReportNonFatalErrorAsync();
        }
    }
}
