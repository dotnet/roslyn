// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.BrokeredServices;

internal abstract class BrokeredServiceProxy<TService>(
    IServiceBroker serviceBroker,
    ServiceRpcDescriptor descriptor,
    ServiceRpcDescriptor? fallbackDescriptor = null) where TService : class
{
    private async ValueTask<TService> GetRequiredServiceAsync(CancellationToken cancellationToken)
    {
#pragma warning disable ISB001 // Dispose of proxies - handled by caller
        var service = await serviceBroker.GetProxyAsync<TService>(descriptor, cancellationToken).ConfigureAwait(false);
        if (service == null && fallbackDescriptor != null)
        {
            service = await serviceBroker.GetProxyAsync<TService>(fallbackDescriptor, cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies
        }

        Contract.ThrowIfNull(service);
        return service;
    }

    protected async ValueTask InvokeAsync(Func<TService, CancellationToken, ValueTask> operation, CancellationToken cancellationToken)
    {
        var service = await GetRequiredServiceAsync(cancellationToken).ConfigureAwait(false);
        using ((IDisposable?)service)
        {
            await operation(service, cancellationToken).ConfigureAwait(false);
        }
    }

    protected async ValueTask<TResult> InvokeAsync<TResult>(Func<TService, CancellationToken, ValueTask<TResult>> operation, CancellationToken cancellationToken)
    {
        var service = await GetRequiredServiceAsync(cancellationToken).ConfigureAwait(false);
        using ((IDisposable?)service)
        {
            return await operation(service, cancellationToken).ConfigureAwait(false);
        }
    }

    protected async ValueTask<TResult> InvokeAsync<TArgs, TResult>(Func<TService, TArgs, CancellationToken, ValueTask<TResult>> operation, TArgs args, CancellationToken cancellationToken)
    {
        var service = await GetRequiredServiceAsync(cancellationToken).ConfigureAwait(false);
        using ((IDisposable?)service)
        {
            return await operation(service, args, cancellationToken).ConfigureAwait(false);
        }
    }

    protected async ValueTask InvokeAsync<TArgs>(Func<TService, TArgs, CancellationToken, ValueTask> operation, TArgs args, CancellationToken cancellationToken)
    {
        var service = await GetRequiredServiceAsync(cancellationToken).ConfigureAwait(false);
        using ((IDisposable?)service)
        {
            await operation(service, args, cancellationToken).ConfigureAwait(false);
        }
    }
}
