// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Collections;
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
    private readonly FrozenDictionary<string, ImmutableArray<Func<ILspServices, object>>> _baseServices;

    /// <summary>
    /// Gates access to <see cref="_servicesToDispose"/>.
    /// </summary>
    private readonly object _gate = new();
    private readonly HashSet<IDisposable> _servicesToDispose = new(ReferenceEqualityComparer.Instance);

    public LspServices(
        ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> mefLspServices,
        ImmutableArray<Lazy<ILspServiceFactory, LspServiceMetadataView>> mefLspServiceFactories,
        WellKnownLspServerKinds serverKind,
        FrozenDictionary<string, ImmutableArray<Func<ILspServices, object>>> baseServices)
    {
        var serviceMap = new Dictionary<string, Lazy<ILspService, LspServiceMetadataView>>();

        // Convert MEF exported service factories to the lazy LSP services that they create.
        foreach (var lazyServiceFactory in mefLspServiceFactories)
        {
            serviceMap.Add(
                lazyServiceFactory.Metadata.TypeRef.TypeName,
                new(() => lazyServiceFactory.Value.CreateILspService(this, serverKind), lazyServiceFactory.Metadata));
        }

        foreach (var lazyService in mefLspServices)
        {
            // Make sure that we only include services exported for the specified server kind (or NotSpecified).
            if (lazyService.Metadata.ServerKind != serverKind &&
                lazyService.Metadata.ServerKind != WellKnownLspServerKinds.Any)
            {
                continue;
            }

            serviceMap.Add(lazyService.Metadata.TypeRef.TypeName, lazyService);
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
        => (T?)TryGetService(typeof(T));

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

    public object? TryGetService(Type type)
    {
        var typeName = type.AssemblyQualifiedName;
        Contract.ThrowIfNull(typeName);

        return TryGetService(typeName);
    }

    private object? TryGetService(string typeName)
    {
        // We provide this ILspServices instance as a service.
        if (typeName == typeof(ILspServices).AssemblyQualifiedName)
        {
            return this;
        }

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
        using var builder = new TemporaryArray<(TypeRef, ImmutableArray<HandlerMethodDetails>)>();

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

    private ImmutableArray<T> GetBaseServices<T>()
    {
        var typeName = typeof(T).AssemblyQualifiedName;
        Contract.ThrowIfNull(typeName);

        return _baseServices.TryGetValue(typeName, out var baseServices)
            ? baseServices.SelectAsArray(creatorFunc => (T)creatorFunc(this))
            : [];
    }

    private IEnumerable<T> GetMefServices<T>()
    {
        if (typeof(T) == typeof(IMethodHandler))
        {
            // HACK: There is special handling for the IMethodHandler to make sure that its types remain lazy
            // Special case this to avoid providing them twice.
            yield break;
        }

        foreach (var (typeName, lazyService) in _lazyMefLspServices)
        {
            if (lazyService.Metadata.InterfaceNames.Contains(typeof(T).AssemblyQualifiedName!))
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
