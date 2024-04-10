// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Shell.TableControl;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.GoToDefinition)]
    public partial class CSharpGoToDefinition : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGoToDefinition()
            : base(nameof(CSharpGoToDefinition))
        {
        }

        [IdeFact]
        public async Task GoToClassDeclaration()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileDef.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileDef.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"class SomeClass
{
}", HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileConsumer.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileConsumer.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"class SomeOtherClass
{
    SomeClass sc;
}", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("SomeClass", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal($"FileDef.cs", await TestServices.Shell.GetActiveDocumentFileNameAsync(HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.TextContainsAsync(@"class $$SomeClass", assertCaretPosition: true, HangMitigatingCancellationToken);
            Assert.False(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task GoToDefinitionOpensProvisionalTabIfDocumentNotAlreadyOpen()
        {
            var project = ProjectName;
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileDef.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileDef.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"class SomeClass
{
}
", HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.CloseCodeFileAsync(project, "FileDef.cs", saveFile: true, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddFileAsync(project, "FileConsumer.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(project, "FileConsumer.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"class SomeOtherClass
{
    SomeClass sc;
}", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("SomeClass", charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("FileDef.cs", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.TextContainsAsync(@"class $$SomeClass", assertCaretPosition: true, HangMitigatingCancellationToken);
            Assert.True(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task GoToDefinitionWithMultipleResults()
        {
            await SetUpEditorAsync(
@"partial class /*Marker*/ $$PartialClass { }

partial class PartialClass { int i = 0; }", HangMitigatingCancellationToken);

            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("'PartialClass' declarations - Entire solution", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));

            var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);

            Assert.Collection(
                results,
                new Action<ITableEntryHandle2>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "partial class /*Marker*/ PartialClass { }", actual: reference.GetText());
                        Assert.Equal(expected: 0, actual: reference.GetLine());
                        Assert.Equal(expected: 25, actual: reference.GetColumn());
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "partial class PartialClass { int i = 0; }", actual: reference.GetText());
                        Assert.Equal(expected: 2, actual: reference.GetLine());
                        Assert.Equal(expected: 14, actual: reference.GetColumn());
                    }
                });
        }

        [IdeFact]
        public async Task GoToDefinitionFromMetadataCollapsed()
        {
            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened, language: LanguageName, true);

            await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "C.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "C.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"using System;

class C
{
    public override string ToString()
    {
        return ""C"";
    }
}", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("override", charsOffset: -1, HangMitigatingCancellationToken);

            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("Object [decompiled] [Read Only]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));

            var actual = await TestServices.Editor.GetOutliningSpansAsync(HangMitigatingCancellationToken);

            // When collapsing, not everything is collapsed (eg, namespace and class aren't), but most things are
            Assert.Equal(31, actual.Length);
            Assert.Equal(7, actual.Count(s => !s.Collapsed));
        }

        [IdeFact]
        public async Task GoToDefinitionFromMetadataNotCollapsed()
        {
            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);

            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened, language: LanguageName, false);

            await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "C.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "C.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"using System;

class C
{
    public override string ToString()
    {
        return ""C"";
    }
}", HangMitigatingCancellationToken);
            await TestServices.Editor.PlaceCaretAsync("override", charsOffset: -1, HangMitigatingCancellationToken);

            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("Object [decompiled] [Read Only]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));

            var actual = await TestServices.Editor.GetOutliningSpansAsync(HangMitigatingCancellationToken);

            Assert.Equal(31, actual.Length);
            Assert.Equal(1, actual.Count(s => s.Collapsed));
        }

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/70376")]
        public async Task GoToDefinitionFromMetadataSecondHop()
        {
            await TestServices.SolutionExplorer.AddDllReferenceAsync(ProjectName, typeof(CSharpGoToDefinition).Assembly.Location, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "C.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "C.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@"using System;

class C
{
    public void Test()
    {
        var helper = new Roslyn.VisualStudio.NewIntegrationTests.CSharp.CSharpGoToBase();
    }
}", HangMitigatingCancellationToken);

            // Purposefully not using this test class as test data, or the strings in this test could be found
            await TestServices.Editor.PlaceCaretAsync("CSharpGoToBase", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("CSharpGoToBase.cs [embedded] [Read Only]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));

            await TestServices.Editor.PlaceCaretAsync("AbstractEditorTest", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("AbstractEditorTest.cs [embedded] [Read Only]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));

            // Close the file and try again. If symbol mapping isn't working, the second GTD to AbstractEditorTest.cs will fail
            await TestServices.SolutionExplorer.CloseActiveWindow(HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync("CSharpGoToBase", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("CSharpGoToBase.cs [embedded] [Read Only]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));

            await TestServices.Editor.PlaceCaretAsync("AbstractEditorTest", charsOffset: -1, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal("AbstractEditorTest.cs [embedded] [Read Only]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
        }

        [IdeTheory]
        [InlineData("ValueTuple<int> valueTuple1;")]
        [InlineData("ValueTuple<int, int> valueTuple2;")]
        [InlineData("ValueTuple<int, int, int> valueTuple3;")]
        [InlineData("ValueTuple<int, int, int, int> valueTuple4;")]
        [InlineData("ValueTuple<int, int, int, int, int> valueTuple5;")]
        [InlineData("ValueTuple<int, int, int, int, int, int> valueTuple6;")]
        [InlineData("ValueTuple<int, int, int, int, int, int, int> valueTuple7;")]
        [InlineData("ValueTuple<int, int, int, int, int, int, int, int> valueTuple8;")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/71680")]
        public async Task TestGotoDefinitionWithValueTuple(string expression)
        {
            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
            globalOptions.SetGlobalOption(BlockStructureOptionsStorage.CollapseSourceLinkEmbeddedDecompiledFilesWhenFirstOpened, language: LanguageName, false);

            await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "C.cs", cancellationToken: HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "C.cs", HangMitigatingCancellationToken);
            await TestServices.Editor.SetTextAsync(
@$"using System;

class C
{{
    void M()
    {{
        {expression}
    }}
}}", HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync("ValueTuple", charsOffset: -1, HangMitigatingCancellationToken);

            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal($"ValueTuple [{FeaturesResources.Decompiled}] [Read Only]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
        }
    }
}
