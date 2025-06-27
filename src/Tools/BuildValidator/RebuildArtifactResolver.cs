// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rebuild;
using Microsoft.CodeAnalysis.Text;

namespace BuildValidator
{
    internal sealed class RebuildArtifactResolver : IRebuildArtifactResolver
    {
        internal LocalSourceResolver SourceResolver { get; }
        internal LocalReferenceResolver ReferenceResolver { get; }

        internal RebuildArtifactResolver(LocalSourceResolver sourceResolver, LocalReferenceResolver referenceResolver)
        {
            SourceResolver = sourceResolver;
            ReferenceResolver = referenceResolver;
        }

        public SourceText ResolveSourceText(SourceTextInfo sourceTextInfo)
            => SourceResolver.ResolveSource(sourceTextInfo);

        public MetadataReference ResolveMetadataReference(MetadataReferenceInfo metadataReferenceInfo)
        {
            if (!ReferenceResolver.TryResolveReferences(metadataReferenceInfo, out var metadataReference))
            {
                throw new InvalidOperationException($"Could not resolve reference: {metadataReferenceInfo.FileName}");
            }

            return metadataReference;
        }
    }
}
