// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal sealed class WorkspaceMetadataFileReferenceResolver : MetadataReferenceResolver, IEquatable<WorkspaceMetadataFileReferenceResolver>
    {
        private readonly IMetadataService _metadataService;
        private readonly RelativePathResolver _pathResolver;

        public WorkspaceMetadataFileReferenceResolver(IMetadataService metadataService, RelativePathResolver pathResolver)
        {
            Debug.Assert(metadataService != null);
            Debug.Assert(pathResolver != null);

            _metadataService = metadataService;
            _pathResolver = pathResolver;
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            string path = _pathResolver.ResolvePath(reference, baseFilePath);
            if (path == null)
            {
                return ImmutableArray<PortableExecutableReference>.Empty;
            }

            return ImmutableArray.Create(_metadataService.GetReference(path, properties));
        }

        public bool Equals(WorkspaceMetadataFileReferenceResolver other)
        {
            return _metadataService == other._metadataService &&
                   _pathResolver.Equals(other._pathResolver);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_metadataService, Hash.Combine(_pathResolver, 0));
        }

        public override bool Equals(object other) => Equals(other as WorkspaceMetadataFileReferenceResolver);
    }
}
