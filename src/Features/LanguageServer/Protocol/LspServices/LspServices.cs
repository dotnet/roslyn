// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal class LspServices : IDisposable
{
    private readonly ImmutableDictionary<Type, Lazy<ILspService, LspServiceMetadataView>> _lazyLspServices;

    /// <summary>
    /// Gates access to <see cref="_servicesToDispose"/>.
    /// </summary>
    private readonly object _gate = new();
    private readonly HashSet<IDisposable> _servicesToDispose = new(ReferenceEqualityComparer.Instance);

    public LspServices(
        ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> mefLspServices,
        ImmutableArray<Lazy<ILspServiceFactory, LspServiceMetadataView>> mefLspServiceFactories,
        WellKnownLspServerKinds serverKind,
        ImmutableArray<Lazy<ILspService, LspServiceMetadataView>> baseServices)
    {
        // Convert MEF exported service factories to the lazy LSP services that they create.
        var servicesFromFactories = mefLspServiceFactories.Select(lz => new Lazy<ILspService, LspServiceMetadataView>(() => lz.Value.CreateILspService(this, serverKind), lz.Metadata));

        var services = mefLspServices.Concat(servicesFromFactories);

        // Make sure that we only include services exported for the specified server kind (or NotSpecified).
        services = services.Where(lazyService => lazyService.Metadata.ServerKind == serverKind || lazyService.Metadata.ServerKind == WellKnownLspServerKinds.Any);

        // Include the base level services that were passed in.
        services = services.Concat(baseServices);

        _lazyLspServices = services.ToImmutableDictionary(lazyService => lazyService.Metadata.Type, lazyService => lazyService);
    }

    public T GetRequiredService<T>() where T : class, ILspService
    {
        var service = GetService<T>();
        Contract.ThrowIfNull(service, $"Missing required LSP service {typeof(T).FullName}");
        return service;
    }

    public T? GetService<T>() where T : class, ILspService
    {
        var type = typeof(T);
        return TryGetService(type, out var service) ? (T)service : null;
    }

    public bool TryGetService(Type type, [NotNullWhen(true)] out object? lspService)
    {
        if (_lazyLspServices.TryGetValue(type, out var lazyService))
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

            return true;
        }

        lspService = null;
        return false;
    }

    public ImmutableArray<Type> GetRegisteredServices() => _lazyLspServices.Keys.ToImmutableArray();

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
