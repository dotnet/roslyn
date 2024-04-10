// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    [Trait(Traits.Feature, Traits.Features.Formatting)]
    public class BasicFormatting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicFormatting()
            : base(nameof(BasicFormatting))
        {
        }

        [IdeFact]
        public async Task VerifyFormattingIndent()
        {
            var testCode = new StringBuilder()
                .AppendLine("$$Module A")
                .AppendLine("    Sub Main(args As String())")
                .AppendLine("    ")
                .AppendLine("        End Sub")
                .AppendLine("End Module")
                .ToString();

            await SetUpEditorAsync(testCode, HangMitigatingCancellationToken);

            await TestServices.Editor.FormatDocumentAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(
@"Module A
    Sub Main(args As String())

    End Sub
End Module", cancellationToken: HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task VerifyCaseCorrection()
        {
            await SetUpEditorAsync(@"
$$module A
end module", HangMitigatingCancellationToken);
            await TestServices.Editor.FormatDocumentAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"
Module A
End Module", cancellationToken: HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task ShiftEnterWithIntelliSenseAndBraceMatching()
        {
            await SetUpEditorAsync(@"
Module Program
    Function Main(ooo As Object) As Object
        Return Main$$
    End Function
End Module", HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync(["(o", (VirtualKeyCode.RETURN, VirtualKeyCode.SHIFT), "'comment"], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"
Module Program
    Function Main(ooo As Object) As Object
        Return Main(ooo)
        'comment
    End Function
End Module", cancellationToken: HangMitigatingCancellationToken);
        }
    }
}
