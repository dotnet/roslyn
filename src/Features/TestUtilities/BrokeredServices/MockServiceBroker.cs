// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.ServiceHub.Framework;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BrokeredServices.UnitTests;

internal class MockServiceBroker : IServiceBroker
{
    public Func<Type, object>? CreateService;

#pragma warning disable CS0067 // The event 'MockServiceBroker.AvailabilityChanged' is never used
    public event EventHandler<BrokeredServicesChangedEventArgs>? AvailabilityChanged;
#pragma warning restore

    public ValueTask<IDuplexPipe?> GetPipeAsync(ServiceMoniker serviceMoniker, ServiceActivationOptions options = default, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public ValueTask<T?> GetProxyAsync<T>(ServiceRpcDescriptor serviceDescriptor, ServiceActivationOptions options = default, CancellationToken cancellationToken = default) where T : class
        => ValueTaskFactory.FromResult((T?)(CreateService ?? throw new NotImplementedException()).Invoke(typeof(T)));
}
