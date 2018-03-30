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
        public void NoReferenceDirectives()
        {
            var scriptCode = "1+1";
            var scriptOptions = ScriptOptions.Default.WithMetadataResolver(new TestFakeMetadataResolver());
            var compilation = CSharpScript.Create(scriptCode, scriptOptions).GetCompilation();
            var metadataReferences = compilation.References.ToArray();

            Assert.DoesNotContain(CurrentAssemblyMetadataReference, metadataReferences);
            Assert.DoesNotContain(MicrosoftCodeAnalysisMetadataReference, metadataReferences);
        }

        [Fact]
        public void ReferenceDirectiveTo0MetadataReferences()
        {
            var scriptCode = "#r \"0 references\""; ;
            var scriptOptions = ScriptOptions.Default.WithMetadataResolver(new TestFakeMetadataResolver());
            var compilation = CSharpScript.Create(scriptCode, scriptOptions).GetCompilation();
            var metadataReferences = compilation.References.ToArray();

            Assert.DoesNotContain(CurrentAssemblyMetadataReference, metadataReferences);
            Assert.DoesNotContain(MicrosoftCodeAnalysisMetadataReference, metadataReferences);
        }

        [Fact]
        public void ReferenceDirectiveTo1MetadataReference()
        {
            var scriptCode = "#r \"1 reference\"";
            var scriptOptions = ScriptOptions.Default.WithMetadataResolver(new TestFakeMetadataResolver());
            var compilation = CSharpScript.Create(scriptCode, scriptOptions).GetCompilation();
            var metadataReferences = compilation.References.ToArray();

            Assert.Contains(CurrentAssemblyMetadataReference, metadataReferences);
            Assert.DoesNotContain(MicrosoftCodeAnalysisMetadataReference, metadataReferences);
        }

        [Fact]
        public void ReferenceDirectiveToManyMetadataReferences()
        {
            var scriptCode = "#r \"2 references\"";
            var scriptOptions = ScriptOptions.Default.WithMetadataResolver(new TestFakeMetadataResolver());
            var compilation = CSharpScript.Create(scriptCode, scriptOptions).GetCompilation();
            var metadataReferences = compilation.References.ToArray();

            Assert.Contains(CurrentAssemblyMetadataReference, metadataReferences);
            Assert.Contains(MicrosoftCodeAnalysisMetadataReference, metadataReferences);
        }

        [Fact]
        public void MultipleReferenceDirectiveToManyMetadataReferences()
        {
            var scriptCode = @"#r ""1 reference""
                               #r ""2 references""";
            var scriptOptions = ScriptOptions.Default.WithMetadataResolver(new TestFakeMetadataResolver());
            var compilation = CSharpScript.Create(scriptCode, scriptOptions).GetCompilation();
            var metadataReferences = compilation.References.ToArray();

            Assert.Contains(CurrentAssemblyMetadataReference, metadataReferences);
            Assert.Contains(MicrosoftCodeAnalysisMetadataReference, metadataReferences);
            Assert.True(metadataReferences.Count(r => r == CurrentAssemblyMetadataReference) == 1, "Same MetadataReference, resolved from 2 different #r directives, should appear only once in the compilation");
        }

        private class TestFakeMetadataResolver : MetadataReferenceResolver
        {
            public override int GetHashCode() => 0;

            public bool Equals(TestFakeMetadataResolver other) => ReferenceEquals(this, other);

            public override bool Equals(object other) => Equals(other as TestFakeMetadataResolver);

            public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
            {
                if (reference.Equals("1 reference"))
                {
                    return new PortableExecutableReference[]
                    {
                        CurrentAssemblyMetadataReference
                    }.AsImmutable();
                }

                if (reference.Equals("2 references"))
                {
                    return new PortableExecutableReference[]
                    {
                        CurrentAssemblyMetadataReference,
                        MicrosoftCodeAnalysisMetadataReference
                    }.AsImmutable();
                }

                return Array.Empty<PortableExecutableReference>().ToImmutableArray();
            }
        }
    }
}
