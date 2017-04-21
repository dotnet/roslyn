// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal class TestMissingMetadataReferenceResolver : MetadataReferenceResolver
    {
        internal struct ReferenceAndIdentity
        {
            public readonly MetadataReference Reference;
            public readonly AssemblyIdentity Identity;

            public ReferenceAndIdentity(MetadataReference reference, AssemblyIdentity identity)
            {
                Reference = reference;
                Identity = identity;
            }

            public override string ToString()
            {
                return $"{Reference.Display} -> {Identity.GetDisplayName()}";
            }
        }

        private readonly Dictionary<string, MetadataReference> _map;
        public readonly List<ReferenceAndIdentity> ResolutionAttempts = new List<ReferenceAndIdentity>();

        public TestMissingMetadataReferenceResolver(Dictionary<string, MetadataReference> map)
        {
            _map = map;
        }

        public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
            ResolutionAttempts.Add(new ReferenceAndIdentity(definition, referenceIdentity));

            MetadataReference reference;
            string nameAndVersion = referenceIdentity.Name + (referenceIdentity.Version != AssemblyIdentity.NullVersion ? $", {referenceIdentity.Version}" : "");
            return _map.TryGetValue(nameAndVersion, out reference) ? (PortableExecutableReference)reference : null;
        }

        public override bool ResolveMissingAssemblies => true;
        public override bool Equals(object other) => true;
        public override int GetHashCode() => 1;
        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties) => default(ImmutableArray<PortableExecutableReference>);

        public void VerifyResolutionAttempts(params string[] expected)
        {
            AssertEx.Equal(expected, ResolutionAttempts.Select(a => a.ToString()));
        }
    }
}
