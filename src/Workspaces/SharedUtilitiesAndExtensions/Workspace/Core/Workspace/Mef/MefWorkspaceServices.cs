// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

[assembly: DebuggerTypeProxy(typeof(MefWorkspaceServices.LazyServiceMetadataDebuggerProxy), Target = typeof(ImmutableArray<Lazy<IWorkspaceService, WorkspaceServiceMetadata>>))]

namespace Microsoft.CodeAnalysis.Host.Mef;

internal sealed class MefWorkspaceServices : HostWorkspaceServices
{
    private readonly Workspace _workspace;

    private readonly ImmutableArray<(Lazy<IWorkspaceService, WorkspaceServiceMetadata> lazyService, bool usesFactory)> _services;

    // map of type name to workspace service
    private ImmutableDictionary<Type, (Lazy<IWorkspaceService, WorkspaceServiceMetadata>? lazyService, bool usesFactory)> _serviceMap
        = ImmutableDictionary<Type, (Lazy<IWorkspaceService, WorkspaceServiceMetadata>? lazyService, bool usesFactory)>.Empty;

    private readonly object _gate = new();
    private readonly HashSet<IDisposable> _ownedDisposableServices = new(ReferenceEqualityComparer.Instance);

    // accumulated cache for language services
    private ImmutableDictionary<string, MefLanguageServices> _languageServicesMap
        = ImmutableDictionary<string, MefLanguageServices>.Empty;

    private ImmutableArray<string> _languages;

    public MefWorkspaceServices(IMefHostExportProvider host, Workspace workspace)
    {
        HostExportProvider = host;
        _workspace = workspace;

        var services = host.GetExports<IWorkspaceService, WorkspaceServiceMetadata>()
            .Select(lz => (lz, usesFactory: false));
        var factories = host.GetExports<IWorkspaceServiceFactory, WorkspaceServiceMetadata>()
            .Select(lz => (new Lazy<IWorkspaceService, WorkspaceServiceMetadata>(() => lz.Value.CreateService(this), lz.Metadata), usesFactory: true));

        _services = [.. services, .. factories];
    }

    public override HostServices HostServices
    {
        get { return (HostServices)HostExportProvider; }
    }

    internal IMefHostExportProvider HostExportProvider { get; }

    internal string? WorkspaceKind => _workspace.Kind;

    public override Workspace Workspace
    {
        get
        {
            //#if !CODE_STYLE
            //                Contract.ThrowIfTrue(_workspace.Kind == CodeAnalysis.WorkspaceKind.RemoteWorkspace, "Access .Workspace off of a RemoteWorkspace MefWorkspaceServices is not supported.");
            //#endif
            return _workspace;
        }
    }

    public override void Dispose()
    {
        var allLanguageServices = Interlocked.Exchange(ref _languageServicesMap, _languageServicesMap.Clear());

        ImmutableArray<IDisposable> disposableServices;
        lock (_gate)
        {
            disposableServices = [.. _ownedDisposableServices];
            _ownedDisposableServices.Clear();
        }

        // Take care to give all disposal parts a chance to dispose even if some parts throw exceptions.
        List<Exception>? exceptions = null;

        foreach (var (_, languageServices) in allLanguageServices)
        {
            MefUtilities.DisposeWithExceptionTracking(languageServices, ref exceptions);
        }

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

    public override TWorkspaceService GetService<TWorkspaceService>()
    {
        if (TryGetService(typeof(TWorkspaceService), out var lazyService, out var usesFactory))
        {
            // MEF workspace service instances created by a factory are not owned by the MEF catalog or disposed
            // when the MEF catalog is disposed. Whenever we are potentially going to create an instance of a
            // service provided by a factory, we need to check if the resulting service implements IDisposable. The
            // specific conditions here are:
            //
            // * usesFactory: This is true when the workspace service is provided by a factory. Services provided
            //   directly are owned by the MEF catalog so they do not need to be tracked by the workspace.
            // * IsValueCreated: This will be false at least once prior to accessing the lazy value. Once the value
            //   is known to be created, we no longer need to try adding it to _ownedDisposableServices, so we use a
            //   lock-free fast path.
            var checkAddDisposable = usesFactory && !lazyService.IsValueCreated;

            var serviceInstance = (TWorkspaceService)lazyService.Value;
            if (checkAddDisposable && serviceInstance is IDisposable disposable)
            {
                lock (_gate)
                {
                    _ownedDisposableServices.Add(disposable);
                }
            }

            return serviceInstance;
        }
        else
        {
            return default!;
        }
    }

    private bool TryGetService(Type serviceType, [NotNullWhen(true)] out Lazy<IWorkspaceService, WorkspaceServiceMetadata>? lazyService, out bool usesFactory)
    {
        if (!_serviceMap.TryGetValue(serviceType, out var service))
        {
            service = ImmutableInterlocked.GetOrAdd(ref _serviceMap, serviceType, serviceType => LayeredServiceUtilities.PickService(serviceType, _workspace.Kind, _services));
        }

        (lazyService, usesFactory) = (service.lazyService, service.usesFactory);
        return lazyService != null;
    }

    private ImmutableArray<string> ComputeSupportedLanguages()
    {
        var localLanguages = _languages;
        if (localLanguages.IsDefault)
        {
            var list = HostExportProvider.GetExports<ILanguageService, LanguageServiceMetadata>().Select(lz => lz.Metadata.Language).Concat(
                       HostExportProvider.GetExports<ILanguageServiceFactory, LanguageServiceMetadata>().Select(lz => lz.Metadata.Language))
                       .Distinct()
                       .ToImmutableArray();

            ImmutableInterlocked.InterlockedCompareExchange(ref _languages, list, localLanguages);
        }

        return _languages;
    }

    public override IEnumerable<string> SupportedLanguages => ComputeSupportedLanguages();

#if !WORKSPACE
    internal ImmutableArray<string> SupportedLanguagesArray => ComputeSupportedLanguages();
#else
    internal override ImmutableArray<string> SupportedLanguagesArray => ComputeSupportedLanguages();
#endif

    public override bool IsSupported(string languageName)
        => this.SupportedLanguagesArray.Contains(languageName);

    public override HostLanguageServices GetLanguageServices(string languageName)
    {
        var currentServicesMap = _languageServicesMap;
        if (!currentServicesMap.TryGetValue(languageName, out var languageServices))
        {
            languageServices = ImmutableInterlocked.GetOrAdd(ref _languageServicesMap, languageName, static (languageName, self) => new MefLanguageServices(self, languageName), this);
        }

        if (languageServices.HasServices)
        {
            return languageServices;
        }
        else
        {
            // throws exception
#pragma warning disable RS0030 // Do not used banned API 'GetLanguageServices', use 'GetExtendedLanguageServices' instead - allowed in this context.
            return base.GetLanguageServices(languageName);
#pragma warning restore RS0030 // Do not used banned APIs
        }
    }

    public override IEnumerable<TLanguageService> FindLanguageServices<TLanguageService>(MetadataFilter filter)
    {
        foreach (var language in SupportedLanguagesArray)
        {
#pragma warning disable RS0030 // Do not used banned API 'GetLanguageServices', use 'GetExtendedLanguageServices' instead - allowed in this context.
            var services = (MefLanguageServices)this.GetLanguageServices(language);
#pragma warning restore RS0030 // Do not used banned APIs
            if (services.TryGetService<TLanguageService>(filter, out var service))
            {
                yield return service;
            }
        }
    }

    internal bool TryGetLanguageServices(string languageName, [NotNullWhen(true)] out MefLanguageServices? languageServices)
        => _languageServicesMap.TryGetValue(languageName, out languageServices);

    internal sealed class LazyServiceMetadataDebuggerProxy(ImmutableArray<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> services)
    {
        public (string type, string layer)[] Metadata
            => [.. services.Select(s => (s.Metadata.ServiceType, s.Metadata.Layer))];
    }
}
