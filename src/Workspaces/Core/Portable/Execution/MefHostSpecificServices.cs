// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Execution
{
    internal class MefHostSpecificServices : HostSpecificServices
    {
        private readonly MefWorkspaceServices _workspaceServices;
        private readonly string _host;
        private readonly ImmutableArray<Lazy<IHostSpecificService, HostSpecificServiceMetadata>> _services;

        private ImmutableDictionary<Type, Lazy<IHostSpecificService, HostSpecificServiceMetadata>> _serviceMap
            = ImmutableDictionary<Type, Lazy<IHostSpecificService, HostSpecificServiceMetadata>>.Empty;

        public MefHostSpecificServices(
            MefWorkspaceServices workspaceServices,
            string host)
        {
            _workspaceServices = workspaceServices;
            _host = host;

            var hostServices = workspaceServices.HostExportProvider;

            _services = hostServices.GetExports<IHostSpecificService, HostSpecificServiceMetadata>()
                    .Concat(hostServices.GetExports<IHostSpecificServiceFactory, HostSpecificServiceMetadata>()
                                        .Select(lz => new Lazy<IHostSpecificService, HostSpecificServiceMetadata>(() => lz.Value.CreateService(this), lz.Metadata)))
                    .Where(lz => lz.Metadata.Host == host).ToImmutableArray();
        }

        public override HostWorkspaceServices WorkspaceServices
        {
            get { return _workspaceServices; }
        }

        public override string Host
        {
            get { return _host; }
        }

        public bool HasServices
        {
            get { return _services.Length > 0; }
        }

        public override THostSpecificService GetService<THostSpecificService>()
        {
            Lazy<IHostSpecificService, HostSpecificServiceMetadata> service;
            if (TryGetService(typeof(THostSpecificService), out service))
            {
                return (THostSpecificService)service.Value;
            }
            else
            {
                return default(THostSpecificService);
            }
        }

        internal bool TryGetService(Type serviceType, out Lazy<IHostSpecificService, HostSpecificServiceMetadata> service)
        {
            if (!_serviceMap.TryGetValue(serviceType, out service))
            {
                service = ImmutableInterlocked.GetOrAdd(ref _serviceMap, serviceType, svctype =>
                {
                    // PERF: Hoist AssemblyQualifiedName out of inner lambda to avoid repeated string allocations.
                    var assemblyQualifiedName = svctype.AssemblyQualifiedName;
                    return PickHostSpecificService(_services.Where(lz => lz.Metadata.ServiceType == assemblyQualifiedName));
                });
            }

            return service != default(Lazy<IHostSpecificService, HostSpecificServiceMetadata>);
        }

        private Lazy<IHostSpecificService, HostSpecificServiceMetadata> PickHostSpecificService(IEnumerable<Lazy<IHostSpecificService, HostSpecificServiceMetadata>> services)
        {
            Lazy<IHostSpecificService, HostSpecificServiceMetadata> service;

            // workspace specific kind is best
            if (TryGetServiceByLayer(_workspaceServices.Workspace.Kind, services, out service))
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
            return default(Lazy<IHostSpecificService, HostSpecificServiceMetadata>);
        }

        private static bool TryGetServiceByLayer(string layer, IEnumerable<Lazy<IHostSpecificService, HostSpecificServiceMetadata>> services, out Lazy<IHostSpecificService, HostSpecificServiceMetadata> service)
        {
            service = services.SingleOrDefault(lz => lz.Metadata.Layer == layer);
            return service != default(Lazy<IHostSpecificService, HostSpecificServiceMetadata>);
        }
    }
}
