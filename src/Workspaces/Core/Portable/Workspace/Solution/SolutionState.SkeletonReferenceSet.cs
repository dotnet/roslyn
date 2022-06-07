// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionState
{
    private sealed class SkeletonReferenceSet
    {
        /// <summary>
        /// The actual assembly metadata produced from another compilation.
        /// </summary>
        private readonly AssemblyMetadata _metadata;

        private readonly string? _assemblyName;

        /// <summary>
        /// The documentation provider used to lookup xml docs for any metadata reference we pass out.  See
        /// docs on <see cref="DeferredDocumentationProvider"/> for why this is safe to hold onto despite it
        /// rooting a compilation internally.
        /// </summary>
        private readonly DeferredDocumentationProvider _documentationProvider;

        /// <summary>
        /// Lock this object while reading/writing from it.  Used so we can return the same reference for the same
        /// properties.  While this is isn't strictly necessary (as the important thing to keep the same is the
        /// AssemblyMetadata), this allows higher layers to see that reference instances are the same which allow
        /// reusing the same higher level objects (for example, the set of references a compilation has).
        /// </summary>
        private readonly Dictionary<MetadataReferenceProperties, PortableExecutableReference> _referenceMap = new();

        public SkeletonReferenceSet(
            AssemblyMetadata metadata,
            string? assemblyName,
            DeferredDocumentationProvider documentationProvider)
        {
            _metadata = metadata;
            _assemblyName = assemblyName;
            _documentationProvider = documentationProvider;
        }

        public PortableExecutableReference GetOrCreateMetadataReference(MetadataReferenceProperties properties)
        {
            lock (_referenceMap)
            {
                if (!_referenceMap.TryGetValue(properties, out var value))
                {
                    value = _metadata.GetReference(
                        _documentationProvider,
                        aliases: properties.Aliases,
                        embedInteropTypes: properties.EmbedInteropTypes,
                        display: _assemblyName);

                    _referenceMap.Add(properties, value);
                }

                return value;
            }
        }
    }
}
