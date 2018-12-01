// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicIntelliSense : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicIntelliSense( )
            : base( nameof(BasicIntelliSense))
        {
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void IntelliSenseTriggersOnParenWithBraceCompletionAndCorrectUndoMerging()
        {
            SetUpEditor(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module");

            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);

            VisualStudioInstance.SendKeys.Send("dim q as lis(");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("Of");

            VisualStudioInstance.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As List($$)
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudioInstance.SendKeys.Send(
                VirtualKey.Down,
                VirtualKey.Tab);

            VisualStudioInstance.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of$$)
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudioInstance.SendKeys.Send(" inte");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("Integer");

            VisualStudioInstance.SendKeys.Send(')');

            VisualStudioInstance.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of Integer)$$
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudioInstance.SendKeys.Send(Ctrl(VirtualKey.Z));

            VisualStudioInstance.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of inte)$$
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudioInstance.SendKeys.Send(Ctrl(VirtualKey.Z));

            VisualStudioInstance.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of inte$$)
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudioInstance.SendKeys.Send(Ctrl(VirtualKey.Z));

            VisualStudioInstance.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of$$)
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudioInstance.SendKeys.Send(Ctrl(VirtualKey.Z));

            VisualStudioInstance.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As lis($$)
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudioInstance.SendKeys.Send(Ctrl(VirtualKey.Z));

            VisualStudioInstance.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As lis($$
    End Sub
End Module",
assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void TypeAVariableDeclaration()
        {
            SetUpEditor(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module");

            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);

            VisualStudioInstance.SendKeys.Send("dim");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("Dim", "ReDim");

            VisualStudioInstance.SendKeys.Send(' ');
            Assert.AreEqual(false, VisualStudioInstance.Editor.IsCompletionActive());

            VisualStudioInstance.SendKeys.Send('i');
            Assert.AreEqual(false, VisualStudioInstance.Editor.IsCompletionActive());

            VisualStudioInstance.SendKeys.Send(' ');
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("As");

            VisualStudioInstance.SendKeys.Send("a ");
            VisualStudioInstance.SendKeys.Send("intege");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("Integer", "UInteger");

            VisualStudioInstance.SendKeys.Send(' ');
            Assert.AreEqual(false, VisualStudioInstance.Editor.IsCompletionActive());

            VisualStudioInstance.SendKeys.Send('=');
            Assert.AreEqual(true, VisualStudioInstance.Editor.IsCompletionActive());

            VisualStudioInstance.SendKeys.Send(' ');
            Assert.AreEqual(true, VisualStudioInstance.Editor.IsCompletionActive());

            VisualStudioInstance.SendKeys.Send("fooo");
            Assert.AreEqual(false, VisualStudioInstance.Editor.IsCompletionActive());

            VisualStudioInstance.SendKeys.Send(' ');
            Assert.AreEqual(true, VisualStudioInstance.Editor.IsCompletionActive());

            VisualStudioInstance.SendKeys.Send(VirtualKey.Backspace);
            Assert.AreEqual(false, VisualStudioInstance.Editor.IsCompletionActive());

            VisualStudioInstance.SendKeys.Send(VirtualKey.Backspace);
            Assert.AreEqual(true, VisualStudioInstance.Editor.IsCompletionActive());

            VisualStudioInstance.SendKeys.Send(
                VirtualKey.Left,
                VirtualKey.Delete);
            Assert.AreEqual(true, VisualStudioInstance.Editor.IsCompletionActive());
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void DismissIntelliSenseOnApostrophe()
        {
            SetUpEditor(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module");

            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);

            VisualStudioInstance.SendKeys.Send("dim q as ");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("_AppDomain");

            VisualStudioInstance.SendKeys.Send("'");
            Assert.AreEqual(false, VisualStudioInstance.Editor.IsCompletionActive());
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"Module Module1
    Sub Main()
        Dim q As '
    End Sub
End Module", actualText);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void TypeLeftAngleAfterImports()
        {
            SetUpEditor(@"
Imports$$");

            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);

            VisualStudioInstance.SendKeys.Send(' ');
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("Microsoft", "System");

            VisualStudioInstance.SendKeys.Send('<');
            Assert.AreEqual(false, VisualStudioInstance.Editor.IsCompletionActive());
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void DismissAndRetriggerIntelliSenseOnEquals()
        {
            SetUpEditor(@"
Module Module1
    Function M(val As Integer) As Integer
        $$
    End Function
End Module");

            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);

            VisualStudioInstance.SendKeys.Send('M');
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("M");

            VisualStudioInstance.SendKeys.Send("=v");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("val");

            VisualStudioInstance.SendKeys.Send(' ');
            VisualStudioInstance.Editor.Verify.TextContains(@"
Module Module1
    Function M(val As Integer) As Integer
        M=val $$
    End Function
End Module",
assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void CtrlAltSpaceOption()
        {
            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);

            VisualStudioInstance.SendKeys.Send("Nam Foo");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Namespace Foo$$", assertCaretPosition: true);

            ClearEditor();

            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_ToggleCompletionMode);

            VisualStudioInstance.SendKeys.Send("Nam Foo");
            VisualStudioInstance.Editor.Verify.CurrentLineText("Nam Foo$$", assertCaretPosition: true);
        }

        [TestMethod, TestCategory(Traits.Features.Completion)]
        public void EnterTriggerCompletionListAndImplementInterface()
        {
            SetUpEditor(@"
Interface UFoo
    Sub FooBar()
End Interface

Public Class Bar
    Implements$$

End Class");

            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);

            VisualStudioInstance.SendKeys.Send(" UF");
            VisualStudioInstance.Editor.Verify.CompletionItemsExist("UFoo");

            VisualStudioInstance.SendKeys.Send(VirtualKey.Enter);
            Assert.AreEqual(false, VisualStudioInstance.Editor.IsCompletionActive());
            var actualText = VisualStudioInstance.Editor.GetText();
            ExtendedAssert.Contains(@"
Interface UFoo
    Sub FooBar()
End Interface

Public Class Bar
    Implements UFoo

    Public Sub FooBar() Implements UFoo.FooBar
        Throw New NotImplementedException()
    End Sub
End Class", actualText);
        }
    }
}
