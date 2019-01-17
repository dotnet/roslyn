// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    public class BasicFormatting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicFormatting(VisualStudioInstanceFactory instanceFactory)
            : base(nameof(BasicFormatting))
        {
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Formatting)]
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

            VisualStudioInstance.Editor.FormatDocument();
            VisualStudioInstance.Editor.Verify.TextContains(
@"Module A
    Sub Main(args As String())

    End Sub
End Module");
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Formatting)]
        public void VerifyCaseCorrection()
        {
            SetUpEditor(@"
$$module A
end module");
            VisualStudioInstance.Editor.FormatDocument();
            VisualStudioInstance.Editor.Verify.TextContains(@"
Module A
End Module");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/18065"),
         TestProperty(Traits.Feature, Traits.Features.Formatting)]
        public void ShiftEnterWithIntelliSenseAndBraceMatching()
        {
            SetUpEditor(@"
Module Program
    Function Main(ooo As Object) As Object
        Return Main$$
    End Function
End Module");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Editor.SendKeys("(o", new KeyPress(VirtualKey.Enter, ShiftState.Shift), "'comment");
            VisualStudioInstance.Editor.Verify.TextContains(@"
Module Program
    Function Main(ooo As Object) As Object
        Return Main(ooo)
        'comment
    End Function
End Module");
        }
    }
}
