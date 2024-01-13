// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

[assembly: DebuggerTypeProxy(typeof(MefLanguageServices.LazyServiceMetadataDebuggerProxy), Target = typeof(ImmutableArray<Lazy<ILanguageService, WorkspaceServiceMetadata>>))]

namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal sealed class MefLanguageServices : HostLanguageServices
    {
        private readonly MefWorkspaceServices _workspaceServices;
        private readonly string _language;
        private readonly ImmutableArray<Lazy<ILanguageService, LanguageServiceMetadata>> _services;

        private ImmutableDictionary<Type, Lazy<ILanguageService, LanguageServiceMetadata>> _serviceMap
            = ImmutableDictionary<Type, Lazy<ILanguageService, LanguageServiceMetadata>>.Empty;

        public MefLanguageServices(
            MefWorkspaceServices workspaceServices,
            string language)
        {
            _workspaceServices = workspaceServices;
            _language = language;

            var hostServices = workspaceServices.HostExportProvider;

            var services = hostServices.GetExports<ILanguageService, LanguageServiceMetadata>();
            var factories = hostServices.GetExports<ILanguageServiceFactory, LanguageServiceMetadata>()
                .Select(lz => new Lazy<ILanguageService, LanguageServiceMetadata>(() => lz.Value.CreateLanguageService(this), lz.Metadata));

            _services = services.Concat(factories).Where(lz => lz.Metadata.Language == language).ToImmutableArray();
        }

        public override HostWorkspaceServices WorkspaceServices => _workspaceServices;

        public override string Language => _language;

        public bool HasServices
        {
            get { return _services.Length > 0; }
        }

        public override TLanguageService GetService<TLanguageService>()
        {
            if (TryGetService(typeof(TLanguageService), out var service))
            {
                return (TLanguageService)service.Value;
            }
            else
            {
                return default;
            }
        }

        internal bool TryGetService(Type serviceType, out Lazy<ILanguageService, LanguageServiceMetadata> service)
        {
            if (!_serviceMap.TryGetValue(serviceType, out service))
            {
                service = ImmutableInterlocked.GetOrAdd(ref _serviceMap, serviceType, svctype =>
                {
                    // PERF: Hoist AssemblyQualifiedName out of inner lambda to avoid repeated string allocations.
                    var assemblyQualifiedName = svctype.AssemblyQualifiedName;
                    return PickLanguageService(_services.Where(lz => lz.Metadata.ServiceType == assemblyQualifiedName));
                });
            }

            return service != null;
        }

        private Lazy<ILanguageService, LanguageServiceMetadata> PickLanguageService(IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> services)
        {
            Lazy<ILanguageService, LanguageServiceMetadata> service;
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
            return null;
        }

        private static bool TryGetServiceByLayer(string layer, IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> services, out Lazy<ILanguageService, LanguageServiceMetadata> service)
        {
            service = services.SingleOrDefault(lz => lz.Metadata.Layer == layer);
            return service != null;
        }

        internal sealed class LazyServiceMetadataDebuggerProxy(ImmutableArray<Lazy<ILanguageService, LanguageServiceMetadata>> services)
        {
            public (string type, string layer)[] Metadata
                => services.Select(s => (s.Metadata.ServiceType, s.Metadata.Layer)).ToArray();
        }
    }
}
