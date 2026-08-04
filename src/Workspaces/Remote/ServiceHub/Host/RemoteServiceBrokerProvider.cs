// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.Remote.Host;

/// <summary>
/// Exposes a <see cref="IServiceBroker"/> to services that expect there to be a global singleton.
/// The first remote service that gets called into will record its broker here.
/// </summary>
[ExportWorkspaceService(typeof(IServiceBrokerProvider), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RemoteServiceBrokerProvider() : IServiceBrokerProvider
{
    private static IServiceBroker? s_instance;

    public static void RegisterServiceBroker(IServiceBroker serviceBroker)
    {
        Interlocked.CompareExchange(ref s_instance, serviceBroker, null);
    }

    public IServiceBroker ServiceBroker
    {
        get
        {
            var broker = Volatile.Read(ref s_instance);
            Contract.ThrowIfNull(broker, "Global service broker not registered");
            return broker;
        }
    }
}
