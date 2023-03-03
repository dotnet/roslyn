// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.HelloWorld;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Utilities.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services;

internal class Descriptors
{
    // Descriptors for remote services.
    // If adding services here, make sure to update RemoteServicesToRegister.

    public static readonly ServiceRpcDescriptor RemoteHelloWorldService = CreateDescriptor(new("helloServiceHubDotNetHost", new Version("0.1")));

    // Descriptors for local services.

    public static readonly ServiceRpcDescriptor LocalHelloWorldService = CreateDescriptor(new(HelloWorldService.MonikerName, new Version(HelloWorldService.MonikerVersion)));

    /// <summary>
    /// The set of remote services that we register to our container.
    /// </summary>
    /// <remarks>
    /// Note that while today we only support static registration of services in the remote process it would be possible to implement dynamic registration
    /// if we read the remote brokered service manifest.
    /// </remarks>
    public static ImmutableDictionary<ServiceMoniker, ServiceRegistration> RemoteServicesToRegister = new Dictionary<ServiceMoniker, ServiceRegistration>
    {
        { Descriptors.RemoteHelloWorldService.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
    }.ToImmutableDictionary();

    public static ServiceJsonRpcDescriptor CreateDescriptor(ServiceMoniker serviceMoniker) => new(
        serviceMoniker,
        ServiceJsonRpcDescriptor.Formatters.UTF8,
        ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders);
}
