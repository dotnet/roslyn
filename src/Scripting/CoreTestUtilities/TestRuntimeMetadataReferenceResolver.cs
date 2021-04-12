// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    public sealed class TestRuntimeMetadataReferenceResolver : MetadataReferenceResolver
    {
        public static readonly TestRuntimeMetadataReferenceResolver Instance = new TestRuntimeMetadataReferenceResolver();
        private static readonly MetadataReferenceProperties s_resolvedMissingAssemblyReferenceProperties = MetadataReferenceProperties.Assembly.WithAliases(ImmutableArray.Create("<implicit>"));

        private TestRuntimeMetadataReferenceResolver() { }

        public override bool Equals(object other) => other == Instance;
        public override int GetHashCode() => 0;
        public override bool ResolveMissingAssemblies => true;

        public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
            // resolve assemblies from the directory containing the test and from directory containing corlib

            string name = referenceIdentity.Name;
            string testDir = Path.GetDirectoryName(GetType().GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName);
            string testDependencyAssemblyPath = Path.Combine(testDir, name + ".dll");
            if (File.Exists(testDependencyAssemblyPath))
            {
                return MetadataReference.CreateFromFile(testDependencyAssemblyPath, s_resolvedMissingAssemblyReferenceProperties);
            }

            string fxDir = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName);
            string fxAssemblyPath = Path.Combine(fxDir, name + ".dll");
            if (File.Exists(fxAssemblyPath))
            {
                return MetadataReference.CreateFromFile(fxAssemblyPath, s_resolvedMissingAssemblyReferenceProperties);
            }

            return null;
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            // Note: currently not handling relative paths, since we don't have tests that use them

            if (File.Exists(reference))
            {
                return ImmutableArray.Create(MetadataReference.CreateFromFile(reference, properties));
            }

            return default(ImmutableArray<PortableExecutableReference>);
        }
    }
}
