// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.Remote
{
    internal abstract partial class BrokeredServiceBase
    {
        internal readonly struct ServiceConstructionArguments
        {
            public readonly IServiceProvider ServiceProvider;
            public readonly IServiceBroker ServiceBroker;

            public ServiceConstructionArguments(IServiceProvider serviceProvider, IServiceBroker serviceBroker)
            {
                ServiceProvider = serviceProvider;
                ServiceBroker = serviceBroker;
            }
        }
    }
}
