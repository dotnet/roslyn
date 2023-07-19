// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copy of https://devdiv.visualstudio.com/DevDiv/_git/VS.CloudCache?path=%2Ftest%2FMicrosoft.VisualStudio.Cache.Tests%2FMocks&_a=contents&version=GBmain
// Try to keep in sync and avoid unnecessary changes here.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks
{
    internal class ServiceBrokerMock : IServiceBroker
    {
        public event EventHandler<BrokeredServicesChangedEventArgs>? AvailabilityChanged;

        internal Dictionary<ServiceMoniker, object> BrokeredServices { get; } = new();

        public ValueTask<IDuplexPipe?> GetPipeAsync(ServiceMoniker serviceMoniker, ServiceActivationOptions options = default, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<T?> GetProxyAsync<T>(ServiceRpcDescriptor serviceDescriptor, ServiceActivationOptions options = default, CancellationToken cancellationToken = default)
            where T : class
        {
            if (this.BrokeredServices.TryGetValue(serviceDescriptor.Moniker, out var service))
            {
                return new((T?)service);
            }

            return default;
        }

        internal void OnAvailabilityChanged(BrokeredServicesChangedEventArgs args) => this.AvailabilityChanged?.Invoke(this, args);
    }
}
