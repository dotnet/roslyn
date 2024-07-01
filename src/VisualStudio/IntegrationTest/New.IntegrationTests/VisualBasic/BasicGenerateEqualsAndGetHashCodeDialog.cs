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

[Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
public class BasicGenerateEqualsAndGetHashCodeDialog : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicGenerateEqualsAndGetHashCodeDialog()
        : base(nameof(BasicGenerateEqualsAndGetHashCodeDialog))
    {
    }

    [IdeFact]
    public async Task VerifyCodeRefactoringOfferedAndCanceled()
    {
        await SetUpEditorAsync(@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class", HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Generate Equals(object)...", applyFix: true, blockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        await TestServices.PickMembersDialog.ClickCancelAsync(HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        var expectedText = @"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean


End Class";
        Assert.Contains(expectedText, actualText);
    }

    [IdeFact]
    public async Task VerifyCodeRefactoringOfferedAndAccepted()
    {
        await SetUpEditorAsync(@"
Imports TestProj

Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class", HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Generate Equals(object)...", applyFix: true, blockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        await TestServices.PickMembersDialog.ClickOKAsync(HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        var expectedText = @"
Imports TestProj

Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim c = TryCast(obj, C)
        Return c IsNot Nothing AndAlso
               i = c.i AndAlso
               j = c.j AndAlso
               k = c.k
    End Function
End Class";
        Assert.Contains(expectedText, actualText);
    }
}
