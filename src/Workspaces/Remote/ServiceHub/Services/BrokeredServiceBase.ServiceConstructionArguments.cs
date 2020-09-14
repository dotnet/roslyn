// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.Remote
{
    internal abstract partial class BrokeredServiceBase
    {
        internal readonly struct ServiceConstructionArguments
        {
            public readonly IServiceProvider ServiceProvider;
            public readonly IServiceBroker ServiceBroker;
            public readonly CancellationTokenSource ClientDisconnectedSource;

            public ServiceConstructionArguments(IServiceProvider serviceProvider, IServiceBroker serviceBroker, CancellationTokenSource clientDisconnectedSource)
            {
                ServiceProvider = serviceProvider;
                ServiceBroker = serviceBroker;
                ClientDisconnectedSource = clientDisconnectedSource;
            }
        }
    }
}
