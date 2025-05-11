// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.NavigateTo)]
public class CSharpNavigateTo : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpNavigateTo()
        : base(nameof(CSharpNavigateTo))
    {
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/69364")]
    public async Task NavigateTo()
    {
        await using var telemetry = await TestServices.Telemetry.EnableTestTelemetryChannelAsync(HangMitigatingCancellationToken);

        var project = ProjectName;
        await TestServices.SolutionExplorer.AddFileAsync(project, "test1.cs", open: false, contents: @"
class FirstClass
{
    void FirstMethod() { }
}", cancellationToken: HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(project, "test2.cs", open: true, contents: @"
", cancellationToken: HangMitigatingCancellationToken);

        await TestServices.Shell.ShowNavigateToDialogAsync(HangMitigatingCancellationToken);
        await TestServices.Input.SendToNavigateToAsync(["FirstMethod", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);
        Assert.Equal($"test1.cs", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
        Assert.Equal("FirstMethod", await TestServices.Editor.GetSelectedTextAsync(HangMitigatingCancellationToken));

        // Add a VB project and verify that VB files are found when searching from C#
        var vbProject = "VBProject";
        await TestServices.SolutionExplorer.AddProjectAsync(vbProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(vbProject, "vbfile.vb", open: true, cancellationToken: HangMitigatingCancellationToken);

        var isAllInOneSearch = await TestServices.Shell.ShowNavigateToDialogAsync(HangMitigatingCancellationToken);
        await TestServices.Input.SendToNavigateToAsync(["FirstClass", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);
        Assert.Equal($"test1.cs", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
        Assert.Equal("FirstClass", await TestServices.Editor.GetSelectedTextAsync(HangMitigatingCancellationToken));

        if (isAllInOneSearch)
        {
            await telemetry.VerifyFiredAsync(["vs/ide/vbcs/navigateto/search", "vs/ide/search/completed"], HangMitigatingCancellationToken);
        }
        else
        {
            await telemetry.VerifyFiredAsync(["vs/ide/vbcs/navigateto/search", "vs/platform/goto/launch"], HangMitigatingCancellationToken);
        }
    }
}
