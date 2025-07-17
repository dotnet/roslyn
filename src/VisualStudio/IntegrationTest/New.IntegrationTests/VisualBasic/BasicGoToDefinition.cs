// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.GoToDefinition)]
public class BasicGoToDefinition : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicGoToDefinition()
        : base(nameof(BasicGoToDefinition), WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary)
    {
    }

    [IdeFact]
    public async Task GoToClassDeclaration()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.AddFileAsync(project, "FileDef.vb", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "FileDef.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync(
            """
            Class SomeClass
            End Class
            """, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(project, "FileConsumer.vb", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "FileConsumer.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync(
            """
            Class SomeOtherClass
                Dim gibberish As SomeClass
            End Class
            """, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("SomeClass", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
        Assert.Equal($"FileDef.vb", await TestServices.Shell.GetActiveDocumentFileNameAsync(HangMitigatingCancellationToken));
        await TestServices.EditorVerifier.TextContainsAsync(@"Class $$SomeClass", assertCaretPosition: true, HangMitigatingCancellationToken);
        Assert.False(await TestServices.Shell.IsActiveTabProvisionalAsync(HangMitigatingCancellationToken));
    }

    [IdeTheory, CombinatorialData]
    public async Task ObjectBrowserNavigation(bool navigateToObjectBrowser)
    {
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);

        var project = ProjectName;
        await TestServices.SolutionExplorer.AddFileAsync(project, "ObjBrowser.vb", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "ObjBrowser.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync(@"", HangMitigatingCancellationToken);

        await SetUpEditorAsync(
            """
            Class C
                Dim i As Integer$$
            End Class
            """, HangMitigatingCancellationToken);

        globalOptions.SetGlobalOption(VisualStudioNavigationOptionsStorage.NavigateToObjectBrowser, LanguageNames.VisualBasic, navigateToObjectBrowser);

        // We want to make sure that if navigationToObjectBrowserEnabled = false we are navigating to a source representation; we will disable
        // decompiled sources and embedded sources so that way the type of source (and contents within) are stable.
        globalOptions.SetGlobalOption(MetadataAsSourceOptionsStorage.NavigateToDecompiledSources, false);
        globalOptions.SetGlobalOption(MetadataAsSourceOptionsStorage.NavigateToSourceLinkAndEmbeddedSources, false);

        await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);

        if (navigateToObjectBrowser)
        {
            Assert.Equal("Object Browser", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
        }
        else
        {
            await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
            Assert.Contains("Int32", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
            await TestServices.EditorVerifier.TextContainsAsync("Public Structure Int32", cancellationToken: HangMitigatingCancellationToken);
        }
    }

    [IdeFact]
    public async Task GoToBaseFromMetadataAsSource()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.AddFileAsync(project, "SomeClass.vb", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project, "SomeClass.vb", HangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync(
            """
            Class SomeClass
                Public Overrides Function ToString() As String
                    Return MyBase.ToString()
                End Function
            End Class
            """, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Overrides", charsOffset: -1, HangMitigatingCancellationToken);
        await TestServices.Editor.GoToDefinitionAsync(HangMitigatingCancellationToken);
        Assert.Equal("Object [from metadata] [Read Only]", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
        await TestServices.EditorVerifier.TextContainsAsync(@"Public Overridable Function $$ToString() As String", assertCaretPosition: true);
    }
}
