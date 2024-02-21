// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LspServices : ILspServices
{
    private readonly ImmutableDictionary<Type, Lazy<ILspService, LspServiceMetadataView>> _lazyMefLspServices;

    /// <summary>
    /// A set of base services that apply to all roslyn lsp services.
    /// Unfortunately MEF doesn't provide a good way to export something for multiple contracts with metadata
    /// so these are manually created in <see cref="RoslynLanguageServer"/>.
    /// TODO - cleanup once https://github.com/dotnet/roslyn/issues/63555 is resolved.
    /// </summary>
    private readonly ImmutableDictionary<Type, ImmutableArray<Func<ILspServices, object>>> _baseServices;

    /// <summary>
    /// Gates access to <see cref="_servicesToDispose"/>.
    /// </summary>
    private readonly object _gate = new();
    private readonly HashSet<IDisposable> _servicesToDispose = new(ReferenceEqualityComparer.Instance);

    public LspServices(
        ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> mefLspServices,
        ImmutableArray<Lazy<ILspServiceFactory, LspServiceMetadataView>> mefLspServiceFactories,
        WellKnownLspServerKinds serverKind,
        ImmutableDictionary<Type, ImmutableArray<Func<ILspServices, object>>> baseServices)
    {
        // Convert MEF exported service factories to the lazy LSP services that they create.
        var servicesFromFactories = mefLspServiceFactories.Select(lz => new Lazy<ILspService, LspServiceMetadataView>(() => lz.Value.CreateILspService(this, serverKind), lz.Metadata));

        var services = mefLspServices.Concat(servicesFromFactories);

        // Make sure that we only include services exported for the specified server kind (or NotSpecified).
        services = services.Where(lazyService => lazyService.Metadata.ServerKind == serverKind || lazyService.Metadata.ServerKind == WellKnownLspServerKinds.Any);

        // This will throw if the same service is registered twice
        _lazyMefLspServices = services.ToImmutableDictionary(lazyService => lazyService.Metadata.Type, lazyService => lazyService);

        // Bit cheaky, but lets make an this ILspService available on the base services to make constructors that take an ILspServices instance possible.
        _baseServices = baseServices.Add(typeof(ILspServices), ImmutableArray.Create<Func<ILspServices, object>>((_) => this));
    }

    public T GetRequiredService<T>() where T : notnull
    {
        var service = GetService<T>();
        Contract.ThrowIfNull(service, $"Missing required LSP service {typeof(T).FullName}");
        return service;
    }

    public T? GetService<T>()
    {
        T? service;

        // Check the base services first
        service = GetBaseServices<T>().SingleOrDefault();
        service ??= (T?)TryGetService(typeof(T));

        return service;
    }

    public IEnumerable<T> GetRequiredServices<T>()
    {
        var baseServices = GetBaseServices<T>();
        var mefServices = GetMefServices<T>();

        return baseServices != null ? mefServices.Concat(baseServices) : mefServices;
    }

    public object? TryGetService(Type type)
    {
        object? lspService;
        if (_lazyMefLspServices.TryGetValue(type, out var lazyService))
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

    public ImmutableArray<Type> GetRegisteredServices()
        => _lazyMefLspServices.Keys.ToImmutableArray();

    public bool SupportsGetRegisteredServices()
    {
        return true;
    }

    private IEnumerable<T> GetBaseServices<T>()
        => _baseServices.TryGetValue(typeof(T), out var baseServices)
            ? baseServices.Select(creatorFunc => (T)creatorFunc(this)).ToImmutableArray()
            : (IEnumerable<T>)ImmutableArray<T>.Empty;

    private IEnumerable<T> GetMefServices<T>()
    {
        if (typeof(T) == typeof(IMethodHandler))
        {
            // HACK: There is special handling for the IMethodHandler to make sure that its types remain lazy
            // Special case this to avoid providing them twice.
            yield break;
        }

        var allServices = GetRegisteredServices();
        foreach (var service in allServices)
        {
            var @interface = service.GetInterface(typeof(T).Name);
            if (@interface is not null)
            {
                var instance = TryGetService(service);
                if (instance is not null)
                {
                    yield return (T)instance;
                }
                else
                {
                    throw new Exception("Service failed to construct");
                }
            }
        }
    }

    public void Dispose()
    {
        ImmutableArray<IDisposable> disposableServices;
        lock (_gate)
        {
            disposableServices = _servicesToDispose.ToImmutableArray();
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
