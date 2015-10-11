// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    internal class TestMissingMetadataReferenceResolver : MetadataReferenceResolver
    {
        private readonly Dictionary<string, MetadataReference> _map;
        public readonly List<AssemblyIdentity> ResolutionAttempts = new List<AssemblyIdentity>();

        public TestMissingMetadataReferenceResolver(Dictionary<string, MetadataReference> map)
        {
            _map = map;
        }

        public override PortableExecutableReference ResolveMissingAssembly(AssemblyIdentity identity)
        {
            ResolutionAttempts.Add(identity);

            MetadataReference reference;
            string nameAndVersion = identity.Name + (identity.Version != AssemblyIdentity.NullVersion ? $", {identity.Version}" : "");
            return _map.TryGetValue(nameAndVersion, out reference) ? (PortableExecutableReference)reference : null;
        }

        public override bool ResolveMissingAssemblies => true;
        public override bool Equals(object other) => true;
        public override int GetHashCode() => 1;
        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties) => default(ImmutableArray<PortableExecutableReference>);
    }
}
