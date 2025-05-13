// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

[assembly: DebuggerTypeProxy(typeof(MefLanguageServices.LazyServiceMetadataDebuggerProxy), Target = typeof(ImmutableArray<Lazy<ILanguageService, WorkspaceServiceMetadata>>))]

namespace Microsoft.CodeAnalysis.Host.Mef;

internal sealed class MefLanguageServices : HostLanguageServices
{
    private readonly MefWorkspaceServices _workspaceServices;
    private readonly string _language;
    private readonly ImmutableArray<(Lazy<ILanguageService, LanguageServiceMetadata> lazyService, bool usesFactory)> _services;

    private ImmutableDictionary<Type, (Lazy<ILanguageService, LanguageServiceMetadata>? lazyService, bool usesFactory)> _serviceMap
        = ImmutableDictionary<Type, (Lazy<ILanguageService, LanguageServiceMetadata>? lazyService, bool usesFactory)>.Empty;

    private readonly object _gate = new();
    private readonly HashSet<IDisposable> _ownedDisposableServices = new(ReferenceEqualityComparer.Instance);

    public MefLanguageServices(
        MefWorkspaceServices workspaceServices,
        string language)
    {
        _workspaceServices = workspaceServices;
        _language = language;

        var hostServices = workspaceServices.HostExportProvider;

        var services = hostServices.GetExports<ILanguageService, LanguageServiceMetadata>()
            .Select(lz => (lazyService: lz, usesFactory: false));
        var factories = hostServices.GetExports<ILanguageServiceFactory, LanguageServiceMetadata>()
            .Select(lz => (lazyService: new Lazy<ILanguageService, LanguageServiceMetadata>(() => lz.Value.CreateLanguageService(this), lz.Metadata), usesFactory: true));

        _services = [.. services.Concat(factories).Where(lz => lz.lazyService.Metadata.Language == language)];
    }

    public override HostWorkspaceServices WorkspaceServices => _workspaceServices;

    public override string Language => _language;

    public bool HasServices
    {
        get { return _services.Length > 0; }
    }

    public override void Dispose()
    {
        ImmutableArray<IDisposable> disposableServices;
        lock (_gate)
        {
            disposableServices = [.. _ownedDisposableServices];
            _ownedDisposableServices.Clear();
        }

        // Take care to give all disposal parts a chance to dispose even if some parts throw exceptions.
        List<Exception>? exceptions = null;
        foreach (var service in disposableServices)
        {
            MefUtilities.DisposeWithExceptionTracking(service, ref exceptions);
        }

        if (exceptions is not null)
        {
            throw new AggregateException(CompilerExtensionsResources.Instantiated_parts_threw_exceptions_from_IDisposable_Dispose, exceptions);
        }

        base.Dispose();
    }

    public override TLanguageService GetService<TLanguageService>()
    {
        if (TryGetService<TLanguageService>(static _ => true, out var service))
        {
            return service;
        }
        else
        {
            return default!;
        }
    }

    internal bool TryGetService<TLanguageService>(HostWorkspaceServices.MetadataFilter filter, [MaybeNullWhen(false)] out TLanguageService languageService)
    {
        if (TryGetService(typeof(TLanguageService), out var lazyService, out var usesFactory)
            && filter(lazyService.Metadata.Data))
        {
            // MEF language service instances created by a factory are not owned by the MEF catalog or disposed
            // when the MEF catalog is disposed. Whenever we are potentially going to create an instance of a
            // service provided by a factory, we need to check if the resulting service implements IDisposable. The
            // specific conditions here are:
            //
            // * usesFactory: This is true when the language service is provided by a factory. Services provided
            //   directly are owned by the MEF catalog so they do not need to be tracked by the workspace.
            // * IsValueCreated: This will be false at least once prior to accessing the lazy value. Once the value
            //   is known to be created, we no longer need to try adding it to _ownedDisposableServices, so we use a
            //   lock-free fast path.
            var checkAddDisposable = usesFactory && !lazyService.IsValueCreated;

            languageService = (TLanguageService)lazyService.Value;
            if (checkAddDisposable && languageService is IDisposable disposable)
            {
                lock (_gate)
                {
                    _ownedDisposableServices.Add(disposable);
                }
            }

            return true;
        }
        else
        {
            languageService = default;
            return false;
        }
    }

    private bool TryGetService(Type serviceType, [NotNullWhen(true)] out Lazy<ILanguageService, LanguageServiceMetadata>? lazyService, out bool usesFactory)
    {
        if (!_serviceMap.TryGetValue(serviceType, out var service))
        {
            service = ImmutableInterlocked.GetOrAdd(ref _serviceMap, serviceType, serviceType => LayeredServiceUtilities.PickService(serviceType, _workspaceServices.WorkspaceKind, _services));
        }

        (lazyService, usesFactory) = (service.lazyService, service.usesFactory);
        return lazyService != null;
    }

    internal sealed class LazyServiceMetadataDebuggerProxy(ImmutableArray<Lazy<ILanguageService, LanguageServiceMetadata>> services)
    {
        public (string type, string layer)[] Metadata
            => [.. services.Select(s => (s.Metadata.ServiceType, s.Metadata.Layer))];
    }
}
