// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    public class MefHostServices : HostServices, IMefHostExportProvider
    {
        internal delegate MefHostServices CreationHook(IEnumerable<Assembly> assemblies, bool requestingDefaultHost);

        /// <summary>
        /// This delegate allows test code to override the behavior of <see cref="Create(IEnumerable{Assembly})"/>.
        /// </summary>
        /// <seealso cref="TestAccessor.HookServiceCreation"/>
        private static CreationHook s_CreationHook;

        private readonly CompositionContext _compositionContext;

        public MefHostServices(CompositionContext compositionContext)
        {
            _compositionContext = compositionContext;
        }

        public static MefHostServices Create(CompositionContext compositionContext)
        {
            if (compositionContext == null)
            {
                throw new ArgumentNullException(nameof(compositionContext));
            }

            return new MefHostServices(compositionContext);
        }

        public static MefHostServices Create(IEnumerable<System.Reflection.Assembly> assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            if (s_CreationHook != null)
            {
                var requestingDefaultAssemblies =
                    assemblies is ImmutableArray<Assembly> array
                    && array == DefaultAssemblies;
                return s_CreationHook(assemblies, requestingDefaultAssemblies);
            }

            var compositionConfiguration = new ContainerConfiguration().WithAssemblies(assemblies.Distinct());
            var container = compositionConfiguration.CreateContainer();
            return new MefHostServices(container);
        }

        protected internal override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
        {
            return new MefWorkspaceServices(this, workspace);
        }

        IEnumerable<Lazy<TExtension>> IMefHostExportProvider.GetExports<TExtension>()
        {
            return _compositionContext.GetExports<TExtension>().Select(e => new Lazy<TExtension>(() => e));
        }

        IEnumerable<Lazy<TExtension, TMetadata>> IMefHostExportProvider.GetExports<TExtension, TMetadata>()
        {
            var importer = new WithMetadataImporter<TExtension, TMetadata>();
            _compositionContext.SatisfyImports(importer);
            return importer.Exports;
        }

        private class WithMetadataImporter<TExtension, TMetadata>
        {
            [ImportMany]
            public IEnumerable<Lazy<TExtension, TMetadata>> Exports { get; set; }
        }

        #region Defaults

        private static MefHostServices s_defaultHost;
        public static MefHostServices DefaultHost
        {
            get
            {
                if (s_defaultHost == null)
                {
                    var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
                    Interlocked.CompareExchange(ref s_defaultHost, host, null);
                }

                return s_defaultHost;
            }
        }

        private static ImmutableArray<Assembly> s_defaultAssemblies;
        public static ImmutableArray<Assembly> DefaultAssemblies
        {
            get
            {
                if (s_defaultAssemblies.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref s_defaultAssemblies, LoadDefaultAssemblies());
                }

                return s_defaultAssemblies;
            }
        }

        private static ImmutableArray<Assembly> LoadDefaultAssemblies()
        {
            // build a MEF composition using the main workspaces assemblies and the known VisualBasic/CSharp workspace assemblies.
            // updated: includes feature assemblies since they now have public API's.
            var assemblyNames = new string[]
            {
                "Microsoft.CodeAnalysis.Workspaces",
                "Microsoft.CodeAnalysis.CSharp.Workspaces",
                "Microsoft.CodeAnalysis.VisualBasic.Workspaces",
                "Microsoft.CodeAnalysis.Features",
                "Microsoft.CodeAnalysis.CSharp.Features",
                "Microsoft.CodeAnalysis.VisualBasic.Features"
            };

            return LoadNearbyAssemblies(assemblyNames);
        }

        internal static ImmutableArray<Assembly> LoadNearbyAssemblies(string[] assemblyNames)
        {
            var assemblies = new List<Assembly>();

            foreach (var assemblyName in assemblyNames)
            {
                var assembly = TryLoadNearbyAssembly(assemblyName);
                if (assembly != null)
                {
                    assemblies.Add(assembly);
                }
            }

            return assemblies.ToImmutableArray();
        }

        private static Assembly TryLoadNearbyAssembly(string assemblySimpleName)
        {
            var thisAssemblyName = typeof(MefHostServices).GetTypeInfo().Assembly.GetName();
            var assemblyShortName = thisAssemblyName.Name;
            var assemblyVersion = thisAssemblyName.Version;
            var publicKeyToken = thisAssemblyName.GetPublicKeyToken().Aggregate(string.Empty, (s, b) => s + b.ToString("x2"));

            if (string.IsNullOrEmpty(publicKeyToken))
            {
                publicKeyToken = "null";
            }

            var assemblyName = new AssemblyName(string.Format("{0}, Version={1}, Culture=neutral, PublicKeyToken={2}", assemblySimpleName, assemblyVersion, publicKeyToken));

            try
            {
                return Assembly.Load(assemblyName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        internal readonly struct TestAccessor
        {
            /// <summary>
            /// Injects replacement behavior for the <see cref="Create(IEnumerable{Assembly})"/> method.
            /// </summary>
            internal static void HookServiceCreation(CreationHook hook)
            {
                s_CreationHook = hook;

                // The existing host, if any, is not retained past this call.
                s_defaultHost = null;
            }
        }
    }
}
