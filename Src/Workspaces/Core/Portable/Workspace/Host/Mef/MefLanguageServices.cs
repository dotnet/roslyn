using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal class MefLanguageServices : HostLanguageServices
    {
        private readonly MefWorkspaceServices workspaceServices;
        private readonly string language;
        private readonly ImmutableArray<Lazy<ILanguageService, LanguageServiceMetadata>> services;

        private ImmutableDictionary<Type, Lazy<ILanguageService, LanguageServiceMetadata>> serviceMap
            = ImmutableDictionary<Type, Lazy<ILanguageService, LanguageServiceMetadata>>.Empty;

        public MefLanguageServices(
            MefWorkspaceServices workspaceServices,
            string language)
        {
            this.workspaceServices = workspaceServices;
            this.language = language;

            var hostServices = workspaceServices.HostExportProvider;

            this.services = hostServices.GetExports<ILanguageService, LanguageServiceMetadata>()
                    .Concat(hostServices.GetExports<ILanguageServiceFactory, LanguageServiceMetadata>()
                                        .Select(lz => new Lazy<ILanguageService, LanguageServiceMetadata>(() => lz.Value.CreateLanguageService(this), lz.Metadata)))
                    .Where(lz => lz.Metadata.Language == language).ToImmutableArray();
        }

        public override HostWorkspaceServices WorkspaceServices
        {
            get { return this.workspaceServices; }
        }

        public override string Language
        {
            get { return this.language; }
        }

        public bool HasServices
        {
            get { return this.services.Length > 0; }
        }

        public override TLanguageService GetService<TLanguageService>()
        {
            Lazy<ILanguageService, LanguageServiceMetadata> service;
            if (TryGetService(typeof(TLanguageService), out service))
            {
                return (TLanguageService)service.Value;
            }
            else
            {
                return default(TLanguageService);
            }
        }

        internal bool TryGetService(Type serviceType, out Lazy<ILanguageService, LanguageServiceMetadata> service)
        {
            if (!this.serviceMap.TryGetValue(serviceType, out service))
            {
                service = ImmutableInterlocked.GetOrAdd(ref this.serviceMap, serviceType, svctype =>
                {
                    return PickLanguageService(this.services.Where(lz => lz.Metadata.ServiceType == svctype.AssemblyQualifiedName));
                });
            }

            return service != default(Lazy<ILanguageService, LanguageServiceMetadata>);
        }

        private Lazy<ILanguageService, LanguageServiceMetadata> PickLanguageService(IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> services)
        {
            Lazy<ILanguageService, LanguageServiceMetadata> service;

            // workspace specific kind is best
            if (TryGetServiceByLayer(this.workspaceServices.Workspace.Kind, services, out service))
            {
                return service;
            }

            // host layer overrides editor or default
            if (TryGetServiceByLayer(ServiceLayer.Host, services, out service))
            {
                return service;
            }

            // editor layer overrides default
            if (TryGetServiceByLayer(ServiceLayer.Editor, services, out service))
            {
                return service;
            }

            // that just leaves default
            if (TryGetServiceByLayer(ServiceLayer.Default, services, out service))
            {
                return service;
            }

            // no service
            return default(Lazy<ILanguageService, LanguageServiceMetadata>);
        }

        private static bool TryGetServiceByLayer(string layer, IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> services, out Lazy<ILanguageService, LanguageServiceMetadata> service)
        {
            service = services.SingleOrDefault(lz => lz.Metadata.Layer == layer);
            return service != default(Lazy<ILanguageService, LanguageServiceMetadata>);
        }
    }
}
