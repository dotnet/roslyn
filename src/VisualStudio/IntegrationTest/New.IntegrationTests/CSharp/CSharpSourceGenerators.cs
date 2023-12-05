// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.TestSourceGenerator;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.TableControl;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.SourceGenerators)]
    public class CSharpSourceGenerators : AbstractEditorTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public CSharpSourceGenerators(ITestOutputHelper testOutputHelper)
            : base(nameof(CSharpSourceGenerators), WellKnownProjectTemplates.ConsoleApplication)
        {
            _testOutputHelper = testOutputHelper;
        }

        protected override string LanguageName => LanguageNames.CSharp;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            await TestServices.SolutionExplorer.AddAnalyzerReferenceAsync(ProjectName, typeof(HelloWorldGenerator).Assembly.Location, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(new[] { FeatureAttribute.Workspace, FeatureAttribute.NavigateTo }, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task GoToDefinitionOpensGeneratedFile()
        {
            await TestServices.Editor.SetTextAsync(@"using System;
internal static class Program
{
    public static void Main()
    {
        Console.WriteLine(" + HelloWorldGenerator.GeneratedEnglishClassName + @".GetMessage());
    }
}", HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync(HelloWorldGenerator.GeneratedEnglishClassName, charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal($"{HelloWorldGenerator.GeneratedEnglishClassName}.cs {ServicesVSResources.generated_suffix}", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            Assert.Equal(HelloWorldGenerator.GeneratedEnglishClassName, await TestServices.Editor.GetSelectedTextAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task GoToDefinitionOpensGeneratedFile_InFolder()
        {
            await TestServices.Editor.SetTextAsync($$"""
                class C
                {
                    void M({{HelloWorldGenerator.GeneratedFolderClassName}} x) { }
                }
                """, HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync(HelloWorldGenerator.GeneratedFolderClassName, charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Equal($"{HelloWorldGenerator.GeneratedFolderName}/{HelloWorldGenerator.GeneratedFolderClassName}.cs {ServicesVSResources.generated_suffix}", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            Assert.Equal(HelloWorldGenerator.GeneratedFolderClassName, await TestServices.Editor.GetSelectedTextAsync(HangMitigatingCancellationToken));
        }

        [IdeTheory(Skip = "https://github.com/dotnet/roslyn/issues/64721")]
        [CombinatorialData]
        public async Task FindReferencesForFileWithDefinitionInSourceGeneratedFile(bool invokeFromSourceGeneratedFile)
        {
            await TestServices.Editor.SetTextAsync(@"using System;
internal static class Program
{
    public static void Main()
    {
        Console.WriteLine(" + HelloWorldGenerator.GeneratedEnglishClassName + @".GetMessage());
    }
}", HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync(HelloWorldGenerator.GeneratedEnglishClassName, charsOffset: 0, HangMitigatingCancellationToken);

            if (invokeFromSourceGeneratedFile)
            {
                var workspace = await TestServices.Shell.GetComponentModelServiceAsync<VisualStudioWorkspace>(HangMitigatingCancellationToken);

                // clear configuration options already read by initialization above, so that the global option update below is effective:
                var configurationService = (WorkspaceConfigurationService)workspace.Services.GetRequiredService<IWorkspaceConfigurationService>();
                configurationService.Clear();

                var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
                globalOptions.SetGlobalOption(WorkspaceConfigurationOptionsStorage.EnableOpeningSourceGeneratedFilesInWorkspace, true);

                await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
                Assert.Equal($"{HelloWorldGenerator.GeneratedEnglishClassName}.cs {ServicesVSResources.generated_suffix}", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            }

            await TestServices.Input.SendAsync((VirtualKeyCode.F12, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);

            var results = (await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken)).OrderBy(r => r.GetLine()).ToArray();

            Assert.Collection(
                results,
                new Action<ITableEntryHandle2>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "/// <summary><see cref=\"HelloWorld\" /> is a simple class to fetch the classic message.</summary>", actual: reference.GetText());
                        Assert.Equal(expected: 1, actual: reference.GetLine());
                        Assert.Equal(expected: 24, actual: reference.GetColumn());
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "internal class HelloWorld", actual: reference.GetText());
                        Assert.Equal(expected: 2, actual: reference.GetLine());
                        Assert.Equal(expected: 15, actual: reference.GetColumn());
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "Console.WriteLine(" + HelloWorldGenerator.GeneratedEnglishClassName + ".GetMessage());", actual: reference.GetText());
                        Assert.Equal(expected: 5, actual: reference.GetLine());
                        Assert.Equal(expected: 26, actual: reference.GetColumn());
                    },
                });
        }

        [IdeTheory, CombinatorialData]
        public async Task FindReferencesAndNavigateToReferenceInGeneratedFile(bool isPreview)
        {
            await TestServices.Editor.SetTextAsync(@"using System;
internal static class Program
{
    public static void Main()
    {
        Console.WriteLine(" + HelloWorldGenerator.GeneratedEnglishClassName + @".GetMessage());
    }
}", HangMitigatingCancellationToken);

            await TestServices.Editor.PlaceCaretAsync(HelloWorldGenerator.GeneratedEnglishClassName, charsOffset: 0, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync((VirtualKeyCode.F12, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);

            var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);
            var referenceInGeneratedFile = results.Single(r => r.GetText()?.Contains("<summary>") ?? false);
            await TestServices.FindReferencesWindow.NavigateToAsync(referenceInGeneratedFile, isPreview: isPreview, shouldActivate: true, HangMitigatingCancellationToken);

            // Assert we are in the right file now
            Assert.Equal($"{HelloWorldGenerator.GeneratedEnglishClassName}.cs {ServicesVSResources.generated_suffix}", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            Assert.Equal(isPreview, await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task InvokeNavigateToForGeneratedFile()
        {
            await TestServices.Shell.ShowNavigateToDialogAsync(HangMitigatingCancellationToken);

            await TestServices.Input.SendToNavigateToAsync(new InputKey[] { HelloWorldGenerator.GeneratedEnglishClassName, VirtualKeyCode.RETURN }, HangMitigatingCancellationToken);
            await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);

            Assert.Equal($"{HelloWorldGenerator.GeneratedEnglishClassName}.cs [generated]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            Assert.Equal(HelloWorldGenerator.GeneratedEnglishClassName, await TestServices.Editor.GetSelectedTextAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task InvokeNavigateToForGeneratedFile_InFolder()
        {
            await TestServices.Shell.ShowNavigateToDialogAsync(HangMitigatingCancellationToken);

            await TestServices.Input.SendToNavigateToAsync(new InputKey[] { HelloWorldGenerator.GeneratedFolderClassName, VirtualKeyCode.RETURN }, HangMitigatingCancellationToken);
            await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);

            Assert.Equal($"{HelloWorldGenerator.GeneratedFolderName}/{HelloWorldGenerator.GeneratedFolderClassName}.cs [generated]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            Assert.Equal(HelloWorldGenerator.GeneratedFolderClassName, await TestServices.Editor.GetSelectedTextAsync(HangMitigatingCancellationToken));
        }
    }
}
