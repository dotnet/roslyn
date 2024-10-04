// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class CompileTimeSolutionProviderTests
    {
        [Theory]
        [InlineData("razor")]
        [InlineData("cshtml")]
        public async Task TryGetCompileTimeDocumentAsync(string kind)
        {
            var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features);
            var projectId = ProjectId.CreateNewId();

            var projectFilePath = Path.Combine(TempRoot.Root, "a.csproj");
            var additionalFilePath = Path.Combine(TempRoot.Root, "a", $"X.{kind}");
            var designTimeFilePath = Path.Combine(TempRoot.Root, "a", $"X.{kind}.g.cs");

            var generator = new TestSourceGenerator() { ExecuteImpl = context => context.AddSource($"a_X_{kind}.g.cs", "") };
            var sourceGeneratedPathPrefix = Path.Combine(typeof(TestSourceGenerator).Assembly.GetName().Name!, typeof(TestSourceGenerator).FullName);
            var analyzerConfigId = DocumentId.CreateNewId(projectId);
            var documentId = DocumentId.CreateNewId(projectId);
            var additionalDocumentId = DocumentId.CreateNewId(projectId);
            var designTimeDocumentId = DocumentId.CreateNewId(projectId);

            var designTimeSolution = workspace.CurrentSolution.
                AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "proj", "proj", LanguageNames.CSharp, filePath: projectFilePath)).
                WithProjectMetadataReferences(projectId, TargetFrameworkUtil.GetReferences(TargetFramework.NetStandard20)).
                AddAnalyzerReference(projectId, new TestGeneratorReference(generator)).
                AddAdditionalDocument(additionalDocumentId, "additional", SourceText.From(""), filePath: additionalFilePath).
                AddAnalyzerConfigDocument(analyzerConfigId, "config", SourceText.From(""), filePath: "RazorSourceGenerator.razorencconfig").
                AddDocument(documentId, "a.cs", "").
                AddDocument(DocumentInfo.Create(
                    designTimeDocumentId,
                    name: "a",
                    loader: null,
                    filePath: designTimeFilePath,
                    isGenerated: true).WithDesignTimeOnly(true));

            var designTimeDocument = designTimeSolution.GetRequiredDocument(designTimeDocumentId);

            var provider = workspace.Services.GetRequiredService<ICompileTimeSolutionProvider>();
            var compileTimeSolution = provider.GetCompileTimeSolution(designTimeSolution);

            Assert.False(compileTimeSolution.ContainsAnalyzerConfigDocument(analyzerConfigId));
            Assert.False(compileTimeSolution.ContainsDocument(designTimeDocumentId));
            Assert.True(compileTimeSolution.ContainsDocument(documentId));

            var sourceGeneratedDoc = (await compileTimeSolution.Projects.Single().GetSourceGeneratedDocumentsAsync()).Single();

            var compileTimeDocument = await CompileTimeSolutionProvider.TryGetCompileTimeDocumentAsync(designTimeDocument, compileTimeSolution, CancellationToken.None, sourceGeneratedPathPrefix);
            Assert.Same(sourceGeneratedDoc, compileTimeDocument);
        }

        [Fact]
        public async Task GeneratorOutputCachedBetweenAcrossCompileTimeSolutions()
        {
            var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features);
            var projectId = ProjectId.CreateNewId();

            var generatorInvocations = 0;

            var generator = new PipelineCallbackGenerator(context =>
            {
                // We'll replicate a simple example of how the razor generator handles disabling here so the test
                // functions similar to the real world
                var isDisabled = context.AnalyzerConfigOptionsProvider.Select(
                    (o, ct) => o.GlobalOptions.TryGetValue("build_property.SuppressRazorSourceGenerator", out var value) && bool.Parse(value));

                var sources = context.AdditionalTextsProvider.Combine(isDisabled).Select((pair, ct) =>
                {
                    var (additionalText, isDisabledFlag) = pair;

                    if (isDisabledFlag)
                        return null;

                    Interlocked.Increment(ref generatorInvocations);
                    return "// " + additionalText.GetText(ct)!.ToString();
                });

                context.RegisterSourceOutput(sources, (context, s) =>
                {
                    if (s != null)
                        context.AddSource("hint", SourceText.From(s));
                });
            });

            var analyzerConfigId = DocumentId.CreateNewId(projectId);
            var additionalDocumentId = DocumentId.CreateNewId(projectId);

            var analyzerConfigText = "is_global = true\r\nbuild_property.SuppressRazorSourceGenerator = true";

            workspace.SetCurrentSolution(s => s.
                AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "proj", "proj", LanguageNames.CSharp)).
                AddAnalyzerReference(projectId, new TestGeneratorReference(generator)).
                AddAdditionalDocument(additionalDocumentId, "additional", SourceText.From(""), filePath: "additional.razor").
                AddAnalyzerConfigDocument(analyzerConfigId, "config", SourceText.From(analyzerConfigText), filePath: "Z:\\RazorSourceGenerator.razorencconfig"),
                WorkspaceChangeKind.SolutionAdded);

            // Fetch a compilation first for the base solution; we're doing this because currently if we try to move the
            // cached generator state to a snapshot that has no CompilationTracker at all, we won't update the state.
            _ = await workspace.CurrentSolution.GetRequiredProject(projectId).GetCompilationAsync();

            var provider = workspace.Services.GetRequiredService<ICompileTimeSolutionProvider>();
            var compileTimeSolution1 = provider.GetCompileTimeSolution(workspace.CurrentSolution);

            _ = await compileTimeSolution1.GetRequiredProject(projectId).GetCompilationAsync();

            Assert.Equal(1, generatorInvocations);

            // Now do something that shouldn't force the generator to rerun; we must change this through the workspace since the
            // service itself uses versions that won't change otherwise
            var documentId = DocumentId.CreateNewId(projectId);
            workspace.SetCurrentSolution(
                s => s.AddDocument(documentId, "Test.cs", "// source file"),
                WorkspaceChangeKind.DocumentAdded,
                projectId,
                documentId);

            var compileTimeSolution2 = provider.GetCompileTimeSolution(workspace.CurrentSolution);
            Assert.NotSame(compileTimeSolution1, compileTimeSolution2);

            _ = await compileTimeSolution2.GetRequiredProject(projectId).GetCompilationAsync();

            Assert.Equal(1, generatorInvocations);
        }
    }
}
