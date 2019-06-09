// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Provides host services imported via VS MEF.
    /// </summary>
    internal sealed class VisualStudioMefHostServices : HostServices, IMefHostExportProvider
    {
        // the export provider for the MEF composition
        private readonly ExportProvider _exportProvider;

        // accumulated cache for exports
        private ImmutableDictionary<ExportKey, IEnumerable> _exportsMap
            = ImmutableDictionary<ExportKey, IEnumerable>.Empty;

        private VisualStudioMefHostServices(ExportProvider exportProvider)
        {
            Contract.ThrowIfNull(exportProvider);
            _exportProvider = exportProvider;
        }

        public static VisualStudioMefHostServices Create(ExportProvider exportProvider)
            => new VisualStudioMefHostServices(exportProvider);

        /// <summary>
        /// Creates a new <see cref="HostWorkspaceServices"/> associated with the specified workspace.
        /// </summary>
        protected internal override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
            => new MefWorkspaceServices(this, workspace);

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

        private struct ExportKey : IEquatable<ExportKey>
        {
            internal readonly string ExtensionTypeName;
            internal readonly string MetadataTypeName;
            private readonly int _hash;

            public ExportKey(string extensionTypeName, string metadataTypeName)
            {
                ExtensionTypeName = extensionTypeName;
                MetadataTypeName = metadataTypeName;
                _hash = Hash.Combine(metadataTypeName.GetHashCode(), extensionTypeName.GetHashCode());
            }

            public bool Equals(ExportKey other)
                => string.Compare(ExtensionTypeName, other.ExtensionTypeName, StringComparison.OrdinalIgnoreCase) == 0 &&
                   string.Compare(MetadataTypeName, other.MetadataTypeName, StringComparison.OrdinalIgnoreCase) == 0;

            public override bool Equals(object obj)
                => obj is ExportKey key && Equals(key);

            public override int GetHashCode()
                => Hash.Combine(MetadataTypeName.GetHashCode(), ExtensionTypeName.GetHashCode());
        }
    }
}
