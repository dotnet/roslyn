// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// A class that provides host services via classes instances exported via a MEF version 1 composition.
    /// </summary>
    public class MefV1HostServices : HostServices, IMefHostExportProvider
    {
        internal delegate MefV1HostServices CreationHook(IEnumerable<Assembly> assemblies);

        /// <summary>
        /// This delegate allows test code to override the behavior of <see cref="Create(IEnumerable{Assembly})"/>.
        /// </summary>
        /// <seealso cref="TestAccessor.HookServiceCreation"/>
        private static CreationHook s_CreationHook;

        // the export provider for the MEF composition
        private readonly ExportProvider _exportProvider;

        // accumulated cache for exports
        private ImmutableDictionary<ExportKey, IEnumerable> _exportsMap
            = ImmutableDictionary<ExportKey, IEnumerable>.Empty;

        private MefV1HostServices(ExportProvider exportProvider)
        {
            _exportProvider = exportProvider;
        }

        public static MefV1HostServices Create(ExportProvider exportProvider)
        {
            if (exportProvider == null)
            {
                throw new ArgumentNullException(nameof(exportProvider));
            }

            return new MefV1HostServices(exportProvider);
        }

        public static MefV1HostServices Create(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            if (s_CreationHook != null)
            {
                return s_CreationHook(assemblies);
            }

            var catalog = new AggregateCatalog(assemblies.Select(a => new AssemblyCatalog(a)));
            var container = new CompositionContainer(catalog, compositionOptions: CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);
            return new MefV1HostServices(container);
        }

        /// <summary>
        /// Creates a new <see cref="HostWorkspaceServices"/> associated with the specified workspace.
        /// </summary>
        protected internal override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
        {
            return new MefWorkspaceServices(this, workspace);
        }

        /// <summary>
        /// Gets all the MEF exports of the specified type with the specified metadata.
        /// </summary>
        public IEnumerable<Lazy<TExtension, TMetadata>> GetExports<TExtension, TMetadata>()
        {
            var key = new ExportKey(typeof(TExtension).AssemblyQualifiedName, typeof(TMetadata).AssemblyQualifiedName);
            if (!_exportsMap.TryGetValue(key, out var exports))
            {
                exports = ImmutableInterlocked.GetOrAdd(ref _exportsMap, key, _ =>
                {
                    return _exportProvider.GetExports<TExtension, TMetadata>().ToImmutableArray();
                });
            }

            return (IEnumerable<Lazy<TExtension, TMetadata>>)exports;
        }

        /// <summary>
        /// Gets all the MEF exports of the specified type.
        /// </summary>
        public IEnumerable<Lazy<TExtension>> GetExports<TExtension>()
        {
            var key = new ExportKey(typeof(TExtension).AssemblyQualifiedName, "");
            if (!_exportsMap.TryGetValue(key, out var exports))
            {
                exports = ImmutableInterlocked.GetOrAdd(ref _exportsMap, key, _ =>
                    _exportProvider.GetExports<TExtension>().ToImmutableArray());
            }

            return (IEnumerable<Lazy<TExtension>>)exports;
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        private struct ExportKey : IEquatable<ExportKey>
        {
            internal readonly string ExtensionTypeName;
            internal readonly string MetadataTypeName;

            public ExportKey(string extensionTypeName, string metadataTypeName)
            {
                this.ExtensionTypeName = extensionTypeName;
                this.MetadataTypeName = metadataTypeName;
                Hash.Combine(metadataTypeName.GetHashCode(), extensionTypeName.GetHashCode());
            }

            public bool Equals(ExportKey other)
            {
                return string.Compare(this.ExtensionTypeName, other.ExtensionTypeName, StringComparison.OrdinalIgnoreCase) == 0
                    && string.Compare(this.MetadataTypeName, other.MetadataTypeName, StringComparison.OrdinalIgnoreCase) == 0;
            }

            public override bool Equals(object obj)
                => obj is ExportKey exportKey && this.Equals(exportKey);

            public override int GetHashCode()
            {
                return Hash.Combine(this.MetadataTypeName.GetHashCode(), this.ExtensionTypeName.GetHashCode());
            }
        }

        internal readonly struct TestAccessor
        {
            private readonly MefV1HostServices _mefV1HostServices;

            public TestAccessor(MefV1HostServices mefV1HostServices)
            {
                _mefV1HostServices = mefV1HostServices;
            }

            /// <summary>
            /// Injects replacement behavior for the <see cref="Create(IEnumerable{Assembly})"/> method.
            /// </summary>
            /// <param name="hook"></param>
            internal static void HookServiceCreation(CreationHook hook)
            {
                s_CreationHook = hook;
            }
        }
    }
}
