// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
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
            => new(exportProvider);

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
            var key = new ExportKey(typeof(TExtension).AssemblyQualifiedName!, typeof(TMetadata).AssemblyQualifiedName!);
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
            var key = new ExportKey(typeof(TExtension).AssemblyQualifiedName!, "");
            if (!_exportsMap.TryGetValue(key, out var exports))
            {
                exports = ImmutableInterlocked.GetOrAdd(ref _exportsMap, key, _ =>
                    _exportProvider.GetExports<TExtension>().ToImmutableArray());
            }

            return (IEnumerable<Lazy<TExtension>>)exports;
        }

        private readonly struct ExportKey : IEquatable<ExportKey>
        {
            internal readonly string ExtensionTypeName;
            internal readonly string MetadataTypeName;

            public ExportKey(string extensionTypeName, string metadataTypeName)
            {
                ExtensionTypeName = extensionTypeName;
                MetadataTypeName = metadataTypeName;
            }

            public bool Equals(ExportKey other)
                => string.Compare(ExtensionTypeName, other.ExtensionTypeName, StringComparison.OrdinalIgnoreCase) == 0 &&
                   string.Compare(MetadataTypeName, other.MetadataTypeName, StringComparison.OrdinalIgnoreCase) == 0;

            public override bool Equals(object? obj)
                => obj is ExportKey key && Equals(key);

            public override int GetHashCode()
                => Hash.Combine(MetadataTypeName.GetHashCode(), ExtensionTypeName.GetHashCode());
        }
    }
}
