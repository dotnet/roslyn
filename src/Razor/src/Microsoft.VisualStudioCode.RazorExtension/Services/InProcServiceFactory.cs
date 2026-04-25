// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

/// <summary>
///  Creates Razor brokered services.
/// </summary>
/// <remarks>
///  This class holds <see cref="RazorBrokeredServiceBase.FactoryBase{TService}"/> instances in a static
///  field. This should work fine in tests, since brokered service factories are intended to be stateless.
///  However, if a factory is introduced that maintains state, this class will need to be revisited to
///  avoid holding onto state across tests.
/// </remarks>
internal static class InProcServiceFactory
{
    private static readonly Dictionary<Type, IInProcServiceFactory> s_factoryMap = BuildFactoryMap();
    private static ExportProvider? _exportProvider;

    private static Dictionary<Type, IInProcServiceFactory> BuildFactoryMap()
    {
        var result = new Dictionary<Type, IInProcServiceFactory>();

        foreach (var type in typeof(RazorBrokeredServiceBase.FactoryBase<>).Assembly.GetTypes())
        {
            if (!type.IsAbstract &&
                typeof(IInProcServiceFactory).IsAssignableFrom(type))
            {
                Assumes.True(typeof(RazorBrokeredServiceBase.FactoryBase<>) == type.BaseType!.GetGenericTypeDefinition());

                var genericType = type.BaseType.GetGenericArguments().Single();

                // ServiceHub requires a parameterless constructor, so we can safely rely on it existing too
                var factory = (IInProcServiceFactory)Activator.CreateInstance(type).AssumeNotNull();
                result.Add(genericType, factory);
            }
        }

        return result;
    }

    public static async Task<TService> CreateServiceAsync<TService>(
        VSCodeBrokeredServiceInterceptor brokeredServiceInterceptor, IWorkspaceProvider workspaceProvider, ILoggerFactory loggerFactory)
        where TService : class
    {
        Assumes.True(s_factoryMap.TryGetValue(typeof(TService), out var factory));

        var brokeredServiceData = new RazorBrokeredServiceData(ExportProvider: _exportProvider, loggerFactory, brokeredServiceInterceptor, workspaceProvider);
        var hostProvidedServices = new HostProvidedServices(brokeredServiceData);

        return (TService)await factory.CreateInProcAsync(hostProvidedServices).ConfigureAwait(false);
    }

    private class HostProvidedServices(RazorBrokeredServiceData brokeredServiceData) : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(RazorBrokeredServiceData) ? brokeredServiceData : null;
    }

    internal static class TestAccessor
    {
        public static void SetExportProvider(ExportProvider exportProvider)
        {
            _exportProvider = exportProvider;
        }
    }
}
