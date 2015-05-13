// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class AssemblyReferenceResolver : MetadataReferenceResolver
    {
        internal readonly MetadataFileReferenceResolver PathResolver;
        internal readonly MetadataFileReferenceProvider Provider;

        public AssemblyReferenceResolver(MetadataFileReferenceResolver pathResolver, MetadataFileReferenceProvider provider)
        {
            Debug.Assert(pathResolver != null && provider != null);
            this.PathResolver = pathResolver;
            this.Provider = provider;
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            var path = PathResolver.ResolveReference(reference, baseFilePath);
            if (path == null)
            {
                return ImmutableArray<PortableExecutableReference>.Empty;
            }

            return ImmutableArray.Create(Provider.GetReference(path, properties));
        }

        public override bool Equals(object other)
        {
            return Equals(other as AssemblyReferenceResolver);
        }

        public bool Equals(AssemblyReferenceResolver other)
        {
            return other != null
                && PathResolver.Equals(other.PathResolver)
                && ReferenceEquals(Provider, other.Provider);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(PathResolver, Hash.Combine(Provider, 0));
        }
    }
}
