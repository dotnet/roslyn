// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Scripting.Test
{
    public class MetadataReferenceResolverTests
    {
        [Fact]
        public void ReferenceDirectiveTo0MetadataReferences()
        {
            var scriptCode = "1+1";
            var scriptOptions = ScriptOptions.Default.WithMetadataResolver(new TestFakeMetadataResolver());
            var compilation = CSharpScript.Create(scriptCode, scriptOptions).GetCompilation();
            var metadataReferences = compilation.References.ToArray();

            Assert.Equal(1, metadataReferences.Length);
        }

        [Fact]
        public void ReferenceDirectiveTo1MetadataReference()
        {
            var scriptCode = "#r \"1.dll\"";
            var scriptOptions = ScriptOptions.Default.WithMetadataResolver(new TestFakeMetadataResolver());
            var compilation = CSharpScript.Create(scriptCode, scriptOptions).GetCompilation();
            var metadataReferences = compilation.References.ToArray();

            Assert.Equal(2, metadataReferences.Length);
        }

        [Fact]
        public void ReferenceDirectiveToManyMetadataReferences()
        {
            var scriptCode = "#r \"2.dll\"";
            var scriptOptions = ScriptOptions.Default.WithMetadataResolver(new TestFakeMetadataResolver());
            var compilation = CSharpScript.Create(scriptCode, scriptOptions).GetCompilation();
            var metadataReferences = compilation.References.ToArray();

            Assert.Equal(4, metadataReferences.Length);
        }

        private class TestFakeMetadataResolver : MetadataReferenceResolver
        {
            public override bool Equals(object other)
            {
                return false;
            }

            public override int GetHashCode()
            {
                return new Random().Next(0, 1000);
            }

            public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
            {
                if (reference.Contains("1.dll"))
                {
                    return new PortableExecutableReference[]
                    {
                        MetadataReference.CreateFromFile(typeof(MetadataReferenceResolverTests).Assembly.Location), // current assembly
                    }.AsImmutable();
                }

                return new PortableExecutableReference[]
                {
                    MetadataReference.CreateFromFile(typeof(MetadataReferenceResolverTests).Assembly.Location), // current assembly
                    MetadataReference.CreateFromFile(typeof(MetadataReferenceProperties).Assembly.Location), // Microsoft.CodeAnalysis
                    MetadataReference.CreateFromFile(typeof(ScriptOptions).Assembly.Location) // Microsoft.CodeAnalysis.Scripting
                }.AsImmutable();
            }
        }
    }

    public class NuGetPackageResolverTests : TestBase
    {
        [Fact]
        public void ParsePackageNameAndVersion()
        {
            ParseInvalidPackageReference("A");
            ParseInvalidPackageReference("A/1");
            ParseInvalidPackageReference("nuget");
            ParseInvalidPackageReference("nuget:");
            ParseInvalidPackageReference("NUGET:");
            ParseInvalidPackageReference("nugetA/1");

            ParseValidPackageReference("nuget:A", "A", "");
            ParseValidPackageReference("nuget:A.B", "A.B", "");
            ParseValidPackageReference("nuget:  ", "  ", "");

            ParseInvalidPackageReference("nuget:A/");
            ParseInvalidPackageReference("nuget:A//1.0");
            ParseInvalidPackageReference("nuget:/1.0.0");
            ParseInvalidPackageReference("nuget:A/B/2.0.0");

            ParseValidPackageReference("nuget::nuget/1", ":nuget", "1");
            ParseValidPackageReference("nuget:A/1", "A", "1");
            ParseValidPackageReference("nuget:A.B/1.0.0", "A.B", "1.0.0");
            ParseValidPackageReference("nuget:A/B.C", "A", "B.C");
            ParseValidPackageReference("nuget:  /1", "  ", "1");
            ParseValidPackageReference("nuget:A\t/\n1.0\r ", "A\t", "\n1.0\r ");
        }

        private static void ParseValidPackageReference(string reference, string expectedName, string expectedVersion)
        {
            string name;
            string version;
            Assert.True(NuGetPackageResolver.TryParsePackageReference(reference, out name, out version));
            Assert.Equal(expectedName, name);
            Assert.Equal(expectedVersion, version);
        }

        private static void ParseInvalidPackageReference(string reference)
        {
            string name;
            string version;
            Assert.False(NuGetPackageResolver.TryParsePackageReference(reference, out name, out version));
            Assert.Null(name);
            Assert.Null(version);
        }
    }
}
