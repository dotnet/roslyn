// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Utilities.ServiceBroker;
using Nerdbank.Streams;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services;

internal class Descriptors
{
    // Descriptors for remote services.
    // If adding services here, make sure to update RemoteServicesToRegister.

    public static readonly ServiceRpcDescriptor RemoteModelService = CreateDescriptor(new("vs-intellicode-base-models", new Version("0.1")));

    /// <summary>
    /// See https://devdiv.visualstudio.com/DevDiv/_git/CPS?path=/src/Microsoft.VisualStudio.ProjectSystem.Server/BrokerServices/ProjectInitializationStatusServiceDescriptor.cs
    /// </summary>
    public static readonly ServiceRpcDescriptor RemoteProjectInitializationStatusService = new ServiceJsonRpcDescriptor(
        new("Microsoft.VisualStudio.ProjectSystem.ProjectInitializationStatusService", new Version(0, 1)),
        clientInterface: null,
        ServiceJsonRpcDescriptor.Formatters.MessagePack,
        ServiceJsonRpcDescriptor.MessageDelimiters.BigEndianInt32LengthHeader,
        new MultiplexingStream.Options { ProtocolMajorVersion = 3 });

    // Descriptors for local services.

    /// <summary>
    /// The set of remote services that we register to our container.
    /// </summary>
    /// <remarks>
    /// Note that while today we only support static registration of services in the remote process it would be possible to implement dynamic registration
    /// if we read the remote brokered service manifest.
    /// </remarks>
    public static ImmutableDictionary<ServiceMoniker, ServiceRegistration> RemoteServicesToRegister = new Dictionary<ServiceMoniker, ServiceRegistration>
    {
        { RemoteModelService.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
        { RemoteProjectInitializationStatusService.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
        { BrokeredServiceDescriptors.SolutionSnapshotProvider.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
        { BrokeredServiceDescriptors.DebuggerManagedHotReloadService.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
        { BrokeredServiceDescriptors.HotReloadSessionNotificationService.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
        { BrokeredServiceDescriptors.ManagedHotReloadAgentManagerService.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
        { BrokeredServiceDescriptors.GenericHotReloadAgentManagerService.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
        { BrokeredServiceDescriptors.HotReloadOptionService.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
        { BrokeredServiceDescriptors.HotReloadLoggerService.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
        { BrokeredServiceDescriptors.MauiLaunchCustomizerService.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
        { BrokeredServiceDescriptors.DebuggerSymbolLocatorService.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
        { BrokeredServiceDescriptors.DebuggerSourceLinkService.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
    }.ToImmutableDictionary();

    public static ServiceJsonRpcDescriptor CreateDescriptor(ServiceMoniker serviceMoniker) => new(
        serviceMoniker,
        ServiceJsonRpcDescriptor.Formatters.UTF8,
        ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders);
}
