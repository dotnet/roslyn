// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.TestSourceGenerator;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Shell.TableControl;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
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

        [IdeFact, Trait(Traits.Feature, Traits.Features.SourceGenerators)]
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

        [IdeTheory, Trait(Traits.Feature, Traits.Features.SourceGenerators)]
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
                var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
                globalOptions.SetGlobalOption(new OptionKey(VisualStudioSyntaxTreeConfigurationService.OptionsMetadata.EnableOpeningSourceGeneratedFilesInWorkspace, language: null), true);
                await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
                Assert.Equal($"{HelloWorldGenerator.GeneratedEnglishClassName}.cs {ServicesVSResources.generated_suffix}", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            }

            await TestServices.Input.SendAsync(new KeyPress(VirtualKey.F12, ShiftState.Shift));

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

        [IdeTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.SourceGenerators)]
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
            await TestServices.Input.SendAsync(new KeyPress(VirtualKey.F12, ShiftState.Shift));

            var results = await TestServices.FindReferencesWindow.GetContentsAsync(HangMitigatingCancellationToken);
            var referenceInGeneratedFile = results.Single(r => r.GetText()?.Contains("<summary>") ?? false);
            await TestServices.FindReferencesWindow.NavigateToAsync(referenceInGeneratedFile, isPreview: isPreview, shouldActivate: true, HangMitigatingCancellationToken);

            // Assert we are in the right file now
            Assert.Equal($"{HelloWorldGenerator.GeneratedEnglishClassName}.cs {ServicesVSResources.generated_suffix}", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            Assert.Equal(isPreview, await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.SourceGenerators)]
        public async Task InvokeNavigateToForGeneratedFile()
        {
            await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd12CmdID.NavigateTo, HangMitigatingCancellationToken);

            await TestServices.Input.SendToNavigateToAsync(HelloWorldGenerator.GeneratedEnglishClassName, VirtualKey.Enter);
            await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);

            Assert.Equal($"{HelloWorldGenerator.GeneratedEnglishClassName}.cs [generated]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            Assert.Equal("HelloWorld", await TestServices.Editor.GetSelectedTextAsync(HangMitigatingCancellationToken));
        }
    }
}
