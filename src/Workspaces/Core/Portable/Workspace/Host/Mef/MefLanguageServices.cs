// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal class MefLanguageServices : HostLanguageServices
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

            _services = hostServices.GetExports<ILanguageService, LanguageServiceMetadata>()
                    .Concat(hostServices.GetExports<ILanguageServiceFactory, LanguageServiceMetadata>()
                                        .Select(lz => new Lazy<ILanguageService, LanguageServiceMetadata>(() => lz.Value.CreateLanguageService(this), lz.Metadata)))
                    .Where(lz => lz.Metadata.Language == language).ToImmutableArray();
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

            return service != default(Lazy<ILanguageService, LanguageServiceMetadata>);
        }

        private Lazy<ILanguageService, LanguageServiceMetadata> PickLanguageService(IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> services)
        {

            // workspace specific kind is best
            if (TryGetServiceByLayer(_workspaceServices.Workspace.Kind, services, out var service))
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

        private static bool TryGetServiceByLayer(string layer, IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> services, out Lazy<ILanguageService, LanguageServiceMetadata> service)
        {
            service = services.SingleOrDefault(lz => lz.Metadata.Layer == layer);
            return service != default(Lazy<ILanguageService, LanguageServiceMetadata>);
        }
    }
}
