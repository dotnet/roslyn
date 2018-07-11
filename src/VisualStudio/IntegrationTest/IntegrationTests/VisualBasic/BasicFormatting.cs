// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicFormatting : AbstractIdeEditorTest
    {
        public BasicFormatting()
            : base(nameof(BasicFormatting))
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [IdeFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task VerifyFormattingIndentAsync()
        {
            var testCode = new StringBuilder()
                .AppendLine("$$Module A")
                .AppendLine("    Sub Main(args As String())")
                .AppendLine("    ")
                .AppendLine("        End Sub")
                .AppendLine("End Module")
                .ToString();

            await SetUpEditorAsync(testCode);

            await VisualStudio.Editor.FormatDocumentAsync();
            await VisualStudio.Editor.Verify.TextContainsAsync(
@"Module A
    Sub Main(args As String())

    End Sub
End Module");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task VerifyCaseCorrectionAsync()
        {
            await SetUpEditorAsync(@"
$$module A
end module");
            await VisualStudio.Editor.FormatDocumentAsync();
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Module A
End Module");
        }

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/18065")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ShiftEnterWithIntelliSenseAndBraceMatchingAsync()
        {
            await SetUpEditorAsync(@"
Module Program
    Function Main(ooo As Object) As Object
        Return Main$$
    End Function
End Module");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.Editor.SendKeysAsync("(o", new KeyPress(VirtualKey.Enter, ShiftState.Shift), "'comment");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
Module Program
    Function Main(ooo As Object) As Object
        Return Main(ooo)
        'comment
    End Function
End Module");
        }
    }
}
