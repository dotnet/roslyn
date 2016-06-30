﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [CompilerTrait(CompilerFeature.SourceGenerators)]
    public class SourceGeneratorTests : TestBase
    {
        private sealed class TypeBasedSourceGenerator : ITypeBasedSourceGenerator
        {
            private readonly Action<SourceGeneratorTypeContext> _execute;
            public TypeBasedSourceGenerator(Action<SourceGeneratorTypeContext> execute)
            {
                _execute = execute;
            }

            public void Execute(SourceGeneratorTypeContext context) => _execute(context);
        }

        [Fact]
        public void TrivialTypeBasedGenerator()
        {
            var comp = CSharpCompilation.Create(GetUniqueName(),
                new[] { CSharpSyntaxTree.ParseText(@"[DoNothingGenerator]class C {}") },
                new[] { MscorlibRef });
            const string generatedSrc = @"
partial class C
{
    public void DoNothing() {}
}";
            var generator = new TypeBasedSourceGenerator(ctx =>
            {
                ctx.AddCompilationUnit("C.DoNothing", generatedSrc);
            });

            var dir = Temp.CreateDirectory();
            var generators = ImmutableArray.Create<ITypeBasedSourceGenerator>(generator);
            comp.GenerateSource(generators, "DoNothingGenerator", dir.Path, writeToDisk: true);

            var generatedPath = Path.Combine(dir.Path, "C.DoNothing.cs");
            Assert.True(File.Exists(generatedPath));
            Assert.Equal(generatedSrc, File.ReadAllText(generatedPath));
        }

        [Fact]
        public void NoGenerators()
        {
            string text =
@"class C
{
}";
            var compilation = CSharpCompilation.Create(GetUniqueName(), new[] { CSharpSyntaxTree.ParseText(text) }, new[] { MscorlibRef });
            using (var directory = new DisposableDirectory(Temp))
            {
                var path = directory.Path;
                ImmutableArray<Diagnostic> diagnostics;
                // Default generators array.
                Assert.Throws<ArgumentException>(() =>
                    compilation.GenerateSource(
                        default(ImmutableArray<SourceGenerator>),
                        path,
                        writeToDisk: false,
                        cancellationToken: default(CancellationToken),
                        diagnostics: out diagnostics));
                // Empty generators array.
                var trees = compilation.GenerateSource(
                    ImmutableArray<SourceGenerator>.Empty,
                    path,
                    writeToDisk: false,
                    cancellationToken: default(CancellationToken),
                    diagnostics: out diagnostics);
                Assert.True(trees.IsEmpty);
            }
        }

        /// <summary>
        /// Generate source without persisting.
        /// </summary>
        [Fact]
        public void DoNotPersist()
        {
            string text =
@"class C
{
}";
            var compilation = CSharpCompilation.Create(GetUniqueName(), new[] { CSharpSyntaxTree.ParseText(text) }, new[] { MscorlibRef });
            var generator = new SimpleSourceGenerator(context => context.AddCompilationUnit("__c", CSharpSyntaxTree.ParseText(@"class __C { }")));
            using (var directory = new DisposableDirectory(Temp))
            {
                var path = directory.Path;
                ImmutableArray<Diagnostic> diagnostics;
                var trees = compilation.GenerateSource(
                    ImmutableArray.Create<SourceGenerator>(generator),
                    path,
                    writeToDisk: false,
                    cancellationToken: default(CancellationToken),
                    diagnostics: out diagnostics);
                Assert.Equal(1, trees.Length);
                var filePath = Path.Combine(path, "__c.cs");
                Assert.Equal(filePath, trees[0].FilePath);
                Assert.False(File.Exists(filePath));
            }
        }

        [Fact]
        public void Paths()
        {
            string text =
@"class C
{
}";
            var compilation = CSharpCompilation.Create(GetUniqueName(), new[] { CSharpSyntaxTree.ParseText(text) }, new[] { MscorlibRef });
            var generator = new SimpleSourceGenerator(context => context.AddCompilationUnit("__c", CSharpSyntaxTree.ParseText(@"class __C { }")));
            using (var directory = new DisposableDirectory(Temp))
            {
                ImmutableArray<Diagnostic> diagnostics;
                // Null path.
                Assert.Throws<ArgumentNullException>(() =>
                    compilation.GenerateSource(
                        ImmutableArray.Create<SourceGenerator>(generator),
                        path: null,
                        writeToDisk: false,
                        cancellationToken: default(CancellationToken),
                        diagnostics: out diagnostics));
                // Relative path.
                var path = Path.GetFileName(directory.Path);
                var trees = compilation.GenerateSource(
                    ImmutableArray.Create<SourceGenerator>(generator),
                    path,
                    writeToDisk: false,
                    cancellationToken: default(CancellationToken),
                    diagnostics: out diagnostics);
                Assert.Equal(1, trees.Length);
                var filePath = Path.Combine(path, "__c.cs");
                Assert.Equal(filePath, trees[0].FilePath);
            }
        }

        /// <summary>
        /// Persist SourceText with no explicit encoding.
        /// </summary>
        [Fact]
        public void Encoding()
        {
            Persist(generatedEncoding: null, persistedEncoding: System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Persist SourceText with explicit encoding.
        /// </summary>
        [Fact]
        public void ExplicitEncoding()
        {
            Persist(generatedEncoding: System.Text.Encoding.Unicode, persistedEncoding: System.Text.Encoding.Unicode);
        }

        private void Persist(Encoding generatedEncoding, Encoding persistedEncoding)
        {
            var compilation = CSharpCompilation.Create(GetUniqueName(), new[] { CSharpSyntaxTree.ParseText(@"class C { }") }, new[] { MscorlibRef });
            var generatedText = @"class __C { }";
            var generator = new SimpleSourceGenerator(context => context.AddCompilationUnit("__c", CSharpSyntaxTree.ParseText(generatedText, encoding: generatedEncoding)));
            using (var directory = new DisposableDirectory(Temp))
            {
                var path = directory.Path;
                ImmutableArray<Diagnostic> diagnostics;
                var trees = compilation.GenerateSource(
                    ImmutableArray.Create<SourceGenerator>(generator),
                    path,
                    writeToDisk: true,
                    cancellationToken: default(CancellationToken),
                    diagnostics: out diagnostics);
                Assert.Equal(1, trees.Length);
                var filePath = Path.Combine(path, "__c.cs");
                Assert.Equal(filePath, trees[0].FilePath);
                Assert.True(File.Exists(filePath));
                using (var reader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true))
                {
                    // Need at least one read to get encoding.
                    var persistedText = reader.ReadToEnd();
                    Assert.Equal(persistedEncoding, reader.CurrentEncoding);
                    Assert.Equal(persistedText, generatedText);
                }
            }
        }
    }
}
