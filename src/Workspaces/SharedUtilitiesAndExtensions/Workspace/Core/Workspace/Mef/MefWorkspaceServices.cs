﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
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

        private readonly ImmutableArray<(Lazy<IWorkspaceService, WorkspaceServiceMetadata> lazyService, bool usesFactory)> _services;

        // map of type name to workspace service
        private ImmutableDictionary<Type, (Lazy<IWorkspaceService, WorkspaceServiceMetadata> lazyService, bool usesFactory)> _serviceMap
            = ImmutableDictionary<Type, (Lazy<IWorkspaceService, WorkspaceServiceMetadata> lazyService, bool usesFactory)>.Empty;

        private readonly object _gate = new();
        private readonly HashSet<IDisposable> _ownedDisposableServices = new(ReferenceEqualityComparer.Instance);

        // accumulated cache for language services
        private ImmutableDictionary<string, MefLanguageServices> _languageServicesMap
            = ImmutableDictionary<string, MefLanguageServices>.Empty;

        public MefWorkspaceServices(IMefHostExportProvider host, Workspace workspace)
        {
            _exportProvider = host;
            _workspace = workspace;

            var services = host.GetExports<IWorkspaceService, WorkspaceServiceMetadata>()
                .Select(lz => (lz, usesFactory: false));
            var factories = host.GetExports<IWorkspaceServiceFactory, WorkspaceServiceMetadata>()
                .Select(lz => (new Lazy<IWorkspaceService, WorkspaceServiceMetadata>(() => lz.Value.CreateService(this), lz.Metadata), usesFactory: true));

            _services = services.Concat(factories).ToImmutableArray();
        }

        public override HostServices HostServices
        {
            get { return (HostServices)_exportProvider; }
        }

        internal IMefHostExportProvider HostExportProvider => _exportProvider;

        public override Workspace Workspace => _workspace;

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
                try
                {
                    service.Dispose();
                }
                catch (Exception ex) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(ex))
                {
                    throw ExceptionUtilities.Unreachable;
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            if (exceptions is not null)
            {
                throw new AggregateException(CompilerExtensionsResources.Instantiated_parts_threw_exceptions_from_IDisposable_Dispose, exceptions);
            }

            base.Dispose();
        }

        public override TWorkspaceService GetService<TWorkspaceService>()
        {
            if (TryGetService(typeof(TWorkspaceService), out var service))
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
                var checkAddDisposable = service.usesFactory && !service.lazyService.IsValueCreated;

                var serviceInstance = (TWorkspaceService)service.lazyService.Value;
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
                return default;
            }
        }

        private bool TryGetService(Type serviceType, out (Lazy<IWorkspaceService, WorkspaceServiceMetadata> lazyService, bool usesFactory) service)
        {
            if (!_serviceMap.TryGetValue(serviceType, out service))
            {
                service = ImmutableInterlocked.GetOrAdd(ref _serviceMap, serviceType, svctype =>
                {
                    // Pick from list of exported factories and instances
                    // PERF: Hoist AssemblyQualifiedName out of inner lambda to avoid repeated string allocations.
                    var assemblyQualifiedName = svctype.AssemblyQualifiedName;
                    return PickWorkspaceService(_services.Where(lz => lz.lazyService.Metadata.ServiceType == assemblyQualifiedName));
                });
            }

            return service.lazyService != null;
        }

        private (Lazy<IWorkspaceService, WorkspaceServiceMetadata> lazyService, bool usesFactory) PickWorkspaceService(IEnumerable<(Lazy<IWorkspaceService, WorkspaceServiceMetadata> lazyService, bool usesFactory)> services)
        {
            (Lazy<IWorkspaceService, WorkspaceServiceMetadata> lazyService, bool usesFactory) service;
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
            return default;
        }

        private static bool TryGetServiceByLayer(string layer, IEnumerable<(Lazy<IWorkspaceService, WorkspaceServiceMetadata> lazyService, bool usesFactory)> services, out (Lazy<IWorkspaceService, WorkspaceServiceMetadata> lazyService, bool usesFactory) service)
        {
            service = services.SingleOrDefault(lz => lz.lazyService.Metadata.Layer == layer);
            return service.lazyService != null;
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
                languageServices = ImmutableInterlocked.GetOrAdd(ref _languageServicesMap, languageName, _ => new MefLanguageServices(this, languageName));
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

        internal sealed class LazyServiceMetadataDebuggerProxy
        {
            private readonly ImmutableArray<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> _services;

            public LazyServiceMetadataDebuggerProxy(ImmutableArray<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> services) =>
                _services = services;

            public (string type, string layer)[] Metadata
                => _services.Select(s => (s.Metadata.ServiceType, s.Metadata.Layer)).ToArray();
        }
    }
}
