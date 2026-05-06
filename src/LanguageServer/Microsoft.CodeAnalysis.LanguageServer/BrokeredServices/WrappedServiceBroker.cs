// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Pipelines;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;

/// <summary>
/// Implements a wrapper around <see cref="IServiceBroker"/> that will wait for the service broker to be available before invoking the requested method.
/// </summary>
internal sealed class WrappedServiceBroker : IServiceBroker
{
    private readonly Task<IServiceBroker> _serviceBrokerTask;

    public WrappedServiceBroker(Task<IBrokeredServiceContainer> containerTask)
    {
        _serviceBrokerTask = containerTask.ContinueWith(
            t =>
            {
                var serviceBroker = t.Result.GetFullAccessServiceBroker();
                serviceBroker.AvailabilityChanged += (s, e) => AvailabilityChanged?.Invoke(this, e);
                return serviceBroker;
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.RunContinuationsAsynchronously,
            TaskScheduler.Default);
    }

    public event EventHandler<BrokeredServicesChangedEventArgs>? AvailabilityChanged;

    public async ValueTask<IDuplexPipe?> GetPipeAsync(ServiceMoniker serviceMoniker, ServiceActivationOptions options = default, CancellationToken cancellationToken = default)
    {
        var serviceBroker = await _serviceBrokerTask;
        return await serviceBroker.GetPipeAsync(serviceMoniker, options, cancellationToken);
    }

    public async ValueTask<T?> GetProxyAsync<T>(ServiceRpcDescriptor serviceDescriptor, ServiceActivationOptions options = default, CancellationToken cancellationToken = default) where T : class
    {
        var serviceBroker = await _serviceBrokerTask;
#pragma warning disable ISB001 // Dispose of proxies - caller is responsible for disposing the proxy.
        return await serviceBroker.GetProxyAsync<T>(serviceDescriptor, options, cancellationToken);
#pragma warning restore ISB001 // Dispose of proxies
    }
}