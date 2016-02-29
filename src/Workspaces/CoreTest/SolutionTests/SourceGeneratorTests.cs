// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SourceGeneratorTests : TestBase
    {
        [Fact]
        public void TestSourceGenerators()
        {
            string source0 =
@"partial class C
{
    D F() { return (D)G; }
}
class P
{
    static void Main()
    {
    }
}";
            string source1 =
@"partial class C
{
    const object G = null;
}
class D
{
}";
            var generator = new MyGenerator(c => c.AddCompilationUnit("__c", CSharpSyntaxTree.ParseText(source1)));
            var generatorReference = new MyGeneratorReference(ImmutableArray.Create<SourceGenerator>(generator));

            using (var directory = new DisposableDirectory(Temp))
            {
                var outputPath = Path.Combine(directory.Path, "obj", "debug");
                Directory.CreateDirectory(outputPath);

                var projectId = ProjectId.CreateNewId();
                var docId = DocumentId.CreateNewId(projectId);
                var workspace = new AdhocWorkspace();
                var projectInfo = ProjectInfo.Create(
                    projectId,
                    VersionStamp.Default,
                    name: "C",
                    assemblyName: "C.dll",
                    language: LanguageNames.CSharp,
                    outputFilePath: outputPath + Path.DirectorySeparatorChar);
                var solution = workspace.CurrentSolution
                    .AddProject(projectInfo)
                    .AddMetadataReference(projectId, MscorlibRef)
                    .AddDocument(docId, "C.cs", source0)
                    .AddAnalyzerReference(projectId, generatorReference);

                bool ok = workspace.TryApplyChanges(solution);
                Assert.True(ok);

                var actualAnalyzerReferences = solution.GetProject(projectId).AnalyzerReferences;
                Assert.Equal(1, actualAnalyzerReferences.Count);
                Assert.Equal(generatorReference, actualAnalyzerReferences[0]);
                var actualGenerators = actualAnalyzerReferences[0].GetSourceGenerators(LanguageNames.CSharp);
                Assert.Equal(1, actualGenerators.Length);
                Assert.Equal(generator, actualGenerators[0]);

                // Before generating source.
                solution = workspace.CurrentSolution;
                var project = solution.GetProject(projectId);
                Assert.Equal(1, project.DocumentIds.Count);
                var doc = solution.GetDocument(docId);
                var model = doc.GetSemanticModelAsync().Result;
                Assert.NotNull(model);
                var compilation = model.Compilation;
                var trees = compilation.SyntaxTrees.ToArray();
                Assert.Equal(1, trees.Length);

                // After generating source.
                workspace.UpdateGeneratedDocumentsIfNecessary(projectId);
                solution = workspace.CurrentSolution;
                project = solution.GetProject(projectId);
                Assert.Equal(2, project.DocumentIds.Count);
                doc = solution.GetDocument(docId);
                model = doc.GetSemanticModelAsync().Result;
                Assert.NotNull(model);
                compilation = model.Compilation;
                trees = compilation.SyntaxTrees.ToArray();
                Assert.Equal(2, trees.Length);
                var tree = trees[1];
                doc = solution.GetDocument(tree);
                Assert.NotNull(doc);
                Assert.True(doc.State.IsGenerated);
                var actualSource = tree.GetText().ToString();
                Assert.Equal(source1, actualSource);
                var filePath = doc.FilePath;
                Assert.NotNull(filePath);
                Assert.Equal(outputPath, Path.GetDirectoryName(filePath));
                // Workspace should not write files to disk.
                Assert.False(File.Exists(filePath));
            }
        }

        private sealed class MyGenerator : SourceGenerator
        {
            private readonly Action<SourceGeneratorContext> _execute;

            internal MyGenerator(Action<SourceGeneratorContext> execute)
            {
                _execute = execute;
            }

            public override void Execute(SourceGeneratorContext context)
            {
                _execute(context);
            }
        }

        private sealed class MyGeneratorReference : AnalyzerReference
        {
            private readonly ImmutableArray<SourceGenerator> _generators;

            internal MyGeneratorReference(ImmutableArray<SourceGenerator> generators)
            {
                _generators = generators;
            }

            public override string FullPath
            {
                get { throw new NotImplementedException(); }
            }

            public override object Id
            {
                get { throw new NotImplementedException(); }
            }

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
            {
                return ImmutableArray<DiagnosticAnalyzer>.Empty;
            }

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
            {
                return ImmutableArray<DiagnosticAnalyzer>.Empty;
            }

            public override ImmutableArray<SourceGenerator> GetSourceGenerators(string language)
            {
                return _generators;
            }
        }
    }
}
