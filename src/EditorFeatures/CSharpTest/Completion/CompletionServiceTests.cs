// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion
{
    [UseExportProvider]
    public class CompletionServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AcquireCompletionService()
        {
            var workspace = new AdhocWorkspace();

            var document = workspace
                .AddProject("TestProject", LanguageNames.CSharp)
                .AddDocument("TestDocument.cs", "");

            var service = CompletionService.GetService(document);
            Assert.NotNull(service);
        }

        [Theory, CombinatorialData]
        public async Task GettingCompletionListShoudNotRunSourceGenerator(bool forkBeforeFreeze)
        {
            var sourceMarkup = @"
using System;

namespace N
{
    public class C1
    {
        $$
    }
}";
            MarkupTestFile.GetPosition(sourceMarkup.NormalizeLineEndings(), out var source, out int? position);

            var generatorRanCount = 0;
            var generator = new CallbackGenerator(onInit: _ => { }, onExecute: _ => Interlocked.Increment(ref generatorRanCount));

            using var workspace = WorkspaceTestUtilities.CreateWorkspaceWithPartialSemantics();
            var analyzerReference = new TestGeneratorReference(generator);
            var project = SolutionUtilities.AddEmptyProject(workspace.CurrentSolution)
                .AddAnalyzerReference(analyzerReference)
                .AddDocument("Document1.cs", sourceMarkup, filePath: "Document1.cs").Project;

            Assert.True(workspace.SetCurrentSolution(_ => project.Solution, WorkspaceChangeKind.SolutionChanged));

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            var compeltionService = document.GetLanguageService<CompletionService>();

            Assert.Equal(0, generatorRanCount);

            if (forkBeforeFreeze)
            {
                // Forking before freezing means we'll have to do extra work to produce the final compilation,
                // but we should still not be running generators. 
                document = document.WithText(SourceText.From(sourceMarkup.Replace("C1", "C2")));
            }

            // We want to make sure import completion providers are also participating.
            var options = CompletionOptions.From(document.Project.Solution.Options, document.Project.Language);
            var newOptions = options with { ShowItemsFromUnimportedNamespaces = true };
            var (completionList, _) = await compeltionService.GetCompletionsInternalAsync(document, position.Value, options: newOptions);

            // We expect completion to run on frozen partial semantic, which won't run source generator.
            Assert.Equal(0, generatorRanCount);

            var expectedItem = forkBeforeFreeze ? "C2" : "C1";
            Assert.True(completionList.Items.Select(item => item.DisplayText).Contains(expectedItem));
        }
    }
}
