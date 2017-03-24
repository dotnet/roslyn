// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveCommands : AbstractInteractiveWindowTest
    {
        public CSharpInteractiveCommands(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        [Fact]
        public void VerifyPreviousAndNextHistory()
        {
            SubmitText("1 + 2");
            SubmitText("1.ToString()");
            WaitForLastReplOutput("\"1\"");
            SendKeys(Alt(VirtualKey.Up));
            VerifyLastReplInput("1.ToString()");
            SendKeys(VirtualKey.Enter);
            WaitForReplOutput("\"1\"");
            SendKeys(Alt(VirtualKey.Up));
            VerifyLastReplInput("1.ToString()");
            SendKeys(Alt(VirtualKey.Up));
            VerifyLastReplInput("1 + 2");
            SendKeys(VirtualKey.Enter);
            WaitForReplOutput("3");
            SendKeys(Alt(VirtualKey.Down));
            VerifyLastReplInput("1.ToString()");
            SendKeys(VirtualKey.Enter);
            WaitForReplOutput("\"1\"");
        }

        [Fact]
        public void VerifyMaybeExecuteInput()
        {
            InsertCode("2 + 3");
            SendKeys(VirtualKey.Enter);
            WaitForReplOutput("5");
        }

        [Fact]
        public void VerifyNewLineAndIndent()
        {
            InsertCode("3 + ");
            SendKeys(VirtualKey.Enter);
            InsertCode("4");
            SendKeys(VirtualKey.Enter);
            WaitForReplOutput("7");
        }

        [Fact]
        public void VerifyExecuteInput()
        {
            SubmitText("1 + ");
            WaitForLastReplOutputContains("CS1733");
        }

        [Fact]
        public void VerifyForceNewLineAndIndent()
        {
            InsertCode("1 + 2");
            SendKeys(VirtualKey.Enter);
            SubmitText("+ 3");
            WaitForReplOutputContains("3");
            VerifyReplPromptConsistency("<![CDATA[1 + 2 + 3]]>", "6");
        }

        [Fact]
        public void VerifyCancelInput()
        {
            InsertCode("1 + 4");
            SendKeys(Shift(VirtualKey.Enter));
            SendKeys(VirtualKey.Escape);
            VerifyLastReplInput(string.Empty);
        }

        [Fact]
        public void VerifyUndoAndRedo()
        {
            ClearReplText();
            InsertCode(" 2 + 4 ");
            SendKeys(Ctrl(VirtualKey.Z));
            VerifyReplPromptConsistency("< ![CDATA[]] >", string.Empty);
            SendKeys(Ctrl(VirtualKey.Y));
            VerifyLastReplInput(" 2 + 4 ");
            SendKeys(VirtualKey.Enter);
            WaitForReplOutput("6");
        }

        [Fact]
        public void CutDeletePasteSelectAll()
        {
            SendKeys("Text");
            ExecuteCommand(WellKnownCommandNames.Edit_LineStart);
            ExecuteCommand(WellKnownCommandNames.Edit_LineEnd);
            ExecuteCommand(WellKnownCommandNames.Edit_LineStartExtend);
            ExecuteCommand(WellKnownCommandNames.Edit_SelectionCancel);
            ExecuteCommand(WellKnownCommandNames.Edit_LineEndExtend);
            ExecuteCommand(WellKnownCommandNames.Edit_SelectAll);
            ExecuteCommand(WellKnownCommandNames.Edit_SelectAll);
            ExecuteCommand(WellKnownCommandNames.Edit_Copy);
            ExecuteCommand(WellKnownCommandNames.Edit_Cut);
            ExecuteCommand(WellKnownCommandNames.Edit_Paste);
            ExecuteCommand(WellKnownCommandNames.Edit_Delete);
            ExecuteCommand(WellKnownCommandNames.Edit_LineUp);
            ExecuteCommand(WellKnownCommandNames.Edit_LineDown);
            ExecuteCommand(WellKnownCommandNames.Edit_Paste);
            ExecuteCommand(WellKnownCommandNames.Edit_Paste);
            SendKeys(VirtualKey.Escape);
        }

        //<!-- Regression test for bug 13731. 
        //     Unfortunately we don't have good unit-test infrastructure to test InteractiveWindow.cs.
        //     For now, since we don't have coverage of InteractiveWindow.IndentCurrentLine at all,
        //     I'd rather have a quick integration test scenario rather than no coverage at all.
        //     At some point when we start investing in Interactive work again, we'll go through some
        //     of these tests and convert them to unit-tests.
        //     -->
        //<!-- TODO(https://github.com/dotnet/roslyn/issues/4235)
        [Fact]
        public void VerifyReturnIndentCurrentLine()
        {
            InteractiveWindow.ClearScreen();
            SendKeys(" (");
            SendKeys(")");
            SendKeys(VirtualKey.Left);
            SendKeys(VirtualKey.Enter);
            VerifyCaretPosition(12);
        }
    }
}