// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class SolutionWithSourceGeneratorTests : TestBase
    {
        private static Project AddEmptyProject(Solution solution)
        {
            return solution.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    name: "TestProject",
                    assemblyName: "TestProject",
                    language: LanguageNames.CSharp,
                    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview))).Projects.Single();
        }

        [Theory]
        [CombinatorialData]
        public async Task SourceGeneratorBasedOnAdditionalFileGeneratesSyntaxTreesOnce(
            bool fetchCompilationBeforeAddingGenerator,
            bool generatorSupportsIncrementalUpdates)
        {
            using var workspace = new AdhocWorkspace();
            var analyzerReference = new TestGeneratorReference(new AdditionalFileAddedGenerator() { CanApplyChanges = generatorSupportsIncrementalUpdates });
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference);

            // Optionally fetch the compilation first, which validates that we handle both running the generator
            // when the file already exists, and when it is added incrementally.
            Compilation? originalCompilation = null;

            if (fetchCompilationBeforeAddingGenerator)
            {
                originalCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None);
            }

            project = project.AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var newCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

            Assert.NotSame(originalCompilation, newCompilation);
            var generatedTree = Assert.Single(newCompilation.SyntaxTrees);
            Assert.Equal($"{typeof(AdditionalFileAddedGenerator).Module.ModuleVersionId}_{typeof(AdditionalFileAddedGenerator).FullName}_Test.generated.cs", Path.GetFileName(generatedTree.FilePath));
        }

        [Theory]
        [CombinatorialData]
        public async Task SourceGeneratorsRerunAfterSourceChange(bool generatorSupportsIncrementalUpdates)
        {
            using var workspace = new AdhocWorkspace();
            var analyzerReference = new TestGeneratorReference(new AdditionalFileAddedGenerator() { CanApplyChanges = generatorSupportsIncrementalUpdates });
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddDocument("Hello.cs", "// Source File").Project
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var documentId = project.DocumentIds.Single();

            await AssertCompilationContainsOneRegularAndOneGeneratedFile(project, documentId);

            project = project.Solution.WithDocumentText(documentId, SourceText.From("// Changed Source File")).Projects.Single();

            await AssertCompilationContainsOneRegularAndOneGeneratedFile(project, documentId);

            static async Task AssertCompilationContainsOneRegularAndOneGeneratedFile(Project project, DocumentId documentId)
            {
                var compilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

                var regularDocumentSyntaxTree = await project.GetRequiredDocument(documentId).GetRequiredSyntaxTreeAsync(CancellationToken.None);
                Assert.Contains(regularDocumentSyntaxTree, compilation.SyntaxTrees);

                var generatedSyntaxTree = Assert.Single(compilation.SyntaxTrees.Where(t => t != regularDocumentSyntaxTree));
                Assert.Null(project.GetDocument(generatedSyntaxTree));
            }
        }

        [Fact]
        public async Task PartialCompilationsIncludeGeneratedFilesAfterFullGeneration()
        {
            using var workspace = new AdhocWorkspace();
            var analyzerReference = new TestGeneratorReference(new AdditionalFileAddedGenerator());
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddDocument("Hello.cs", "// Source File").Project
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project;

            var fullCompilation = await project.GetRequiredCompilationAsync(CancellationToken.None);

            Assert.Equal(2, fullCompilation.SyntaxTrees.Count());

            var partialProject = project.Documents.Single().WithFrozenPartialSemantics(CancellationToken.None).Project;
            var partialCompilation = await partialProject.GetRequiredCompilationAsync(CancellationToken.None);

            Assert.Same(fullCompilation, partialCompilation);
        }

        [Fact]
        public async Task CompilationsInCompilationReferencesIncludeGeneratedSourceFiles()
        {
            using var workspace = new AdhocWorkspace();
            var analyzerReference = new TestGeneratorReference(new AdditionalFileAddedGenerator());
            var solution = AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddAdditionalDocument("Test.txt", "Hello, world!").Project.Solution;

            var projectIdWithGenerator = solution.ProjectIds.Single();
            var projectIdWithReference = ProjectId.CreateNewId();

            solution = solution.AddProject(projectIdWithReference, "WithReference", "WithReference", LanguageNames.CSharp)
                               .AddProjectReference(projectIdWithReference, new ProjectReference(projectIdWithGenerator));

            var compilationWithReference = await solution.GetRequiredProject(projectIdWithReference).GetRequiredCompilationAsync(CancellationToken.None);

            var compilationReference = Assert.IsAssignableFrom<CompilationReference>(Assert.Single(compilationWithReference.References));

            var compilationWithGenerator = await solution.GetRequiredProject(projectIdWithGenerator).GetRequiredCompilationAsync(CancellationToken.None);

            Assert.Same(compilationWithGenerator, compilationReference.Compilation);
        }

        private sealed class TestGeneratorReference : AnalyzerReference
        {
            private readonly ISourceGenerator _generator;

            public TestGeneratorReference(ISourceGenerator generator)
            {
                _generator = generator;
            }

            public override string? FullPath => null;
            public override object Id => this;

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language) => ImmutableArray<DiagnosticAnalyzer>.Empty;
            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages() => ImmutableArray<DiagnosticAnalyzer>.Empty;
            public override ImmutableArray<ISourceGenerator> GetGenerators() => ImmutableArray.Create(_generator);
        }

        // TODO: find a way to reuse this implementation from the compiler unit tests
        private sealed class AdditionalFileAddedGenerator : ISourceGenerator
        {
            public bool CanApplyChanges { get; set; } = true;

            public void Execute(SourceGeneratorContext context)
            {
                foreach (var file in context.AdditionalFiles)
                {
                    AddSourceForAdditionalFile(context, file);
                }
            }

            public void Initialize(InitializationContext context)
            {
                // TODO: context.RegisterForAdditionalFileChanges(UpdateContext);
            }

            private static void AddSourceForAdditionalFile(SourceGeneratorContext context, AdditionalText file) => context.AddSource(GetGeneratedFileName(file.Path), SourceText.From("", Encoding.UTF8));

            private static string GetGeneratedFileName(string path) => $"{Path.GetFileNameWithoutExtension(path)}.generated";
        }
    }
}
