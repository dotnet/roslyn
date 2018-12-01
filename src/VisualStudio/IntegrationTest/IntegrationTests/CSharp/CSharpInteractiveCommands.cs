// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpInteractiveCommands : AbstractInteractiveWindowTest
    {
        public CSharpInteractiveCommands( )
            : base()
        {
        }

        [TestMethod]
        public void VerifyPreviousAndNextHistory()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("1 + 2");
            VisualStudioInstance.InteractiveWindow.SubmitText("1.ToString()");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("\"1\"");
            VisualStudioInstance.SendKeys.Send(Alt(VirtualKey.Up));
            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput("1.ToString()");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Enter);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("\"1\"");
            VisualStudioInstance.SendKeys.Send(Alt(VirtualKey.Up));
            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput("1.ToString()");
            VisualStudioInstance.SendKeys.Send(Alt(VirtualKey.Up));
            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput("1 + 2");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Enter);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("3");
            VisualStudioInstance.SendKeys.Send(Alt(VirtualKey.Down));
            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput("1.ToString()");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Enter);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("\"1\"");
        }

        [TestMethod]
        public void VerifyMaybeExecuteInput()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("2 + 3");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Enter);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("5");
        }

        [TestMethod]
        public void VerifyNewLineAndIndent()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("3 + ");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Enter);
            VisualStudioInstance.InteractiveWindow.InsertCode("4");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Enter);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("7");
        }

        [TestMethod]
        public void VerifyExecuteInput()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("1 + ");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("CS1733");
        }

        [TestMethod]
        public void VerifyForceNewLineAndIndent()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("1 + 2");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Enter);
            VisualStudioInstance.InteractiveWindow.SubmitText("+ 3");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutputContains("3");
            VisualStudioInstance.InteractiveWindow.Verify.ReplPromptConsistency("<![CDATA[1 + 2 + 3]]>", "6");
        }

        [TestMethod]
        public void VerifyCancelInput()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("1 + 4");
            VisualStudioInstance.SendKeys.Send(Shift(VirtualKey.Enter));
            VisualStudioInstance.SendKeys.Send(VirtualKey.Escape);
            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(string.Empty);
        }

        [TestMethod]
        public void VerifyUndoAndRedo()
        {
            VisualStudioInstance.InteractiveWindow.ClearReplText();
            VisualStudioInstance.InteractiveWindow.InsertCode(" 2 + 4 ");
            VisualStudioInstance.SendKeys.Send(Ctrl(VirtualKey.Z));
            VisualStudioInstance.InteractiveWindow.Verify.ReplPromptConsistency("< ![CDATA[]] >", string.Empty);
            VisualStudioInstance.SendKeys.Send(Ctrl(VirtualKey.Y));
            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(" 2 + 4 ");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Enter);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("6");
        }

        [TestMethod]
        public void CutDeletePasteSelectAll()
        {
            ClearInteractiveWindow();
            VisualStudioInstance.InteractiveWindow.InsertCode("Text");
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_LineStart);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_LineEnd);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_LineStartExtend);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_SelectionCancel);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_LineEndExtend);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_SelectAll);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_SelectAll);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_Copy);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_Cut);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_Paste);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplInputContains("Text");
            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput("Text");
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_Delete);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_LineUp);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_LineDown);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_Paste);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplInputContains("TextText");
            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput("TextText");
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_Paste);
            VisualStudioInstance.InteractiveWindow.WaitForLastReplInputContains("TextTextText");
            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput("TextTextText");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Escape);
        }

        //<!-- Regression test for bug 13731.
        //     Unfortunately we don't have good unit-test infrastructure to test InteractiveWindow.cs.
        //     For now, since we don't have coverage of InteractiveWindow.IndentCurrentLine at all,
        //     I'd rather have a quick integration test scenario rather than no coverage at all.
        //     At some point when we start investing in Interactive work again, we'll go through some
        //     of these tests and convert them to unit-tests.
        //     -->
        //<!-- TODO(https://github.com/dotnet/roslyn/issues/4235)
        [TestMethod]
        public void VerifyReturnIndentCurrentLine()
        {
            VisualStudioInstance.InteractiveWindow.ClearScreen();
            VisualStudioInstance.SendKeys.Send(" (");
            VisualStudioInstance.SendKeys.Send(")");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Left);
            VisualStudioInstance.SendKeys.Send(VirtualKey.Enter);
            VisualStudioInstance.InteractiveWindow.Verify.CaretPosition(12);
        }
    }
}
