// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.BrokeredServiceBridgeManifest;

[ExportBrokeredService(MonikerName, MonikerVersion, Audience = ServiceAudience.Local)]
internal class BrokeredServiceBridgeManifest : IBrokeredServiceBridgeManifest, IExportedBrokeredService
{
    internal const string MonikerName = "Microsoft.VisualStudio.Server.IBrokeredServiceBridgeManifest";
    internal const string MonikerVersion = "0.1";
    private static readonly ServiceMoniker s_serviceMoniker = new ServiceMoniker(MonikerName, new Version(MonikerVersion));
    private static readonly ServiceRpcDescriptor s_serviceDescriptor = new ServiceJsonRpcDescriptor(
        s_serviceMoniker,
        ServiceJsonRpcDescriptor.Formatters.UTF8,
        ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders);

    private readonly BrokeredServiceContainer _container;

    [ImportingConstructor]
    public BrokeredServiceBridgeManifest([Import("PrivateBrokeredServiceContainer")] BrokeredServiceContainer container)
    {
        _container = container;
    }

    public ServiceRpcDescriptor Descriptor => s_serviceDescriptor;

    public ValueTask<IReadOnlyCollection<ServiceMoniker>> GetAvailableServicesAsync(CancellationToken cancellationToken)
    {
        return ValueTask.FromResult((IReadOnlyCollection<ServiceMoniker>)_container.GetRegisteredServices()
            .Select(s => s.Key)
            .Where(s => s.Name.StartsWith("Microsoft.CodeAnalysis.LanguageServer.", StringComparison.Ordinal) ||
                        s.Name.StartsWith("Microsoft.VisualStudio.LanguageServices.", StringComparison.Ordinal))
            .ToImmutableArray());
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
