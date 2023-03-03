// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

[assembly: DebuggerTypeProxy(typeof(MefLanguageServices.LazyServiceMetadataDebuggerProxy), Target = typeof(ImmutableArray<Lazy<ILanguageService, WorkspaceServiceMetadata>>))]

namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal sealed class MefLanguageServices : HostLanguageServices
    {
        private readonly MefWorkspaceServices _workspaceServices;
        private readonly string _language;
        private readonly ImmutableArray<(Lazy<ILanguageService, LanguageServiceMetadata> lazyService, bool usesFactory)> _services;

        private ImmutableDictionary<Type, (Lazy<ILanguageService, LanguageServiceMetadata> lazyService, bool usesFactory)> _serviceMap
            = ImmutableDictionary<Type, (Lazy<ILanguageService, LanguageServiceMetadata> lazyService, bool usesFactory)>.Empty;

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

            _services = services.Concat(factories).Where(lz => lz.lazyService.Metadata.Language == language).ToImmutableArray();
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
                disposableServices = _ownedDisposableServices.ToImmutableArray();
                _ownedDisposableServices.Clear();
            }

            // Take care to give all disposal parts a chance to dispose even if some parts throw exceptions.
            List<Exception> exceptions = null;
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
                return default;
            }
        }

        internal bool TryGetService<TLanguageService>(HostWorkspaceServices.MetadataFilter filter, [MaybeNullWhen(false)] out TLanguageService languageService)
        {
            if (TryGetService(typeof(TLanguageService), out var service)
                && filter(service.lazyService.Metadata.Data))
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
                var checkAddDisposable = service.usesFactory && !service.lazyService.IsValueCreated;

                languageService = (TLanguageService)service.lazyService.Value;
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

        private bool TryGetService(Type serviceType, out (Lazy<ILanguageService, LanguageServiceMetadata> lazyService, bool usesFactory) service)
        {
            if (!_serviceMap.TryGetValue(serviceType, out service))
            {
                service = ImmutableInterlocked.GetOrAdd(ref _serviceMap, serviceType, svctype =>
                {
                    // PERF: Hoist AssemblyQualifiedName out of inner lambda to avoid repeated string allocations.
                    var assemblyQualifiedName = svctype.AssemblyQualifiedName;
                    return PickLanguageService(_services.Where(lz => lz.lazyService.Metadata.ServiceType == assemblyQualifiedName));
                });
            }

            return service.lazyService != null;
        }

        private (Lazy<ILanguageService, LanguageServiceMetadata> lazyService, bool usesFactory) PickLanguageService(IEnumerable<(Lazy<ILanguageService, LanguageServiceMetadata> lazyService, bool usesFactory)> services)
        {
            (Lazy<ILanguageService, LanguageServiceMetadata> lazyService, bool usesFactory) service;
#if !CODE_STYLE
            // test layer overrides everything else
            if (TryGetServiceByLayer(ServiceLayer.Test, services, out service))
            {
                return service;
            }
#endif
            // workspace specific kind is best
            if (TryGetServiceByLayer(_workspaceServices.WorkspaceKind, services, out service))
            {
                return service;
            }

            // host layer overrides editor, desktop or default
            if (TryGetServiceByLayer(ServiceLayer.Host, services, out service))
            {
                return service;
            }

            // editor layer overrides desktop or default
            if (TryGetServiceByLayer(ServiceLayer.Editor, services, out service))
            {
                return service;
            }

            // desktop layer overrides default
            if (TryGetServiceByLayer(ServiceLayer.Desktop, services, out service))
            {
                return service;
            }

            // that just leaves default
            if (TryGetServiceByLayer(ServiceLayer.Default, services, out service))
            {
                return service;
            }

            // no service
            return default;
        }

        private static bool TryGetServiceByLayer(string layer, IEnumerable<(Lazy<ILanguageService, LanguageServiceMetadata> lazyService, bool usesFactory)> services, out (Lazy<ILanguageService, LanguageServiceMetadata> lazyService, bool usesFactory) service)
        {
            service = services.SingleOrDefault(lz => lz.lazyService.Metadata.Layer == layer);
            return service.lazyService != null;
        }

        internal sealed class LazyServiceMetadataDebuggerProxy
        {
            private readonly ImmutableArray<Lazy<ILanguageService, LanguageServiceMetadata>> _services;

            public LazyServiceMetadataDebuggerProxy(ImmutableArray<Lazy<ILanguageService, LanguageServiceMetadata>> services)
                => _services = services;

            public (string type, string layer)[] Metadata
                => _services.Select(s => (s.Metadata.ServiceType, s.Metadata.Layer)).ToArray();
        }
    }
}
