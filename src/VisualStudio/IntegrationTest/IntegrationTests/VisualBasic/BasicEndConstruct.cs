// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicEndConstruct : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicEndConstruct( )
            : base( nameof(BasicEndConstruct))
        {
        }

        [TestMethod, TestCategory(Traits.Features.EndConstructGeneration)]
        public void EndConstruct()
        {
            SetUpEditor(@"
Class Program
    Sub Main()
        If True Then $$
    End Sub
End Class");
            // Send a space to convert virtual whitespace into real whitespace
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Enter, " ");
            VisualStudioInstance.Editor.Verify.TextContains(@"
Class Program
    Sub Main()
        If True Then
             $$
        End If
    End Sub
End Class", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.EndConstructGeneration)]
        public void IntelliSenseCompletedWhile()
        {
            SetUpEditor(@"
Class Program
    Sub Main()
        $$
    End Sub
End Class");
            // Send a space to convert virtual whitespace into real whitespace
            VisualStudioInstance.Editor.SendKeys("While True", VirtualKey.Enter, " ");
            VisualStudioInstance.Editor.Verify.TextContains(@"
Class Program
    Sub Main()
        While True
             $$
        End While
    End Sub
End Class", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.EndConstructGeneration)]
        public void InterfaceToClassFixup()
        {
            SetUpEditor(@"
Interface$$ C
End Interface");

            VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKey.Backspace, ShiftState.Ctrl));
            VisualStudioInstance.Editor.SendKeys("Class", VirtualKey.Tab);
            VisualStudioInstance.Editor.Verify.TextContains(@"
Class C
End Class");
        }

        [TestMethod, TestCategory(Traits.Features.EndConstructGeneration)]
        public void CaseInsensitveSubToFunction()
        {
            SetUpEditor(@"
Class C
    Public Sub$$ Goo()
    End Sub
End Class");

            VisualStudioInstance.Editor.SendKeys(new KeyPress(VirtualKey.Backspace, ShiftState.Ctrl));
            VisualStudioInstance.Editor.SendKeys("fu", VirtualKey.Tab);
            VisualStudioInstance.Editor.Verify.TextContains(@"
Class C
    Public Function Goo()
    End Function
End Class");
        }
    }
}
