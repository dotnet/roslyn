// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.HelloWorld;

#pragma warning disable RS0030 // This is intentionally using System.ComponentModel.Composition for compatibility with MEF service broker.
[Export]
internal class RemoteHelloWorldProvider
{
    private readonly IServiceBroker _serviceBroker;
    private readonly TaskCompletionSource _serviceAvailable = new();
    private readonly ILogger _logger;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RemoteHelloWorldProvider([Import(typeof(SVsFullAccessServiceBroker))] IServiceBroker serviceBroker, ILoggerFactory loggerFactory)
    {
        _serviceBroker = serviceBroker;
        _logger = loggerFactory.CreateLogger<RemoteHelloWorldProvider>();

        _serviceBroker.AvailabilityChanged += ServiceBroker_AvailabilityChanged;
    }

    public async Task SayHelloToRemoteServerAsync(CancellationToken cancellationToken)
    {
        var response = await TryGetHelloWorldAsync(cancellationToken);
        if (!response)
        {
            await _serviceAvailable.Task;
            Contract.ThrowIfFalse(await TryGetHelloWorldAsync(cancellationToken), "Was not able to get hello world response from remote");
        }
    }

    private void ServiceBroker_AvailabilityChanged(object? sender, BrokeredServicesChangedEventArgs e)
    {
        if (e.ImpactedServices.Contains(Descriptors.RemoteHelloWorldService.Moniker))
            _serviceAvailable.SetResult();
    }

    private async Task<bool> TryGetHelloWorldAsync(CancellationToken cancellationToken)
    {
        var helloWorldService = await _serviceBroker.GetProxyAsync<IHelloWorld>(Descriptors.RemoteHelloWorldService, cancellationToken);
        using (helloWorldService as IDisposable)
        {
            if (helloWorldService is not null)
            {
                try
                {
                    var response = await helloWorldService.SayHelloAsync("C#", cancellationToken);
                    _logger.LogDebug("Response from remote: " + response);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Got exception when invoking callback function:{ex}");
                }
            }
        }

        return false;
    }
}

#pragma warning restore RS0030 // Do not used banned APIs
