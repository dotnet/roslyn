// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
public class BasicGenerateTypeDialog : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicGenerateTypeDialog()
        : base(nameof(BasicGenerateTypeDialog))
    {
    }

    [IdeFact]
    public async Task BasicToCSharp()
    {
        var csProj = "CSProj";
        await TestServices.SolutionExplorer.AddProjectAsync(csProj, WellKnownProjectTemplates.ClassLibrary, LanguageNames.CSharp, HangMitigatingCancellationToken);

        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Class1.vb", HangMitigatingCancellationToken);

        await SetUpEditorAsync(@"
Class C
    Sub Method()
        $$Dim _A As A
    End Sub
End Class
", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Generate new type...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.GenerateTypeDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.SetAccessibilityAsync("Public", HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.SetKindAsync("Structure", HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.SetTargetProjectAsync(csProj, HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.SetTargetFileToNewNameAsync("GenerateTypeTest.cs", HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.ClickOKAsync(HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"Imports CSProj

Class C
    Sub Method()
        Dim _A As A
    End Sub
End Class
", actualText);

        await TestServices.SolutionExplorer.OpenFileAsync(csProj, "GenerateTypeTest.cs", HangMitigatingCancellationToken);
        actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"namespace CSProj
{
    public struct A
    {
    }
}", actualText);
    }

    [IdeFact]
    public async Task SameProject()
    {
        await SetUpEditorAsync(@"
Class C
    Sub Method()
        $$Dim _A As A
    End Sub
End Class
", HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CodeActionAsync("Generate new type...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);
        var project = ProjectName;

        await TestServices.GenerateTypeDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.SetAccessibilityAsync("Public", HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.SetKindAsync("Structure", HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.SetTargetFileToNewNameAsync("GenerateTypeTest", HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.ClickOKAsync(HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(project, "GenerateTypeTest.vb", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"Public Structure A
End Structure
", actualText);

        await TestServices.SolutionExplorer.OpenFileAsync(project, "Class1.vb", HangMitigatingCancellationToken);
        actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains(@"Class C
    Sub Method()
        Dim _A As A
    End Sub
End Class
", actualText);
    }

    [IdeFact]
    public async Task CheckFoldersPopulateComboBox()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.AddFileAsync(project, @"folder1\folder2\GenerateTypeTests.vb", open: true, cancellationToken: HangMitigatingCancellationToken);

        await SetUpEditorAsync(@"Class C
    Sub Method() 
        $$Dim _A As A
    End Sub
End Class
", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Generate new type...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.GenerateTypeDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.SetTargetFileToNewNameAsync("Other", HangMitigatingCancellationToken);

        var folders = await TestServices.GenerateTypeDialog.GetNewFileComboBoxItemsAsync(HangMitigatingCancellationToken);

        Assert.Contains(@"\folder1\", folders);
        Assert.Contains(@"\folder1\folder2\", folders);

        await TestServices.GenerateTypeDialog.ClickCancelAsync(HangMitigatingCancellationToken);
    }
}
