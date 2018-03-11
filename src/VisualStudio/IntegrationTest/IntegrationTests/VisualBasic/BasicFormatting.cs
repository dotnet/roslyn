﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

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

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18065"), 
         Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ShiftEnterWithIntelliSenseAndBraceMatching()
        {
            SetUpEditor(@"
Module Program
    Function Main(ooo As Object) As Object
        Return Main$$
    End Function
End Module");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
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
