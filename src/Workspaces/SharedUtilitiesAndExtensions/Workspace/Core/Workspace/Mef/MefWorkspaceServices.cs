// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

[assembly: DebuggerTypeProxy(typeof(MefWorkspaceServices.LazyServiceMetadataDebuggerProxy), Target = typeof(ImmutableArray<Lazy<IWorkspaceService, WorkspaceServiceMetadata>>))]

namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal sealed class MefWorkspaceServices : HostWorkspaceServices
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

            var services = host.GetExports<IWorkspaceService, WorkspaceServiceMetadata>();
            var factories = host.GetExports<IWorkspaceServiceFactory, WorkspaceServiceMetadata>()
                .Select(lz => new Lazy<IWorkspaceService, WorkspaceServiceMetadata>(() => lz.Value.CreateService(this), lz.Metadata));

            _services = services.Concat(factories).ToImmutableArray();
        }

        public override HostServices HostServices
        {
            get { return (HostServices)_exportProvider; }
        }

        internal IMefHostExportProvider HostExportProvider => _exportProvider;

        internal string WorkspaceKind => _workspace.Kind;

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

        public override TWorkspaceService GetService<TWorkspaceService>()
        {
            if (TryGetService(typeof(TWorkspaceService), out var service))
            {
                return (TWorkspaceService)service.Value;
            }
            else
            {
                return default;
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

            return service != null;
        }

        private Lazy<IWorkspaceService, WorkspaceServiceMetadata> PickWorkspaceService(IEnumerable<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> services)
        {
            Lazy<IWorkspaceService, WorkspaceServiceMetadata> service;
#if !CODE_STYLE
            // test layer overrides all other layers and workspace kind:
            if (TryGetServiceByLayer(ServiceLayer.Test, services, out service))
            {
                return service;
            }
#endif
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
            return null;
        }

        private static bool TryGetServiceByLayer(string layer, IEnumerable<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> services, out Lazy<IWorkspaceService, WorkspaceServiceMetadata> service)
        {
            service = services.SingleOrDefault(lz => lz.Metadata.Layer == layer);
            return service != null;
        }

        private IEnumerable<string> _languages;

        private IEnumerable<string> GetSupportedLanguages()
        {
            if (_languages == null)
            {
                var list = _exportProvider.GetExports<ILanguageService, LanguageServiceMetadata>().Select(lz => lz.Metadata.Language).Concat(
                           _exportProvider.GetExports<ILanguageServiceFactory, LanguageServiceMetadata>().Select(lz => lz.Metadata.Language))
                           .Distinct();

                Interlocked.CompareExchange(ref _languages, list, null);
            }

            return _languages;
        }

        public override IEnumerable<string> SupportedLanguages
        {
            get { return this.GetSupportedLanguages(); }
        }

        public override bool IsSupported(string languageName)
            => this.GetSupportedLanguages().Contains(languageName);

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
            foreach (var language in this.SupportedLanguages)
            {
#pragma warning disable RS0030 // Do not used banned API 'GetLanguageServices', use 'GetExtendedLanguageServices' instead - allowed in this context.
                var services = (MefLanguageServices)this.GetLanguageServices(language);
#pragma warning restore RS0030 // Do not used banned APIs
                if (services.TryGetService(typeof(TLanguageService), out var service))
                {
                    if (filter(service.Metadata.Data))
                    {
                        yield return (TLanguageService)service.Value;
                    }
                }
            }
        }

        internal bool TryGetLanguageServices(string languageName, out MefLanguageServices languageServices)
            => _languageServicesMap.TryGetValue(languageName, out languageServices);

        internal sealed class LazyServiceMetadataDebuggerProxy(ImmutableArray<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> services)
        {
            public (string type, string layer)[] Metadata
                => services.Select(s => (s.Metadata.ServiceType, s.Metadata.Layer)).ToArray();
        }
    }
}
