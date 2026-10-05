// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class LspServices : ILspServices, IMethodHandlerProvider
{
    private readonly FrozenDictionary<string, Lazy<object, LspServiceMetadataView>> _lazyMefLspServices;

    /// <summary>
    /// The MEF sharing boundary that owns every per-server service (see <see cref="ExportLspServiceAttribute"/>).
    /// Disposing it cleans up instantiated <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/> parts,
    /// blocking until asynchronous cleanup completes.
    /// </summary>
    private readonly IDisposable _scope;

    /// <summary>
    /// A set of base services that apply to all Roslyn lsp services.
    /// Unfortunately MEF doesn't provide a good way to export something for multiple contracts with metadata
    /// so these are manually created in <see cref="RoslynLanguageServer"/>.
    /// </summary>
    private readonly FrozenDictionary<string, ImmutableArray<BaseService>> _baseServices;
    private readonly RoslynTelemetry _telemetry = RoslynTelemetry.Current;

    /// <param name="lspServices">The LSP services that apply to this server's LSP contract (see <see cref="LspServerScope.GetServices"/>).</param>
    /// <param name="serverKind">The kind of this server, used to pick server kind specific overrides.</param>
    /// <param name="baseServices">Services created manually by the server, which take precedence over MEF services.</param>
    /// <param name="scope">The MEF sharing boundary that owns the per-server services, disposed with this instance.</param>
    public LspServices(
        ImmutableArray<Lazy<object, LspServiceMetadataView>> lspServices,
        WellKnownLspServerKinds serverKind,
        FrozenDictionary<string, ImmutableArray<BaseService>> baseServices,
        IDisposable scope)
    {
        _scope = scope;
        var serviceMap = new Dictionary<string, Lazy<object, LspServiceMetadataView>>();

        // Add services exported for this server kind.
        foreach (var lazyService in lspServices.Where(s => s.Metadata.ServerKind == serverKind))
            serviceMap.Add(lazyService.Metadata.TypeRef.TypeName, lazyService);

        // Add services exported for any (if there is not already an existing service for the specific server kind).
        foreach (var lazyService in lspServices.Where(s => s.Metadata.ServerKind == WellKnownLspServerKinds.Any))
        {
            var typeName = lazyService.Metadata.TypeRef.TypeName;
            if (!serviceMap.TryGetValue(typeName, out var existing))
            {
                serviceMap.Add(typeName, lazyService);
            }
            else
            {
                // Make sure we're not trying to add a duplicate Any service, but otherwise we should skip adding
                // this service as we already have a more specific service available.
                Contract.ThrowIfTrue(existing.Metadata.ServerKind == WellKnownLspServerKinds.Any);
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
        var type = typeof(T);
        var typeName = type.FullName;
        Contract.ThrowIfNull(typeName);

        // Query for a service with an exact type match.
        var service = GetService(typeName);
        if (service is not null)
        {
            return (T)service;
        }

        // If given an interface, query for a service that implements that interface (this is how GetRequiredServices works)
        // Only allow this if there is exactly one service that implements the interface.
        return type.IsInterface
            ? GetRequiredServices<T>().SingleOrDefault()
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
        if (typeName == typeof(ILspServices).FullName || typeName == typeof(LspServices).FullName)
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
            // A service can first be requested from a context that carries no ambient instance of its own (for
            // example a file-watcher callback or work-queue batch), so re-establish this server's instance for
            // services that capture RoslynTelemetry.Current.
            using var _ = RoslynTelemetry.SetCurrent(_telemetry);
            return lazyService.Value;
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

    public ValueTask DisposeAsync()
    {
        try
        {
            _scope.Dispose();
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex))
        {
        }

        return ValueTask.CompletedTask;
    }
}
