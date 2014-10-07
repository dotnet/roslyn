// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Resolves references to metadata specified in the source (#r directives).
    /// </summary>
    public abstract class MetadataReferenceResolver
    {
        public abstract override bool Equals(object other);
        public abstract override int GetHashCode();
        public abstract ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties);
    }
}
