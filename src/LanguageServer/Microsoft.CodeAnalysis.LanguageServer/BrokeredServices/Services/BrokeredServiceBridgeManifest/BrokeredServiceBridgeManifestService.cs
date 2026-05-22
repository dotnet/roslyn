// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.BrokeredServiceBridgeManifest;

internal sealed class BrokeredServiceBridgeManifest : IBrokeredServiceBridgeManifest
{
    internal const string MonikerName = "Microsoft.VisualStudio.Server.IBrokeredServiceBridgeManifest";
    internal const string MonikerVersion = "0.1";
    private static readonly ServiceMoniker s_serviceMoniker = new(MonikerName, new Version(MonikerVersion));

    internal static readonly ServiceRpcDescriptor ServiceDescriptor = new ServiceJsonRpcDescriptor(
        s_serviceMoniker,
        ServiceJsonRpcDescriptor.Formatters.UTF8,
        ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders);

    private readonly BrokeredServiceContainer _container;
    private readonly ILogger _logger;

    public BrokeredServiceBridgeManifest(BrokeredServiceContainer container, ILoggerFactory loggerFactory)
    {
        _container = container;
        _logger = loggerFactory.CreateLogger<BrokeredServiceBridgeManifest>();
    }

    /// <summary>
    /// Returns a subset of services registered to Microsoft.VisualStudio.Code.Server container that are proferred by the Language Server process.
    /// </summary>
    public ValueTask<IReadOnlyCollection<ServiceMoniker>> GetAvailableServicesAsync(CancellationToken cancellationToken)
    {

        var services = (IReadOnlyCollection<ServiceMoniker>)[.. _container.GetRegisteredServices()
            .Select(s => s.Key)
            .Where(s => s.Name.StartsWith("Microsoft.CodeAnalysis.LanguageServer.", StringComparison.Ordinal) ||
                        s.Name.StartsWith("Microsoft.VisualStudio.LanguageServer.", StringComparison.Ordinal) ||
                        s.Name.StartsWith("Microsoft.VisualStudio.LanguageServices.", StringComparison.Ordinal))];
        _logger.LogDebug($"Proffered services: {string.Join(',', services.Select(s => s.ToString()))}");
        return new ValueTask<IReadOnlyCollection<ServiceMoniker>>(services);
    }
}
