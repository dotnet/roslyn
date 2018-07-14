// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGenerateEqualsAndGetHashCodeDialog : AbstractIdeEditorTest
    {
        public BasicGenerateEqualsAndGetHashCodeDialog()
            : base(nameof(BasicGenerateEqualsAndGetHashCodeDialog))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task VerifyCodeRefactoringOfferedAndCanceledAsync()
        {
            await SetUpEditorAsync(@"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class");

            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Generate Equals(object)...", applyFix: true, willBlockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await VisualStudio.PickMembersDialog.ClickCancelAsync();

            await codeAction;

            var actualText = await VisualStudio.Editor.GetTextAsync();
            var expectedText = @"
Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean


End Class";
            Assert.Contains(expectedText, actualText);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEqualsAndGetHashCode)]
        public async Task VerifyCodeRefactoringOfferedAndAcceptedAsync()
        {
            await SetUpEditorAsync(@"
Imports TestProj

Class C
    Dim i as Integer
    Dim j as String
    Dim k as Boolean

$$
End Class");

            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var codeAction = VisualStudio.Editor.Verify.CodeActionAsync("Generate Equals(object)...", applyFix: true, willBlockUntilComplete: false, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.PickMembersDialog.VerifyOpenAsync(HangMitigatingCancellationToken);
            await VisualStudio.PickMembersDialog.ClickOkAsync();
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb);

            await codeAction;

            var actualText = await VisualStudio.Editor.GetTextAsync();
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
}
