// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
public class BasicGenerateEqualsAndGetHashCodeDialog : AbstractEditorTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicGenerateEqualsAndGetHashCodeDialog(ITestOutputHelper testOutputHelper)
        : base(nameof(BasicGenerateEqualsAndGetHashCodeDialog))
    {
        _testOutputHelper = testOutputHelper;
    }

    private void Log(string message)
        => _testOutputHelper.WriteLine(message);

    [IdeFact]
    public async Task VerifyCodeRefactoringOfferedAndCanceled()
    {
        Log("Setting up the editor with the class to generate Equals/GetHashCode for");
        await SetUpEditorAsync("""

            Class C
                Dim i as Integer
                Dim j as String
                Dim k as Boolean

            $$
            End Class
            """, HangMitigatingCancellationToken);

        Log("Invoking the code action list");
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        Log("Applying the 'Generate Equals(object)...' code action without blocking");
        await TestServices.EditorVerifier.CodeActionAsync("Generate Equals(object)...", applyFix: true, blockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
        Log("Verifying the Pick Members dialog is open");
        await TestServices.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        Log("Clicking Cancel on the Pick Members dialog");
        await TestServices.PickMembersDialog.ClickCancelAsync(HangMitigatingCancellationToken);
        Log("Getting the resulting editor text");
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains("""

            Class C
                Dim i as Integer
                Dim j as String
                Dim k as Boolean


            End Class
            """, actualText);
    }

    [IdeFact]
    public async Task VerifyCodeRefactoringOfferedAndAccepted()
    {
        Log("Setting up the editor with the class to generate Equals/GetHashCode for");
        await SetUpEditorAsync("""

            Imports TestProj

            Class C
                Dim i as Integer
                Dim j as String
                Dim k as Boolean

            $$
            End Class
            """, HangMitigatingCancellationToken);

        Log("Invoking the code action list");
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        Log("Applying the 'Generate Equals(object)...' code action without blocking");
        await TestServices.EditorVerifier.CodeActionAsync("Generate Equals(object)...", applyFix: true, blockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
        Log("Verifying the Pick Members dialog is open");
        await TestServices.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
        Log("Clicking OK on the Pick Members dialog");
        await TestServices.PickMembersDialog.ClickOKAsync(HangMitigatingCancellationToken);
        Log("Getting the resulting editor text");
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains("""

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
            End Class
            """, actualText);
    }
}
