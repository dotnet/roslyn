// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LspServices : ILspServices, IMethodHandlerProvider
{
    private readonly FrozenDictionary<string, Lazy<ILspService, LspServiceMetadataView>> _lazyMefLspServices;

    /// <summary>
    /// A set of base services that apply to all Roslyn lsp services.
    /// Unfortunately MEF doesn't provide a good way to export something for multiple contracts with metadata
    /// so these are manually created in <see cref="RoslynLanguageServer"/>.
    /// </summary>
    private readonly FrozenDictionary<string, ImmutableArray<BaseService>> _baseServices;

    /// <summary>
    /// Gates access to <see cref="_servicesToDispose"/>.
    /// </summary>
    private readonly object _gate = new();
    private readonly HashSet<IDisposable> _servicesToDispose = new(ReferenceEqualityComparer.Instance);

    public LspServices(
        ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> mefLspServices,
        ImmutableArray<Lazy<ILspServiceFactory, LspServiceMetadataView>> mefLspServiceFactories,
        WellKnownLspServerKinds serverKind,
        FrozenDictionary<string, ImmutableArray<BaseService>> baseServices)
    {
        var serviceMap = new Dictionary<string, Lazy<ILspService, LspServiceMetadataView>>();

        // Convert MEF exported service factories to the lazy LSP services that they create.
        foreach (var lazyServiceFactory in mefLspServiceFactories)
        {
            var metadata = lazyServiceFactory.Metadata;

            // Make sure that we only include services exported for the specified server kind (or NotSpecified).
            if (metadata.ServerKind == serverKind ||
                metadata.ServerKind == WellKnownLspServerKinds.Any)
            {
                serviceMap.Add(
                    metadata.TypeRef.TypeName,
                    new(() => lazyServiceFactory.Value.CreateILspService(this, serverKind), metadata));
            }
        }

        foreach (var lazyService in mefLspServices)
        {
            var metadata = lazyService.Metadata;

            // Make sure that we only include services exported for the specified server kind (or NotSpecified).
            if (metadata.ServerKind == serverKind ||
                metadata.ServerKind == WellKnownLspServerKinds.Any)
            {
                serviceMap.Add(metadata.TypeRef.TypeName, lazyService);
            }
        }

        _lazyMefLspServices = serviceMap.ToFrozenDictionary();

        _baseServices = baseServices;
    }

    public T GetRequiredService<T>() where T : notnull
    {
        var service = GetService<T>();
        Contract.ThrowIfNull(service, $"Missing required LSP service {typeof(T).FullName}");
        return service;
    }

    public T? GetService<T>() where T : notnull
    {
        return TryGetService(typeof(T), out var service)
            ? (T)service
            : default;
    }

    public IEnumerable<T> GetRequiredServices<T>()
    {
        // We provide this ILspServices instance as a service.
        if (typeof(T) == typeof(ILspServices))
        {
            yield return (T)(object)this;
        }

        foreach (var service in GetBaseServices<T>())
        {
            yield return service;
        }

        foreach (var service in GetMefServices<T>())
        {
            yield return service;
        }
    }

    public bool TryGetService(Type type, [NotNullWhen(true)] out object? service)
    {
        var typeName = type.FullName;
        Contract.ThrowIfNull(typeName);

        service = GetService(typeName);
        return service is not null;
    }

    private object? GetService(string typeName)
    {
        // We provide this ILspServices instance as a service.
        if (typeName == typeof(ILspServices).FullName)
        {
            return this;
        }

        // Check the base services first
        if (_baseServices.TryGetValue(typeName, out var baseServices))
        {
            // It's possible that there may be more than one base service registered for the same type,
            // such as IMethodHandler. If that's the case, we return null.
            return baseServices is [var baseService]
                ? baseService.GetInstance(this)
                : null;
        }

        if (_lazyMefLspServices.TryGetValue(typeName, out var lazyService))
        {
            // If we are creating a stateful LSP service for the first time, we need to check
            // if it is disposable after creation and keep it around to dispose of on shutdown.
            // Stateless LSP services will be disposed of on MEF container disposal.
            var checkDisposal = !lazyService.Metadata.IsStateless && !lazyService.IsValueCreated;

            var lspService = lazyService.Value;
            if (checkDisposal && lspService is IDisposable disposable)
            {
                lock (_gate)
                {
                    _servicesToDispose.Add(disposable);
                }
            }

            return lspService;
        }

        return null;
    }

    public ImmutableArray<(IMethodHandler? Instance, TypeRef HandlerTypeRef, ImmutableArray<MethodHandlerDetails> HandlerDetails)> GetMethodHandlers()
    {
        using var _ = ArrayBuilder<(IMethodHandler?, TypeRef, ImmutableArray<MethodHandlerDetails>)>.GetInstance(out var builder);

        // First, add any IMethodHandlers found in base services.
        foreach (var handler in GetBaseServices<IMethodHandler>())
        {
            var handlerType = handler.GetType();
            var methods = MethodHandlerDetails.From(handlerType);

            builder.Add((handler, TypeRef.From(handlerType), methods));
        }

        // Now, walk through our MEF services and add any IMethodHandlers.
        foreach (var lazyService in _lazyMefLspServices.Values)
        {
            var metadata = lazyService.Metadata;

            if (metadata.HandlerDetails is { } handlerMethods)
            {
                builder.Add((null, metadata.TypeRef, handlerMethods));
            }
        }

        return builder.ToImmutableAndClear();
    }

    private ImmutableArray<T> GetBaseServices<T>()
    {
        var typeName = typeof(T).FullName;
        Contract.ThrowIfNull(typeName);

        return _baseServices.TryGetValue(typeName, out var baseServices)
            ? baseServices.SelectAsArray(s => (T)s.GetInstance(this))
            : [];
    }

    private IEnumerable<T> GetMefServices<T>()
    {
        foreach (var (typeName, lazyService) in _lazyMefLspServices)
        {
            if (lazyService.Metadata.InterfaceNames.Contains(typeof(T).AssemblyQualifiedName!))
            {
                var serviceInstance = GetService(typeName);
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
