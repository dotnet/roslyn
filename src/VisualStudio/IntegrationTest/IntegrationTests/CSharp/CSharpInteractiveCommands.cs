// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Interactive;
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
            this.SubmitText("1 + 2");
            this.SubmitText("1.ToString()");
            this.WaitForLastReplOutput("\"1\"");
            this.SendKeys(Alt(VirtualKey.Up));
            this.VerifyLastReplInput("1.ToString()");
            this.SendKeys(VirtualKey.Enter);
            this.WaitForLastReplOutput("\"1\"");
            this.SendKeys(Alt(VirtualKey.Up));
            this.VerifyLastReplInput("1.ToString()");
            this.SendKeys(Alt(VirtualKey.Up));
            this.VerifyLastReplInput("1 + 2");
            this.SendKeys(VirtualKey.Enter);
            this.WaitForLastReplOutput("3");
            this.SendKeys(Alt(VirtualKey.Down));
            this.VerifyLastReplInput("1.ToString()");
            this.SendKeys(VirtualKey.Enter);
            this.WaitForLastReplOutput("\"1\"");
        }

        [Fact]
        public void VerifyMaybeExecuteInput()
        {
            this.InsertCode("2 + 3");
            this.SendKeys(VirtualKey.Enter);
            this.WaitForLastReplOutput("5");
        }

        [Fact]
        public void VerifyNewLineAndIndent()
        {
            this.InsertCode("3 + ");
            this.SendKeys(VirtualKey.Enter);
            this.InsertCode("4");
            this.SendKeys(VirtualKey.Enter);
            this.WaitForLastReplOutput("7");
        }

        [Fact]
        public void VerifyExecuteInput()
        {
            this.SubmitText("1 + ");
            this.WaitForLastReplOutputContains("CS1733");
        }

        [Fact]
        public void VerifyForceNewLineAndIndent()
        {
            this.InsertCode("1 + 2");
            this.SendKeys(VirtualKey.Enter);
            this.SubmitText("+ 3");
            this.WaitForLastReplOutputContains("3");
            this.VerifyReplPromptConsistency("<![CDATA[1 + 2 + 3]]>", "6");
        }

        [Fact]
        public void VerifyCancelInput()
        {
            this.InsertCode("1 + 4");
            this.SendKeys(Shift(VirtualKey.Enter));
            this.SendKeys(VirtualKey.Escape);
            this.VerifyLastReplInput(string.Empty);
        }

        [Fact]
        public void VerifyUndoAndRedo()
        {
            this.ClearReplText();
            this.InsertCode(" 2 + 4 ");
            this.SendKeys(Ctrl(VirtualKey.Z));
            this.VerifyReplPromptConsistency("< ![CDATA[]] >", string.Empty);
            this.SendKeys(Ctrl(VirtualKey.Y));
            this.VerifyLastReplInput(" 2 + 4 ");
            this.SendKeys(VirtualKey.Enter);
            this.WaitForLastReplOutput("6");
        }

        [Fact]
        public void CutDeletePasteSelectAll()
        {
            this.ClearReplText();
            this.SendKeys("Text");
            this.ExecuteCommand(WellKnownCommandNames.Edit_LineStart);
            this.ExecuteCommand(WellKnownCommandNames.Edit_LineEnd);
            this.ExecuteCommand(WellKnownCommandNames.Edit_LineStartExtend);
            this.ExecuteCommand(WellKnownCommandNames.Edit_SelectionCancel);
            this.ExecuteCommand(WellKnownCommandNames.Edit_LineEndExtend);
            this.ExecuteCommand(WellKnownCommandNames.Edit_SelectAll);
            this.ExecuteCommand(WellKnownCommandNames.Edit_SelectAll);
            this.ExecuteCommand(WellKnownCommandNames.Edit_Copy);
            this.ExecuteCommand(WellKnownCommandNames.Edit_Cut);
            this.ExecuteCommand(WellKnownCommandNames.Edit_Paste);
            this.WaitForLastReplInputContains("Text");
            this.VerifyLastReplInput("Text");
            this.ExecuteCommand(WellKnownCommandNames.Edit_Delete);
            this.ExecuteCommand(WellKnownCommandNames.Edit_LineUp);
            this.ExecuteCommand(WellKnownCommandNames.Edit_LineDown);
            this.ExecuteCommand(WellKnownCommandNames.Edit_Paste);
            this.WaitForLastReplInputContains("TextText");
            this.VerifyLastReplInput("TextText");
            this.ExecuteCommand(WellKnownCommandNames.Edit_Paste);
            this.WaitForLastReplInputContains("TextTextText");
            this.VerifyLastReplInput("TextTextText");
            this.SendKeys(VirtualKey.Escape);
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
            this.SendKeys(" (");
            this.SendKeys(")");
            this.SendKeys(VirtualKey.Left);
            this.SendKeys(VirtualKey.Enter);
            this.VerifyCaretPosition(12);
        }
    }
}