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

[assembly: DebuggerTypeProxy(typeof(MefWorkspaceServices.LazyServiceMetadataDebuggerProxy), Target = typeof(ImmutableArray<Lazy<IWorkspaceService, WorkspaceServiceMetadata>>))]

namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal sealed class MefWorkspaceServices : HostWorkspaceServices
    {
        private readonly IMefHostExportProvider _exportProvider;
        private readonly Workspace _workspace;

        private readonly ImmutableArray<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> _services;

        // map of type name to workspace service
        private ImmutableDictionary<Type, Lazy<IWorkspaceService, WorkspaceServiceMetadata>?> _serviceMap
            = ImmutableDictionary<Type, Lazy<IWorkspaceService, WorkspaceServiceMetadata>?>.Empty;

        // accumulated cache for language services
        private ImmutableDictionary<string, MefLanguageServices> _languageServicesMap
            = ImmutableDictionary<string, MefLanguageServices>.Empty;

        private ImmutableArray<string> _languages;

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

        public override TWorkspaceService GetService<TWorkspaceService>()
        {
            if (TryGetService(typeof(TWorkspaceService), out var service))
            {
                return (TWorkspaceService)service.Value;
            }
            else
            {
                return default!;
            }
        }

        private bool TryGetService(Type serviceType, [NotNullWhen(true)] out Lazy<IWorkspaceService, WorkspaceServiceMetadata>? service)
        {
            if (!_serviceMap.TryGetValue(serviceType, out service))
            {
                service = ImmutableInterlocked.GetOrAdd(ref _serviceMap, serviceType, serviceType => LayeredServiceUtilities.PickService(serviceType, _workspace.Kind, _services));
            }

            return service != null;
        }

        private ImmutableArray<string> ComputeSupportedLanguages()
        {
            var localLanguages = _languages;
            if (localLanguages.IsDefault)
            {
                var list = _exportProvider.GetExports<ILanguageService, LanguageServiceMetadata>().Select(lz => lz.Metadata.Language).Concat(
                           _exportProvider.GetExports<ILanguageServiceFactory, LanguageServiceMetadata>().Select(lz => lz.Metadata.Language))
                           .Distinct()
                           .ToImmutableArray();

                ImmutableInterlocked.InterlockedCompareExchange(ref _languages, list, localLanguages);
            }

            return _languages;
        }

        public override IEnumerable<string> SupportedLanguages => ComputeSupportedLanguages();

#if CODE_STYLE
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
                if (services.TryGetService(typeof(TLanguageService), out var service))
                {
                    if (filter(service.Metadata.Data))
                    {
                        yield return (TLanguageService)service.Value;
                    }
                }
            }
        }

        internal bool TryGetLanguageServices(string languageName, [NotNullWhen(true)] out MefLanguageServices? languageServices)
            => _languageServicesMap.TryGetValue(languageName, out languageServices);

        internal sealed class LazyServiceMetadataDebuggerProxy(ImmutableArray<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> services)
        {
            public (string type, string layer)[] Metadata
                => services.Select(s => (s.Metadata.ServiceType, s.Metadata.Layer)).ToArray();
        }
    }
}
