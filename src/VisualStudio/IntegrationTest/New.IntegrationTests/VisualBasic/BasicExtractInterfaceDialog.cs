// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
public class BasicExtractInterfaceDialog : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicExtractInterfaceDialog()
        : base(nameof(BasicExtractInterfaceDialog))
    {
    }

    [IdeFact]
    public async Task CoreScenario()
    {
        await SetUpEditorAsync("""
            Class C$$
                Public Sub M()
                End Sub
            End Class
            """, HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Extract interface...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        await TestServices.ExtractInterfaceDialog.ClickOKAsync(HangMitigatingCancellationToken);
        await TestServices.ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.vb", HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""
            Class C
                Implements IC

                Public Sub M() Implements IC.M
                End Sub
            End Class
            """, cancellationToken: HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "IC.vb", HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""
            Interface IC
                Sub M()
            End Interface
            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task CheckFileName()
    {
        await SetUpEditorAsync("""
            Class C2$$
                Public Sub M()
                End Sub
            End Class
            """, HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Extract interface...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);

        var fileName = await TestServices.ExtractInterfaceDialog.GetTargetFileNameAsync(HangMitigatingCancellationToken);

        Assert.Equal(expected: "IC2.vb", actual: fileName);

        await TestServices.ExtractInterfaceDialog.ClickCancelAsync(HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task CheckSameFile()
    {
        await SetUpEditorAsync("""
            Class C$$
                Public Sub M()
                End Sub
            End Class
            """, HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Extract interface...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);

        await TestServices.ExtractInterfaceDialog.SelectSameFileAsync(HangMitigatingCancellationToken);

        await TestServices.ExtractInterfaceDialog.ClickOKAsync(HangMitigatingCancellationToken);
        await TestServices.ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""
            Interface IC
                Sub M()
            End Interface

            Class C
                Implements IC

                Public Sub M() Implements IC.M
                End Sub
            End Class
            """, cancellationToken: HangMitigatingCancellationToken);

    }

    [IdeFact]
    public async Task CheckSameFileOnlySelectedItems()
    {
        await SetUpEditorAsync("""
            Class C$$
                Public Sub M1()
                Public Sub M2()
                End Sub
            End Class
            """, HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Extract interface...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        await TestServices.ExtractInterfaceDialog.ClickDeselectAllAsync(HangMitigatingCancellationToken);
        await TestServices.ExtractInterfaceDialog.ToggleItemAsync("M2()", HangMitigatingCancellationToken);
        await TestServices.ExtractInterfaceDialog.SelectSameFileAsync(HangMitigatingCancellationToken);
        await TestServices.ExtractInterfaceDialog.ClickOKAsync(HangMitigatingCancellationToken);
        await TestServices.ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""
            Interface IC
                Sub M2()
            End Interface

            Class C
                Implements IC

                Public Sub M1()
                Public Sub M2() Implements IC.M2
                End Sub
            End Class
            """);
    }

    [IdeFact]
    public async Task CheckSameFileNamespace()
    {
        await SetUpEditorAsync("""
            Namespace A
                Class C$$
                    Public Sub M()
                    End Sub
                End Class
            End Namespace
            """, HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Extract interface...",
            applyFix: true,
            blockUntilComplete: false,
            cancellationToken: HangMitigatingCancellationToken);

        await TestServices.ExtractInterfaceDialog.VerifyOpenAsync(HangMitigatingCancellationToken);

        await TestServices.ExtractInterfaceDialog.SelectSameFileAsync(HangMitigatingCancellationToken);

        await TestServices.ExtractInterfaceDialog.ClickOKAsync(HangMitigatingCancellationToken);
        await TestServices.ExtractInterfaceDialog.VerifyClosedAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""
            Namespace A
                Interface IC
                    Sub M()
                End Interface

                Class C
                    Implements IC

                    Public Sub M() Implements IC.M
                    End Sub
                End Class
            End Namespace
            """, cancellationToken: HangMitigatingCancellationToken);
    }
}
