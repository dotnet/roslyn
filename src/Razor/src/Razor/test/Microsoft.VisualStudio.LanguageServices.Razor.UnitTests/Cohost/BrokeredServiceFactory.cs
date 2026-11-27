// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.VisualStudio;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Composition;
using Nerdbank.Streams;
using Xunit;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

/// <summary>
///  Creates Razor brokered services.
/// </summary>
/// <remarks>
///  This class holds <see cref="RazorBrokeredServiceBase.FactoryBase{TService}"/> instances in a static
///  field. This should work fine in tests, since brokered service factories are intended to be stateless.
///  However, if a factory is introduced that maintains state, this class will need to be revisited to
///  avoid holding onto state across tests.
/// </remarks>
internal static class BrokeredServiceFactory
{
    private static readonly Dictionary<Type, IServiceHubServiceFactory> s_factoryMap = BuildFactoryMap();

    private static Dictionary<Type, IServiceHubServiceFactory> BuildFactoryMap()
    {
        var result = new Dictionary<Type, IServiceHubServiceFactory>();

        foreach (var type in typeof(RazorBrokeredServiceBase.FactoryBase<>).Assembly.GetTypes())
        {
            if (!type.IsAbstract &&
                typeof(IServiceHubServiceFactory).IsAssignableFrom(type))
            {
                Assert.Equal(typeof(RazorBrokeredServiceBase.FactoryBase<>), type.BaseType.GetGenericTypeDefinition());

                var genericType = type.BaseType.GetGenericArguments().Single();

                // ServiceHub requires a parameterless constructor, so we can safely rely on it existing too
                var factory = (IServiceHubServiceFactory)Activator.CreateInstance(type);
                result.Add(genericType, factory);
            }
        }

        return result;
    }

    public static async Task<TService> CreateServiceAsync<TService>(
        TestBrokeredServiceInterceptor brokeredServiceInterceptor, ExportProvider exportProvider, ILoggerFactory loggerFactory)
        where TService : class
    {
        Assert.True(s_factoryMap.TryGetValue(typeof(TService), out var factory));

        var (clientStream, serverStream) = FullDuplexStream.CreatePair();
        var brokeredServiceData = new RazorBrokeredServiceData(exportProvider, loggerFactory, brokeredServiceInterceptor, WorkspaceProvider: null);
        var hostProvidedServices = VsMocks.CreateServiceProvider(b =>
        {
            b.AddService(brokeredServiceData);

            // Don't provide a trace source. Brokered services and MEF components will rely on ILoggerFactory.
            b.AddService<TraceSource>((object?)null);
        });

        return (TService)await factory.CreateAsync(
            serverStream,
            hostProvidedServices,
            serviceActivationOptions: default,
            StrictMock.Of<IServiceBroker>(),
            authorizationServiceClient: default!);
    }
}
