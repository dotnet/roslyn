// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        private ImmutableDictionary<Type, Lazy<ILanguageService, LanguageServiceMetadata>?> _serviceMap
            = ImmutableDictionary<Type, Lazy<ILanguageService, LanguageServiceMetadata>?>.Empty;

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
                return default!;
            }
        }

        internal bool TryGetService(Type serviceType, [NotNullWhen(true)] out Lazy<ILanguageService, LanguageServiceMetadata>? service)
        {
            if (!_serviceMap.TryGetValue(serviceType, out service))
            {
                service = ImmutableInterlocked.GetOrAdd(ref _serviceMap, serviceType, serviceType => LayeredServiceUtilities.PickService(serviceType, _workspaceServices.WorkspaceKind, _services));
            }

            return service != null;
        }

        internal sealed class LazyServiceMetadataDebuggerProxy(ImmutableArray<Lazy<ILanguageService, LanguageServiceMetadata>> services)
        {
            public (string type, string layer)[] Metadata
                => services.Select(s => (s.Metadata.ServiceType, s.Metadata.Layer)).ToArray();
        }
    }
}
