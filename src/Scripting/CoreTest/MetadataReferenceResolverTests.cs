// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
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
}
