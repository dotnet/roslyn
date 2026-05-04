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

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
public class CSharpGenerateTypeDialog : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpGenerateTypeDialog()
                : base(nameof(CSharpGenerateTypeDialog))
    {
    }

    [IdeFact]
    public async Task OpenAndCloseDialog()
    {
        await SetUpEditorAsync("""
            class C
            {
                void Method() 
                { 
                    $$A a;    
                }
            }

            """, HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CodeActionAsync("Generate new type...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.GenerateTypeDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.ClickCancelAsync(HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.VerifyClosedAsync(HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task CSharpToBasic()
    {
        var vbProj = "VBProj";
        await TestServices.SolutionExplorer.AddProjectAsync(vbProj, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic, HangMitigatingCancellationToken);

        var project = ProjectName;
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Class1.cs", HangMitigatingCancellationToken);

        await SetUpEditorAsync("""
            class C
            {
                void Method() 
                { 
                    $$A a;    
                }
            }

            """, HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CodeActionAsync("Generate new type...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.GenerateTypeDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.SetAccessibilityAsync("public", HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.SetKindAsync("interface", HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.SetTargetProjectAsync("VBProj", HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.SetTargetFileToNewNameAsync("GenerateTypeTest", HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.ClickOKAsync(HangMitigatingCancellationToken);
        await TestServices.GenerateTypeDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(vbProj, "GenerateTypeTest.vb", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains("""
            Public Interface A
            End Interface

            """, actualText);

        await TestServices.SolutionExplorer.OpenFileAsync(project, "Class1.cs", HangMitigatingCancellationToken);
        actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains("""
            using VBProj;

            class C
            {
                void Method() 
                { 
                    A a;    
                }
            }

            """, actualText);

    }
}
