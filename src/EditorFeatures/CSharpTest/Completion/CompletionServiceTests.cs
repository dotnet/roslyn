// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
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
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class CompletionServiceTests
    {
        [Fact]
        public void AcquireCompletionService()
        {
            var workspace = new AdhocWorkspace();

            var document = workspace
                .AddProject("TestProject", LanguageNames.CSharp)
                .AddDocument("TestDocument.cs", "");

            var service = CompletionService.GetService(document);
            Assert.NotNull(service);
        }

        [Fact]
        public void FindCompletionProvider()
        {
            using var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features.AddParts(typeof(ThirdPartyCompletionProvider)));
            var text = SourceText.From("class C { }");

            var document = workspace.CurrentSolution
                .AddProject("TestProject", "Assembly", LanguageNames.CSharp)
                .AddDocument("TestDocument.cs", text);

            var service = CompletionService.GetService(document);

            // Create an item with ProvderName set to ThirdPartyCompletionProvider
            // We should be able to find the provider object vithout calling into CompletionService for other operations.
            var item = CompletionItem.Create("ThirdPartyCompletionProviderItem");
            item.ProviderName = typeof(ThirdPartyCompletionProvider).FullName;

            var provider = service.GetProvider(item, document.Project);
            Assert.True(provider is ThirdPartyCompletionProvider);
        }

        [ExportCompletionProvider(nameof(ThirdPartyCompletionProvider), LanguageNames.CSharp)]
        [ExtensionOrder(After = nameof(KeywordCompletionProvider))]
        [Shared]
        private sealed class ThirdPartyCompletionProvider : CompletionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public ThirdPartyCompletionProvider()
            {
            }

            public override Task ProvideCompletionsAsync(CompletionContext context)
                => Task.CompletedTask;

            public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
            {
                Assert.Equal(1, options.GetOption(new OptionKey(ThirdPartyOption.Instance, LanguageNames.CSharp)));
                return true;
            }
        }

        private sealed class ThirdPartyOption : IOption
        {
            public static ThirdPartyOption Instance = new();

            public string Feature => "TestOptions";
            public string Name => "Option";
            public Type Type => typeof(int);
            public object DefaultValue => 0;
            public bool IsPerLanguage => true;
            public ImmutableArray<OptionStorageLocation> StorageLocations => ImmutableArray<OptionStorageLocation>.Empty;
        }

        /// <summary>
        /// Ensure that 3rd party can set options on solution and access them from within a custom completion provider.
        /// </summary>
        [Fact]
        public async Task PassThroughOptions1()
        {
            using var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features.AddParts(typeof(ThirdPartyCompletionProvider)));

            var text = SourceText.From("class C { }");

            var document = workspace.CurrentSolution
                .AddProject("TestProject", "Assembly", LanguageNames.CSharp)
                .AddDocument("TestDocument.cs", text);

            var service = CompletionService.GetService(document);
            var options = new TestOptionSet(ImmutableDictionary<OptionKey, object>.Empty.Add(new OptionKey(ThirdPartyOption.Instance, LanguageNames.CSharp), 1));
            service.ShouldTriggerCompletion(text, 1, CompletionTrigger.Invoke, options: options);

#pragma warning disable RS0030 // Do not used banned APIs
            await service.GetCompletionsAsync(document, 1, CompletionTrigger.Invoke, options: options);
#pragma warning restore
        }

        /// <summary>
        /// Ensure that 3rd party can set options on solution and access them from within a custom completion provider.
        /// </summary>
        [Fact]
        public async Task PassThroughOptions2()
        {
            using var workspace = new TestWorkspace(composition: EditorTestCompositions.EditorFeatures.AddParts(typeof(ThirdPartyCompletionProvider)));

            var testDocument = new TestHostDocument("class C {}");
            var project = new TestHostProject(workspace, testDocument, name: "project1");
            workspace.AddTestProject(project);
            workspace.OpenDocument(testDocument.Id);

            Assert.True(workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(
                workspace.CurrentSolution.Options.WithChangedOption(new OptionKey(ThirdPartyOption.Instance, LanguageNames.CSharp), 1))));

            var document = workspace.CurrentSolution.GetDocument(testDocument.Id);
            var text = await document.GetTextAsync();

            var service = CompletionService.GetService(document);
            service.ShouldTriggerCompletion(text, 1, CompletionTrigger.Invoke, options: null);

#pragma warning disable RS0030 // Do not used banned APIs
            await service.GetCompletionsAsync(document, 1, CompletionTrigger.Invoke, options: null);
#pragma warning restore
        }

        [Theory, CombinatorialData]
        public async Task GettingCompletionListShoudNotRunSourceGenerator(bool forkBeforeFreeze)
        {
            var sourceMarkup = """
                using System;

                namespace N
                {
                    public class C1
                    {
                        $$
                    }
                }
                """;
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
            var completionService = document.GetLanguageService<CompletionService>();

            Assert.Equal(0, generatorRanCount);

            if (forkBeforeFreeze)
            {
                // Forking before freezing means we'll have to do extra work to produce the final compilation,
                // but we should still not be running generators. 
                document = document.WithText(SourceText.From(sourceMarkup.Replace("C1", "C2")));
            }

            // We want to make sure import completion providers are also participating.
            var options = CompletionOptions.Default with { ShowItemsFromUnimportedNamespaces = true };
            var completionList = await completionService.GetCompletionsAsync(document, position.Value, options, OptionSet.Empty);

            // We expect completion to run on frozen partial semantic, which won't run source generator.
            Assert.Equal(0, generatorRanCount);

            var expectedItem = forkBeforeFreeze ? "C2" : "C1";
            Assert.True(completionList.ItemsList.Select(item => item.DisplayText).Contains(expectedItem));
        }
    }
}
