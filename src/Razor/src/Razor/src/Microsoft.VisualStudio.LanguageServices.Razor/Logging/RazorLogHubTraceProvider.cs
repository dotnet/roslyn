// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.LogHub;
using Microsoft.VisualStudio.RpcContracts.Logging;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.Logging;

[Export(typeof(RazorLogHubTraceProvider))]
[method: ImportingConstructor]
internal class RazorLogHubTraceProvider(
    IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> brokeredServiceContainer,
    JoinableTaskContext joinableTaskContext)
{
    private static readonly LoggerOptions s_logOptions = new(
        requestedLoggingLevel: new LoggingLevelSettings(SourceLevels.Information | SourceLevels.ActivityTracing),
        privacySetting: PrivacyFlags.MayContainPersonallyIdentifibleInformation | PrivacyFlags.MayContainPrivateInformation);

    private readonly IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> _brokeredServiceContainer = brokeredServiceContainer;
    private readonly ReentrantSemaphore _initializationSemaphore = ReentrantSemaphore.Create(
        initialCount: 1,
        joinableTaskContext,
        ReentrantSemaphore.ReentrancyMode.NotAllowed);

    private IServiceBroker? _serviceBroker = null;
    private TraceSource? _traceSource;

    public async Task InitializeTraceAsync(string logIdentifier, int logHubSessionId, CancellationToken cancellationToken)
    {
        var serviceBrokerInitialized = await TryInitializeServiceBrokerAsync(cancellationToken).ConfigureAwait(false);
        if (!serviceBrokerInitialized)
        {
            return;
        }

        var serviceBroker = _serviceBroker.AssumeNotNull();

        var logId = new LogId(
            logName: $"{logIdentifier}.{logHubSessionId}",
            serviceId: new ServiceMoniker($"Razor.{logIdentifier}"));

        using var traceConfig = await TraceConfiguration
            .CreateTraceConfigurationInstanceAsync(serviceBroker, ownsServiceBroker: true, cancellationToken)
            .ConfigureAwait(false);

        _traceSource = await traceConfig.RegisterLogSourceAsync(logId, s_logOptions, cancellationToken).ConfigureAwait(false);
    }

    public bool TryGetTraceSource([NotNullWhen(true)] out TraceSource? traceSource)
    {
        traceSource = _traceSource;
        return traceSource is not null;
    }

    private async Task<bool> TryInitializeServiceBrokerAsync(CancellationToken cancellationToken)
    {
        // Check if the service broker has already been initialized
        if (_serviceBroker is not null)
        {
            return true;
        }

        await _initializationSemaphore.ExecuteAsync(async () =>
        {
            if (_serviceBroker is null &&
                await _brokeredServiceContainer.GetValueOrNullAsync(cancellationToken) is IBrokeredServiceContainer serviceContainer)
            {
                _serviceBroker = serviceContainer.GetFullAccessServiceBroker();
            }
        },
        cancellationToken);

        return _serviceBroker is not null;
    }
}
