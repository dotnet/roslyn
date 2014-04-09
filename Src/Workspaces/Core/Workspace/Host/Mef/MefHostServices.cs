// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
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

        private ImmutableList<string> languages;

        private ImmutableList<string> GetSupportedLanguages()
        {
            if (this.languages == null)
            {
                var list = this.GetExports<ILanguageService, LanguageServiceMetadata>().Select(lz => lz.Metadata.Language).Concat(
                           this.GetExports<ILanguageServiceFactory, LanguageServiceMetadata>().Select(lz => lz.Metadata.Language))
                           .Distinct()
                           .ToImmutableList();

                Interlocked.CompareExchange(ref this.languages, list, null);
            }

            return this.languages;
        }

        #region WorkspaceServices
        private class MefWorkspaceServices : HostWorkspaceServices
        {
            private readonly MefHostServices host;
            private readonly Workspace workspace;

            // map of type name to workspace service
            private ImmutableDictionary<string, IWorkspaceService> serviceMap
                = ImmutableDictionary<string, IWorkspaceService>.Empty;

            // accumulated cache for language services
            private ImmutableDictionary<string, MefLanguageServices> languageServicesMap
                = ImmutableDictionary<string, MefLanguageServices>.Empty;

            public MefWorkspaceServices(MefHostServices host, Workspace workspace)
            {
                this.host = host;
                this.workspace = workspace;
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
                IWorkspaceService service;

                var currentMap = this.serviceMap;
                var key = typeof(TWorkspaceService).AssemblyQualifiedName;
                if (!currentMap.TryGetValue(key, out service))
                {
                    service = ImmutableInterlocked.GetOrAdd(ref this.serviceMap, key, _ =>
                    {
                        // pick from list of exported factories and instances
                        return PickWorkspaceService(
                            this.host.GetExports<IWorkspaceServiceFactory, WorkspaceServiceMetadata>()
                                .Where(lz => lz.Metadata.ServiceType == key)
                                .Select(lz => new KeyValuePair<string, Func<MefWorkspaceServices, IWorkspaceService>>(lz.Metadata.Layer, ws => lz.Value.CreateService(ws)))
                                .Concat(
                            this.host.GetExports<IWorkspaceService, WorkspaceServiceMetadata>()
                                .Where(lz => lz.Metadata.ServiceType == key)
                                .Select(lz => new KeyValuePair<string, Func<MefWorkspaceServices, IWorkspaceService>>(lz.Metadata.Layer, ws => lz.Value))));
                    });
                }

                return (TWorkspaceService)service;
            }

            private IWorkspaceService PickWorkspaceService(IEnumerable<KeyValuePair<string, Func<MefWorkspaceServices, IWorkspaceService>>> services)
            {
                // workspace specific kind is best
                var pair = services.SingleOrDefault(s => s.Key == this.workspace.Kind);
                if (pair.Key != null)
                {
                    return pair.Value(this);
                }

                // host services override editor or default
                pair = services.SingleOrDefault(s => s.Key == ServiceLayer.Host);
                if (pair.Key != null)
                {
                    return pair.Value(this);
                }

                // editor services override default
                pair = services.SingleOrDefault(s => s.Key == ServiceLayer.Editor);
                if (pair.Key != null)
                {
                    return pair.Value(this);
                }

                // services marked as any are default
                pair = services.SingleOrDefault(s => s.Key == ServiceLayer.Default);
                if (pair.Key != null)
                {
                    return pair.Value(this);
                }

                pair = services.SingleOrDefault();
                if (pair.Key != null)
                {
                    return pair.Value(this);
                }
                else
                {
                    return default(IWorkspaceService);
                }
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
        }
        #endregion

        #region LanguageServices
        private class MefLanguageServices : HostLanguageServices
        {
            private readonly MefWorkspaceServices workspaceServices;
            private readonly string language;
            private readonly ImmutableList<Lazy<ILanguageService, LanguageServiceMetadata>> services;
            private readonly ImmutableList<Lazy<ILanguageServiceFactory, LanguageServiceMetadata>> factories;

            private ImmutableDictionary<string, ILanguageService> serviceMap = ImmutableDictionary<string, ILanguageService>.Empty;

            public MefLanguageServices(
                MefWorkspaceServices workspaceServices, 
                string language)
            {
                this.workspaceServices = workspaceServices;
                this.language = language;
                var hostServices = (MefHostServices)workspaceServices.HostServices;
                this.services = hostServices.GetExports<ILanguageService, LanguageServiceMetadata>().Where(lz => lz.Metadata.Language == language).ToImmutableList();
                this.factories = hostServices.GetExports<ILanguageServiceFactory, LanguageServiceMetadata>().Where(lz => lz.Metadata.Language == language).ToImmutableList();
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
                get { return this.services.Count > 0 || this.factories.Count > 0; }
            }

            public override TLanguageService GetService<TLanguageService>()
            {
                ILanguageService service;

                var currentMap = this.serviceMap;
                var key = typeof(TLanguageService).AssemblyQualifiedName;

                if (!currentMap.TryGetValue(key, out service))
                {
                    service = ImmutableInterlocked.GetOrAdd(ref this.serviceMap, key, _ =>
                    {
                        // pick from list of exported factories and instances
                        return PickLanguageService(
                            this.factories
                                .Where(lz => lz.Metadata.ServiceType == key)
                                .Select(lz => new KeyValuePair<string, Func<MefLanguageServices, ILanguageService>>(lz.Metadata.Layer, ls => lz.Value.CreateLanguageService(ls)))
                                .Concat(
                            this.services
                                .Where(lz => lz.Metadata.ServiceType == key)
                                .Select(lz => new KeyValuePair<string, Func<MefLanguageServices, ILanguageService>>(lz.Metadata.Layer, ls => lz.Value))));
                    });
                }
               
                return (TLanguageService)service;
            }

            private ILanguageService PickLanguageService(IEnumerable<KeyValuePair<string, Func<MefLanguageServices, ILanguageService>>> services)
            {
                // workspace specific kind is best
                var pair = services.SingleOrDefault(s => s.Key == this.workspaceServices.Workspace.Kind);
                if (pair.Key != null)
                {
                    return pair.Value(this);
                }

                // host services override editor or default
                pair = services.SingleOrDefault(s => s.Key == ServiceLayer.Host);
                if (pair.Key != null)
                {
                    return pair.Value(this);
                }

                // editor services override default
                pair = services.SingleOrDefault(s => s.Key == ServiceLayer.Editor);
                if (pair.Key != null)
                {
                    return pair.Value(this);
                }

                // services marked as any are default
                pair = services.SingleOrDefault(s => s.Key == ServiceLayer.Default);
                if (pair.Key != null)
                {
                    return pair.Value(this);
                }

                pair = services.SingleOrDefault();
                if (pair.Key != null)
                {
                    return pair.Value(this);
                }
                else
                {
                    return default(ILanguageService);
                }
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
                    this.exportProvider.GetExports<TExtension, TMetadata>().ToImmutableList());
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
                    this.exportProvider.GetExports<TExtension>().ToImmutableList());
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

        private static ImmutableList<Assembly> defaultAssemblies;
        public static ImmutableList<Assembly> DefaultAssemblies
        {
            get
            {
                if (defaultAssemblies == null)
                {
                    Interlocked.CompareExchange(ref defaultAssemblies, LoadDefaultAssemblies(), null);
                }

                return defaultAssemblies;
            }
        }

        private static ImmutableList<Assembly> LoadDefaultAssemblies()
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

            return assemblies.ToImmutableList();
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