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

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.NavigateTo)]
public class BasicNavigateTo : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicNavigateTo()
        : base(nameof(BasicNavigateTo))
    {
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/69364")]
    public async Task NavigateTo()
    {
        var project = ProjectName;
        var csProject = "CSProject";
        await TestServices.SolutionExplorer.AddFileAsync(project, "test1.vb", open: false, contents: @"
Class FirstClass
    Sub FirstMethod()
    End Sub
End Class", cancellationToken: HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(project, "test2.vb", open: true, contents: @"
", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.Shell.ShowNavigateToDialogAsync(HangMitigatingCancellationToken);
        await TestServices.Input.SendToNavigateToAsync(["FirstMethod", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);

        Assert.Equal($"test1.vb", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
        Assert.Equal("FirstMethod", await TestServices.Editor.GetSelectedTextAsync(HangMitigatingCancellationToken));

        // Verify C# files are found when navigating from VB
        await TestServices.SolutionExplorer.AddProjectAsync(csProject, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(csProject, "csfile.cs", open: true, cancellationToken: HangMitigatingCancellationToken);

        await TestServices.Shell.ShowNavigateToDialogAsync(HangMitigatingCancellationToken);
        await TestServices.Input.SendToNavigateToAsync(["FirstClass", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workarounds.WaitForNavigationAsync(HangMitigatingCancellationToken);

        Assert.Equal($"test1.vb", await TestServices.Shell.GetActiveWindowCaptionAsync(HangMitigatingCancellationToken));
        Assert.Equal("FirstClass", await TestServices.Editor.GetSelectedTextAsync(HangMitigatingCancellationToken));
    }
}
