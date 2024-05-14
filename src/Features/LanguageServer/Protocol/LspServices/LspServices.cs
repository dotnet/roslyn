// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LspServices : ILspServices, IMethodHandlerProvider
{
    private readonly ImmutableDictionary<string, Lazy<ILspService, LspServiceMetadataView>> _lazyMefLspServices;

    /// <summary>
    /// A set of base services that apply to all roslyn lsp services.
    /// Unfortunately MEF doesn't provide a good way to export something for multiple contracts with metadata
    /// so these are manually created in <see cref="RoslynLanguageServer"/>.
    /// </summary>
    private readonly ImmutableDictionary<string, ImmutableArray<Func<ILspServices, object>>> _baseServices;

    /// <summary>
    /// Gates access to <see cref="_servicesToDispose"/>.
    /// </summary>
    private readonly object _gate = new();
    private readonly HashSet<IDisposable> _servicesToDispose = new(ReferenceEqualityComparer.Instance);

    public LspServices(
        ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> mefLspServices,
        ImmutableArray<Lazy<ILspServiceFactory, LspServiceMetadataView>> mefLspServiceFactories,
        WellKnownLspServerKinds serverKind,
        ImmutableDictionary<string, ImmutableArray<Func<ILspServices, object>>> baseServices)
    {
        // Convert MEF exported service factories to the lazy LSP services that they create.
        var servicesFromFactories = mefLspServiceFactories.Select(lz => new Lazy<ILspService, LspServiceMetadataView>(() => lz.Value.CreateILspService(this, serverKind), lz.Metadata));

        var services = mefLspServices.Concat(servicesFromFactories);

        // Make sure that we only include services exported for the specified server kind (or NotSpecified).
        services = services.Where(lazyService => lazyService.Metadata.ServerKind == serverKind || lazyService.Metadata.ServerKind == WellKnownLspServerKinds.Any);

        // This will throw if the same service is registered twice
        _lazyMefLspServices = services.ToImmutableDictionary(lazyService => lazyService.Metadata.TypeRef.TypeName, lazyService => lazyService);

        // Bit cheeky, but lets make an this ILspService available on the base services to make constructors that take an ILspServices instance possible.
        var lspServicesTypeName = typeof(ILspServices).AssemblyQualifiedName;
        Contract.ThrowIfNull(lspServicesTypeName);
        _baseServices = baseServices.Add(lspServicesTypeName, [(_) => this]);
    }

    public T GetRequiredService<T>() where T : notnull
    {
        var service = GetService<T>();
        Contract.ThrowIfNull(service, $"Missing required LSP service {typeof(T).FullName}");
        return service;
    }

    public T? GetService<T>() where T : notnull
        => (T?)TryGetService(typeof(T));

    public IEnumerable<T> GetRequiredServices<T>()
    {
        var baseServices = GetBaseServices<T>();
        var mefServices = GetMefServices<T>();

        return baseServices != null ? mefServices.Concat(baseServices) : mefServices;
    }

    public object? TryGetService(Type type)
    {
        var typeName = type.AssemblyQualifiedName;
        Contract.ThrowIfNull(typeName);

        return TryGetService(typeName);
    }

    private object? TryGetService(string typeName)
    {
        object? lspService;

        // Check the base services first
        if (_baseServices.TryGetValue(typeName, out var baseServices))
        {
            lspService = baseServices.Select(creatorFunc => creatorFunc(this)).SingleOrDefault();
            if (lspService is not null)
            {
                return lspService;
            }
        }

        if (_lazyMefLspServices.TryGetValue(typeName, out var lazyService))
        {
            // If we are creating a stateful LSP service for the first time, we need to check
            // if it is disposable after creation and keep it around to dispose of on shutdown.
            // Stateless LSP services will be disposed of on MEF container disposal.
            var checkDisposal = !lazyService.Metadata.IsStateless && !lazyService.IsValueCreated;

            lspService = lazyService.Value;
            if (checkDisposal && lspService is IDisposable disposable)
            {
                lock (_gate)
                {
                    var res = _servicesToDispose.Add(disposable);
                }
            }

            return lspService;
        }

        lspService = null;
        return lspService;
    }

    public ImmutableArray<(TypeRef HandlerTypeRef, ImmutableArray<HandlerMethodDetails> Methods)> GetMethodHandlers()
    {
        using var _ = ArrayBuilder<(TypeRef, ImmutableArray<HandlerMethodDetails>)>.GetInstance(out var builder);

        foreach (var lazyService in _lazyMefLspServices.Values)
        {
            var metadata = lazyService.Metadata;

            if (metadata.HandlerMethods is { } handlerMethods)
            {
                builder.Add((metadata.TypeRef, handlerMethods));
            }
        }

        return builder.ToImmutableAndClear();
    }

    private IEnumerable<T> GetBaseServices<T>()
    {
        var typeName = typeof(T).AssemblyQualifiedName;
        Contract.ThrowIfNull(typeName);

        return _baseServices.TryGetValue(typeName, out var baseServices)
            ? baseServices.SelectAsArray(creatorFunc => (T)creatorFunc(this))
            : (IEnumerable<T>)[];
    }

    private IEnumerable<T> GetMefServices<T>()
    {
        if (typeof(T) == typeof(IMethodHandler))
        {
            // HACK: There is special handling for the IMethodHandler to make sure that its types remain lazy
            // Special case this to avoid providing them twice.
            yield break;
        }

        // Note: This will realize all of the registered services, which will potentially load assemblies.

        foreach (var (typeName, lazyService) in _lazyMefLspServices)
        {
            var serviceType = lazyService.Metadata.TypeRef.GetResolvedType();
            var interfaceType = serviceType.GetInterface(typeof(T).Name);

            if (interfaceType is not null)
            {
                var serviceInstance = TryGetService(typeName);
                if (serviceInstance is not null)
                {
                    yield return (T)serviceInstance;
                }
                else
                {
                    throw new InvalidOperationException($"Could not construct service: {typeName}");
                }
            }
        }
    }

    public void Dispose()
    {
        ImmutableArray<IDisposable> disposableServices;
        lock (_gate)
        {
            disposableServices = [.. _servicesToDispose];
            _servicesToDispose.Clear();
        }

        foreach (var disposableService in disposableServices)
        {
            try
            {
                disposableService.Dispose();
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
            }
        }
    }
}
