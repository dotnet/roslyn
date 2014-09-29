// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// A class that provides host services via classes instances exported via a MEF composition.
    /// </summary>
    public class MefHostServices : HostServices
    {
        // the export provider for the MEF composition
        private readonly ExportProvider exportProvider;

        // accumulated cache for exports
        private ImmutableDictionary<ExportKey, IEnumerable> exportsMap
            = ImmutableDictionary<ExportKey, IEnumerable>.Empty;

        private MefHostServices(ExportProvider exportProvider)
        {
            this.exportProvider = exportProvider;
        }

        public static MefHostServices Create(ExportProvider exportProvider)
        {
            if (exportProvider == null)
            {
                throw new ArgumentNullException("exportProvider");
            }

            return new MefHostServices(exportProvider);
        }

        public static MefHostServices Create(IEnumerable<System.Reflection.Assembly> assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException("assemblies");
            }

            var catalog = new AggregateCatalog(assemblies.Select(a => new AssemblyCatalog(a)));
            var container = new CompositionContainer(catalog, compositionOptions: CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);
            return new MefHostServices(container);
        }

        /// <summary>
        /// Creates a new <see cref="HostWorkspaceServices"/> associated with the specified workspace.
        /// </summary>
        protected internal override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
        {
            return new MefWorkspaceServices(this, workspace);
        }

        private IEnumerable<string> languages;

        private IEnumerable<string> GetSupportedLanguages()
        {
            if (this.languages == null)
            {
                var list = this.GetExports<ILanguageService, LanguageServiceMetadata>().Select(lz => lz.Metadata.Language).Concat(
                           this.GetExports<ILanguageServiceFactory, LanguageServiceMetadata>().Select(lz => lz.Metadata.Language))
                           .Distinct()
                           .ToImmutableArray();

                Interlocked.CompareExchange(ref this.languages, list, null);
            }

            return this.languages;
        }

        #region WorkspaceServices
        private class MefWorkspaceServices : HostWorkspaceServices
        {
            private readonly MefHostServices host;
            private readonly Workspace workspace;

            private readonly ImmutableArray<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> services;

            // map of type name to workspace service
            private ImmutableDictionary<Type, Lazy<IWorkspaceService, WorkspaceServiceMetadata>> serviceMap
                = ImmutableDictionary<Type, Lazy<IWorkspaceService, WorkspaceServiceMetadata>>.Empty;

            // accumulated cache for language services
            private ImmutableDictionary<string, MefLanguageServices> languageServicesMap
                = ImmutableDictionary<string, MefLanguageServices>.Empty;

            public MefWorkspaceServices(MefHostServices host, Workspace workspace)
            {
                this.host = host;
                this.workspace = workspace;
                this.services = host.GetExports<IWorkspaceService, WorkspaceServiceMetadata>()
                    .Concat(host.GetExports<IWorkspaceServiceFactory, WorkspaceServiceMetadata>()
                                .Select(lz => new Lazy<IWorkspaceService, WorkspaceServiceMetadata>(() => lz.Value.CreateService(this), lz.Metadata)))
                    .ToImmutableArray();
            }

            public override HostServices HostServices
            {
                get { return this.host; }
            }

            public override Workspace Workspace
            {
                get { return this.workspace; }
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
                if (!this.serviceMap.TryGetValue(serviceType, out service))
                {
                    service = ImmutableInterlocked.GetOrAdd(ref this.serviceMap, serviceType, svctype =>
                    {
                        // pick from list of exported factories and instances
                        return PickWorkspaceService(this.services.Where(lz => lz.Metadata.ServiceType == svctype.AssemblyQualifiedName));
                    });
                }

                return service != default(Lazy<IWorkspaceService, WorkspaceServiceMetadata>);
            }

            private Lazy<IWorkspaceService, WorkspaceServiceMetadata> PickWorkspaceService(IEnumerable<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> services)
            {
                Lazy<IWorkspaceService, WorkspaceServiceMetadata> service;

                // workspace specific kind is best
                if (TryGetServiceByLayer(this.workspace.Kind, services, out service))
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

                // no service.
                return default(Lazy<IWorkspaceService, WorkspaceServiceMetadata>);
            }

            private bool TryGetServiceByLayer(string layer, IEnumerable<Lazy<IWorkspaceService, WorkspaceServiceMetadata>> services, out Lazy<IWorkspaceService, WorkspaceServiceMetadata> service)
            {
                service = services.SingleOrDefault(lz => lz.Metadata.Layer == layer);
                return service != default(Lazy<IWorkspaceService, WorkspaceServiceMetadata>);
            }

            public override IEnumerable<string> SupportedLanguages
            {
                get { return this.host.GetSupportedLanguages(); }
            }

            public override bool IsSupported(string languageName)
            {
                return this.host.GetSupportedLanguages().Contains(languageName);
            }

            public override HostLanguageServices GetLanguageServices(string languageName)
            {
                var currentServicesMap = this.languageServicesMap;

                MefLanguageServices languageServices;
                if (!currentServicesMap.TryGetValue(languageName, out languageServices))
                {
                    languageServices = ImmutableInterlocked.GetOrAdd(ref this.languageServicesMap, languageName, _ => new MefLanguageServices(this, languageName));
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

        #endregion

        #region LanguageServices
        private class MefLanguageServices : HostLanguageServices
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
                var hostServices = (MefHostServices)workspaceServices.HostServices;

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
                        var serviceTypes = this.services.Where(lz => lz.Metadata.ServiceType == svctype.AssemblyQualifiedName);
                        if (serviceType == typeof(ISyntaxTreeFactoryService) && serviceTypes.IsEmpty())
                        {
                            var assembly = svctype.AssemblyQualifiedName;
                            var kind = this.workspaceServices.Workspace.Kind;
                            var tempServices = this.services.Select(lz => ValueTuple.Create(lz.Value, lz.Metadata, lz.Metadata.ServiceType)).ToArray();
                            var serviceMap = this.serviceMap.Select(kv => ValueTuple.Create(kv.Key, kv.Value.Value, kv.Value.Metadata)).ToArray();

                            ExceptionHelpers.Crash(new Exception("Crash"));

                            GC.KeepAlive(kind);
                            GC.KeepAlive(tempServices);
                            GC.KeepAlive(serviceMap);
                        }

                        return PickLanguageService(serviceTypes);
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
        #endregion

        #region Exports
        /// <summary>
        /// Gets all the MEF exports of the specified type with the specified metadata.
        /// </summary>
        public IEnumerable<Lazy<TExtension, TMetadata>> GetExports<TExtension, TMetadata>()
        {
            IEnumerable exports;
            var key = new ExportKey(typeof(TExtension).AssemblyQualifiedName, typeof(TMetadata).AssemblyQualifiedName);
            if (!this.exportsMap.TryGetValue(key, out exports))
            {
                exports = ImmutableInterlocked.GetOrAdd(ref this.exportsMap, key, _ =>
                    this.exportProvider.GetExports<TExtension, TMetadata>().ToImmutableArray());
            }

            return (IEnumerable<Lazy<TExtension, TMetadata>>)exports;
        }

        /// <summary>
        /// Gets all the MEF exports of the specified type.
        /// </summary>
        public IEnumerable<Lazy<TExtension>> GetExports<TExtension>()
        {
            IEnumerable exports;
            var key = new ExportKey(typeof(TExtension).AssemblyQualifiedName, "");
            if (!this.exportsMap.TryGetValue(key, out exports))
            {
                exports = ImmutableInterlocked.GetOrAdd(ref this.exportsMap, key, _ =>
                    this.exportProvider.GetExports<TExtension>().ToImmutableArray());
            }

            return (IEnumerable<Lazy<TExtension>>)exports;
        }

        private struct ExportKey : IEquatable<ExportKey>
        {
            internal readonly string ExtensionTypeName;
            internal readonly string MetadataTypeName;
            private readonly int hash;

            public ExportKey(string extensionTypeName, string metadataTypeName)
            {
                this.ExtensionTypeName = extensionTypeName;
                this.MetadataTypeName = metadataTypeName;
                this.hash = Hash.Combine(metadataTypeName.GetHashCode(), extensionTypeName.GetHashCode());
            }

            public bool Equals(ExportKey other)
            {
                return string.Compare(this.ExtensionTypeName, other.ExtensionTypeName, StringComparison.OrdinalIgnoreCase) == 0
                    && string.Compare(this.MetadataTypeName, other.MetadataTypeName, StringComparison.OrdinalIgnoreCase) == 0;
            }

            public override bool Equals(object obj)
            {
                return (obj is ExportKey) && this.Equals((ExportKey)obj);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(this.MetadataTypeName.GetHashCode(), this.ExtensionTypeName.GetHashCode());
            }
        }
        #endregion

        #region Defaults
        private static MefHostServices defaultHost;

        public static MefHostServices DefaultHost
        {
            get
            {
                if (defaultHost == null)
                {
                    var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
                    Interlocked.CompareExchange(ref defaultHost, host, null);
                }

                return defaultHost;
            }
        }

        private static ImmutableArray<Assembly> defaultAssemblies;
        public static ImmutableArray<Assembly> DefaultAssemblies
        {
            get
            {
                if (defaultAssemblies.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref defaultAssemblies, LoadDefaultAssemblies());
                }

                return defaultAssemblies;
            }
        }

        private static ImmutableArray<Assembly> LoadDefaultAssemblies()
        {
            // build a MEF composition using this assembly and the known VisualBasic/CSharp workspace assemblies.
            var assemblies = new List<Assembly>();
            var thisAssembly = typeof(MefHostServices).Assembly;
            assemblies.Add(thisAssembly);

            var thisAssemblyName = thisAssembly.GetName();
            var assemblyShortName = thisAssemblyName.Name;
            var assemblyVersion = thisAssemblyName.Version;
            var publicKeyToken = thisAssemblyName.GetPublicKeyToken().Aggregate(string.Empty, (s, b) => s + b.ToString("x2"));

            LoadAssembly(assemblies,
                string.Format("Microsoft.CodeAnalysis.CSharp.Workspaces, Version={0}, Culture=neutral, PublicKeyToken={1}", assemblyVersion, publicKeyToken));

            LoadAssembly(assemblies,
                string.Format("Microsoft.CodeAnalysis.VisualBasic.Workspaces, Version={0}, Culture=neutral, PublicKeyToken={1}", assemblyVersion, publicKeyToken));

            return assemblies.ToImmutableArray();
        }

        private static void LoadAssembly(List<Assembly> assemblies, string assemblyName)
        {
            try
            {
                var loadedAssembly = Assembly.Load(assemblyName);
                assemblies.Add(loadedAssembly);
            }
            catch (Exception)
            {
            }
        }
        #endregion
    }
}