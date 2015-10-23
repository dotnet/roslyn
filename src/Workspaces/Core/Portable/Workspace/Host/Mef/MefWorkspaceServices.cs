// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal class MefWorkspaceServices : HostWorkspaceServices
    {
        private readonly IMefHostExportProvider _exportProvider;
        private readonly Workspace _workspace;

        private readonly ImmutableArray<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> _services;

        // map of type name to workspace service
        private ImmutableDictionary<Type, Lazy<IWorkspaceService, WorkspaceServiceMetadata>> _serviceMap
            = ImmutableDictionary<Type, Lazy<IWorkspaceService, WorkspaceServiceMetadata>>.Empty;

        // accumulated cache for language services
        private ImmutableDictionary<string, MefLanguageServices> _languageServicesMap
            = ImmutableDictionary<string, MefLanguageServices>.Empty;

        public MefWorkspaceServices(IMefHostExportProvider host, Workspace workspace)
        {
            _exportProvider = host;
            _workspace = workspace;
            _services = host.GetExports<IWorkspaceService, WorkspaceServiceMetadata>()
                .Concat(host.GetExports<IWorkspaceServiceFactory, WorkspaceServiceMetadata>()
                            .Select(lz => new Lazy<IWorkspaceService, WorkspaceServiceMetadata>(() => lz.Value.CreateService(this), lz.Metadata)))
                .ToImmutableArray();
        }

        public override HostServices HostServices
        {
            get { return (HostServices)_exportProvider; }
        }

        internal IMefHostExportProvider HostExportProvider
        {
            get { return _exportProvider; }
        }

        public override Workspace Workspace
        {
            get { return _workspace; }
        }

        public override TWorkspaceService GetService<TWorkspaceService>()
        {
            Lazy<IWorkspaceService, WorkspaceServiceMetadata> service;
            if (TryGetService(typeof(TWorkspaceService), out service))
            {
                return (TWorkspaceService)service.Value;
            }
            else
            {
                return default(TWorkspaceService);
            }
        }

        private bool TryGetService(Type serviceType, out Lazy<IWorkspaceService, WorkspaceServiceMetadata> service)
        {
            if (!_serviceMap.TryGetValue(serviceType, out service))
            {
                service = ImmutableInterlocked.GetOrAdd(ref _serviceMap, serviceType, svctype =>
                {
                    // Pick from list of exported factories and instances
                    // PERF: Hoist AssemblyQualifiedName out of inner lambda to avoid repeated string allocations.
                    var assemblyQualifiedName = svctype.AssemblyQualifiedName;
                    return PickWorkspaceService(_services.Where(lz => lz.Metadata.ServiceType == assemblyQualifiedName));
                });
            }

            return service != default(Lazy<IWorkspaceService, WorkspaceServiceMetadata>);
        }

        private Lazy<IWorkspaceService, WorkspaceServiceMetadata> PickWorkspaceService(IEnumerable<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> services)
        {
            Lazy<IWorkspaceService, WorkspaceServiceMetadata> service;

            // workspace specific kind is best
            if (TryGetServiceByLayer(_workspace.Kind, services, out service))
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

            // no service.
            return default(Lazy<IWorkspaceService, WorkspaceServiceMetadata>);
        }

        private static bool TryGetServiceByLayer(string layer, IEnumerable<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> services, out Lazy<IWorkspaceService, WorkspaceServiceMetadata> service)
        {
            service = services.SingleOrDefault(lz => lz.Metadata.Layer == layer);
            return service != default(Lazy<IWorkspaceService, WorkspaceServiceMetadata>);
        }

        private IEnumerable<string> _languages;

        private IEnumerable<string> GetSupportedLanguages()
        {
            if (_languages == null)
            {
                var list = _exportProvider.GetExports<ILanguageService, LanguageServiceMetadata>().Select(lz => lz.Metadata.Language).Concat(
                           _exportProvider.GetExports<ILanguageServiceFactory, LanguageServiceMetadata>().Select(lz => lz.Metadata.Language))
                           .Distinct()
                           .ToImmutableArray();

                Interlocked.CompareExchange(ref _languages, list, null);
            }

            return _languages;
        }

        public override IEnumerable<string> SupportedLanguages
        {
            get { return this.GetSupportedLanguages(); }
        }

        public override bool IsSupported(string languageName)
        {
            return this.GetSupportedLanguages().Contains(languageName);
        }

        public override HostLanguageServices GetLanguageServices(string languageName)
        {
            var currentServicesMap = _languageServicesMap;

            MefLanguageServices languageServices;
            if (!currentServicesMap.TryGetValue(languageName, out languageServices))
            {
                languageServices = ImmutableInterlocked.GetOrAdd(ref _languageServicesMap, languageName, _ => new MefLanguageServices(this, languageName));
            }

            if (languageServices.HasServices)
            {
                return languageServices;
            }
            else
            {
                // throws exception
                return base.GetLanguageServices(languageName);
            }
        }

        public override IEnumerable<TLanguageService> FindLanguageServices<TLanguageService>(MetadataFilter filter)
        {
            foreach (var language in this.SupportedLanguages)
            {
                var services = (MefLanguageServices)this.GetLanguageServices(language);

                Lazy<ILanguageService, LanguageServiceMetadata> service;
                if (services.TryGetService(typeof(TLanguageService), out service))
                {
                    if (filter(service.Metadata.Data))
                    {
                        yield return (TLanguageService)service.Value;
                    }
                }
            }
        }
    }
}
