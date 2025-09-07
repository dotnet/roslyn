// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.IO.Pipelines;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
#pragma warning restore RS0030 // Do not used banned APIs

/// <summary>
/// Implements a wrapper around <see cref="IServiceBroker"/> that allows clients to MEF import the service broker.
/// This wrapper will wait for the service broker to be available before invoking the requested method.
/// </summary>
[Export(typeof(SVsFullAccessServiceBroker)), Shared]
[Export(typeof(WrappedServiceBroker))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WrappedServiceBroker() : IServiceBroker
{
    private readonly TaskCompletionSource<IServiceBroker> _serviceBrokerTask = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal void SetServiceBroker(IServiceBroker serviceBroker)
    {
        Contract.ThrowIfTrue(_serviceBrokerTask.Task.IsCompleted);
        serviceBroker.AvailabilityChanged += (s, e) => AvailabilityChanged?.Invoke(this, e);
        _serviceBrokerTask.SetResult(serviceBroker);
    }

    public event EventHandler<BrokeredServicesChangedEventArgs>? AvailabilityChanged;

    public async ValueTask<IDuplexPipe?> GetPipeAsync(ServiceMoniker serviceMoniker, ServiceActivationOptions options = default, CancellationToken cancellationToken = default)
    {
        var serviceBroker = await _serviceBrokerTask.Task;
        return await serviceBroker.GetPipeAsync(serviceMoniker, options, cancellationToken);
    }

    public async ValueTask<T?> GetProxyAsync<T>(ServiceRpcDescriptor serviceDescriptor, ServiceActivationOptions options = default, CancellationToken cancellationToken = default) where T : class
    {
        var serviceBroker = await _serviceBrokerTask.Task;
#pragma warning disable ISB001 // Dispose of proxies - caller is responsible for disposing the proxy.
        return await serviceBroker.GetProxyAsync<T>(serviceDescriptor, options, cancellationToken);
#pragma warning restore ISB001 // Dispose of proxies
    }
}
