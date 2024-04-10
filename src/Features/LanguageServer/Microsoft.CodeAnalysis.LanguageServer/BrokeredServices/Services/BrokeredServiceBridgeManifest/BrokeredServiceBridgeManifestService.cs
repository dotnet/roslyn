// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.BrokeredServiceBridgeManifest;

#pragma warning disable RS0030 // This is intentionally using System.ComponentModel.Composition for compatibility with MEF service broker.
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

    private readonly ServiceBrokerFactory _serviceBrokerFactory;
    private readonly ILogger _logger;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public BrokeredServiceBridgeManifest(ServiceBrokerFactory serviceBrokerFactory, ILoggerFactory loggerFactory)
    {
        _serviceBrokerFactory = serviceBrokerFactory;
        _logger = loggerFactory.CreateLogger<BrokeredServiceBridgeManifest>();
    }

    public ServiceRpcDescriptor Descriptor => s_serviceDescriptor;

    /// <summary>
    /// Returns a subset of services registered to Microsoft.VisualStudio.Code.Server container that are proferred by the Language Server process.
    /// </summary>
    public ValueTask<IReadOnlyCollection<ServiceMoniker>> GetAvailableServicesAsync(CancellationToken cancellationToken)
    {
        var services = (IReadOnlyCollection<ServiceMoniker>)_serviceBrokerFactory.GetRequiredServiceBrokerContainer().GetRegisteredServices()
            .Select(s => s.Key)
            .Where(s => s.Name.StartsWith("Microsoft.CodeAnalysis.LanguageServer.", StringComparison.Ordinal) ||
                        s.Name.StartsWith("Microsoft.VisualStudio.LanguageServer.", StringComparison.Ordinal) ||
                        s.Name.StartsWith("Microsoft.VisualStudio.LanguageServices.", StringComparison.Ordinal))
            .ToImmutableArray();
        _logger.LogDebug($"Proffered services: {string.Join(',', services.Select(s => s.ToString()))}");
        return ValueTask.FromResult(services);
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
#pragma warning restore RS0030 // Do not used banned APIs
