// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal class TestMissingMetadataReferenceResolver : MetadataReferenceResolver
    {
        internal readonly struct ReferenceAndIdentity
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

            string nameAndVersion = referenceIdentity.Name + (referenceIdentity.Version != AssemblyIdentity.NullVersion ? $", {referenceIdentity.Version}" : "");
            return _map.TryGetValue(nameAndVersion, out var reference) ? (PortableExecutableReference)reference : null;
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
