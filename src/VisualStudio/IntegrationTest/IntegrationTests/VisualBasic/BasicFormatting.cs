// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicFormatting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicFormatting(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicFormatting))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void VerifyFormattingIndent()
        {
            var testCode = new StringBuilder()
                .AppendLine("$$Module A")
                .AppendLine("    Sub Main(args As String())")
                .AppendLine("    ")
                .AppendLine("        End Sub")
                .AppendLine("End Module")
                .ToString();

            SetUpEditor(testCode);

            VisualStudio.Editor.FormatDocument();
            VisualStudio.Editor.Verify.TextContains(
@"Module A
    Sub Main(args As String())

    End Sub
End Module");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void VerifyCaseCorrection()
        {
            SetUpEditor(@"
$$module A
end module");
            VisualStudio.Editor.FormatDocument();
            VisualStudio.Editor.Verify.TextContains(@"
Module A
End Module");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/18065"),
         Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ShiftEnterWithIntelliSenseAndBraceMatching()
        {
            SetUpEditor(@"
Module Program
    Function Main(ooo As Object) As Object
        Return Main$$
    End Function
End Module");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.Editor.SendKeys("(o", new KeyPress(VirtualKey.Enter, ShiftState.Shift), "'comment");
            VisualStudio.Editor.Verify.TextContains(@"
Module Program
    Function Main(ooo As Object) As Object
        Return Main(ooo)
        'comment
    End Function
End Module");
        }
    }
}
