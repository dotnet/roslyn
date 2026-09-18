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
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
public class CSharpGenerateTypeDialog : AbstractEditorTest
{
    private readonly ITestOutputHelper _output;

    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpGenerateTypeDialog(ITestOutputHelper output)
                : base(nameof(CSharpGenerateTypeDialog))
    {
        _output = output;
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

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/85704")]
    public async Task CSharpToBasic()
    {
        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 1");
        var vbProj = "VBProj";
        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 2");
        await TestServices.SolutionExplorer.AddProjectAsync(vbProj, WellKnownProjectTemplates.ClassLibrary, LanguageNames.VisualBasic, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 3");
        var project = ProjectName;
        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 4");
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Class1.cs", HangMitigatingCancellationToken);

        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 5");
        await SetUpEditorAsync("""
            class C
            {
                void Method() 
                { 
                    $$A a;    
                }
            }

            """, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 6");
        await TestServices.EditorVerifier.CodeActionAsync("Generate new type...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 7");
        await TestServices.GenerateTypeDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 8");
        await TestServices.GenerateTypeDialog.SetAccessibilityAsync("public", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 9");
        await TestServices.GenerateTypeDialog.SetKindAsync("interface", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 10");
        await TestServices.GenerateTypeDialog.SetTargetProjectAsync("VBProj", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 11");
        await TestServices.GenerateTypeDialog.SetTargetFileToNewNameAsync("GenerateTypeTest", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 12");
        await TestServices.GenerateTypeDialog.ClickOKAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 13");
        await TestServices.GenerateTypeDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 14");
        await TestServices.SolutionExplorer.OpenFileAsync(vbProj, "GenerateTypeTest.vb", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 15");
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 16");
        Assert.Contains("""
            Public Interface A
            End Interface

            """, actualText);

        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 17");
        await TestServices.SolutionExplorer.OpenFileAsync(project, "Class1.cs", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 18");
        actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpGenerateTypeDialog.CSharpToBasic: action 19");
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
