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

internal sealed class Descriptors
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

    private static readonly ServiceRegistration s_serviceRegistration = new(ServiceAudience.Local, profferingPackageId: null, allowGuestClients: false);

    /// <summary>
    /// The set of remote services that we register to our container.
    /// </summary>
    /// <remarks>
    /// Note that while today we only support static registration of services in the remote process it would be possible to implement dynamic registration
    /// if we read the remote brokered service manifest.
    /// </remarks>
    public static ImmutableDictionary<ServiceMoniker, ServiceRegistration> RemoteServicesToRegister = new Dictionary<ServiceMoniker, ServiceRegistration>
    {
        { RemoteModelService.Moniker, s_serviceRegistration },
        { RemoteProjectInitializationStatusService.Moniker, s_serviceRegistration },
        { BrokeredServiceDescriptors.SolutionSnapshotProvider.Moniker, s_serviceRegistration },

        // Hot Reload services:
        { new("Microsoft.VisualStudio.HotReload.HotReloadEventSubscriber", new Version(3, 0)), s_serviceRegistration },
        { new("Microsoft.VisualStudio.HotReload.ManagedHotReloadUpdatesProviderRegistration", new Version(3, 0)), s_serviceRegistration },

        // TODO: https://github.com/dotnet/roslyn/issues/84158
        // Registered so the XAML diagnostics component in the C# extension for VS Code can call them.
        { new("Microsoft.VisualStudio.HotReload.ProcessTrackingService", new Version(3, 0)), s_serviceRegistration },
        { new("Microsoft.VisualStudio.HotReload.ProjectHotReloadSession", new Version(3, 0)), s_serviceRegistration },
        { new("Microsoft.VisualStudio.Debugger.HotReloadOptionService", new(0, 1)), s_serviceRegistration },
        { new("Microsoft.VisualStudio.Maui.MauiLaunchCustomizerService", new(0, 1)), s_serviceRegistration },
        { new("Microsoft.VisualStudio.WebTools.CssVisualDiagnosticsService", new(0, 1)), s_serviceRegistration },

        { BrokeredServiceDescriptors.HotReloadLoggerServiceLegacy.Moniker, s_serviceRegistration },
        { BrokeredServiceDescriptors.DebuggerSymbolLocatorService.Moniker, s_serviceRegistration },
        { BrokeredServiceDescriptors.DebuggerSourceLinkService.Moniker, s_serviceRegistration },
        { BrokeredServiceDescriptors.ProjectSystemQueryExecutionService.Moniker, s_serviceRegistration },
    }.ToImmutableDictionary();

    public static ServiceJsonRpcDescriptor CreateDescriptor(ServiceMoniker serviceMoniker) => new(
        serviceMoniker,
        ServiceJsonRpcDescriptor.Formatters.UTF8,
        ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders);
}
