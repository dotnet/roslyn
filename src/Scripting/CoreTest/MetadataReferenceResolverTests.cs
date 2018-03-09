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
        private static PortableExecutableReference CurrentAssemblyMetadataReference = MetadataReference.CreateFromFile(typeof(MetadataReferenceResolverTests).Assembly.Location);
        private static PortableExecutableReference MicrosoftCodeAnalysisMetadataReference = MetadataReference.CreateFromFile(typeof(MetadataReferenceProperties).Assembly.Location);

        [Fact]
        public void ReferenceDirectiveTo0MetadataReferences()
        {
            var scriptCode = "1+1";
            var scriptOptions = ScriptOptions.Default.WithMetadataResolver(new TestFakeMetadataResolver());
            var compilation = CSharpScript.Create(scriptCode, scriptOptions).GetCompilation();
            var metadataReferences = compilation.References.ToArray();

            Assert.DoesNotContain(CurrentAssemblyMetadataReference, metadataReferences);
            Assert.DoesNotContain(MicrosoftCodeAnalysisMetadataReference, metadataReferences);
        }

        [Fact]
        public void ReferenceDirectiveTo1MetadataReference()
        {
            var scriptCode = "#r \"1.dll\"";
            var scriptOptions = ScriptOptions.Default.WithMetadataResolver(new TestFakeMetadataResolver());
            var compilation = CSharpScript.Create(scriptCode, scriptOptions).GetCompilation();
            var metadataReferences = compilation.References.ToArray();

            Assert.Contains(CurrentAssemblyMetadataReference, metadataReferences);
            Assert.DoesNotContain(MicrosoftCodeAnalysisMetadataReference, metadataReferences);
        }

        [Fact]
        public void ReferenceDirectiveToManyMetadataReferences()
        {
            var scriptCode = "#r \"2.dll\"";
            var scriptOptions = ScriptOptions.Default.WithMetadataResolver(new TestFakeMetadataResolver());
            var compilation = CSharpScript.Create(scriptCode, scriptOptions).GetCompilation();
            var metadataReferences = compilation.References.ToArray();

            Assert.Contains(CurrentAssemblyMetadataReference, metadataReferences);
            Assert.Contains(MicrosoftCodeAnalysisMetadataReference, metadataReferences);
        }

        private class TestFakeMetadataResolver : MetadataReferenceResolver
        {
            public override int GetHashCode() => 0;

            public bool Equals(TestFakeMetadataResolver other) => ReferenceEquals(this, other);

            public override bool Equals(object other) => Equals(other as TestFakeMetadataResolver);

            public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
            {
                if (reference.Equals("1.dll"))
                {
                    return new PortableExecutableReference[]
                    {
                        CurrentAssemblyMetadataReference
                    }.AsImmutable();
                }

                return new PortableExecutableReference[]
                {
                    CurrentAssemblyMetadataReference,
                    MicrosoftCodeAnalysisMetadataReference
                }.AsImmutable();
            }
        }
    }
}
