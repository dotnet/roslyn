﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            VisualStudio.InteractiveWindow.SubmitText("1 + 2");
            VisualStudio.InteractiveWindow.SubmitText("1.ToString()");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("\"1\"");
            VisualStudio.SendKeys.Send(Alt(VirtualKey.Up));
            VisualStudio.InteractiveWindow.Verify.LastReplInput("1.ToString()");
            VisualStudio.SendKeys.Send(VirtualKey.Enter);
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("\"1\"");
            VisualStudio.SendKeys.Send(Alt(VirtualKey.Up));
            VisualStudio.InteractiveWindow.Verify.LastReplInput("1.ToString()");
            VisualStudio.SendKeys.Send(Alt(VirtualKey.Up));
            VisualStudio.InteractiveWindow.Verify.LastReplInput("1 + 2");
            VisualStudio.SendKeys.Send(VirtualKey.Enter);
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("3");
            VisualStudio.SendKeys.Send(Alt(VirtualKey.Down));
            VisualStudio.InteractiveWindow.Verify.LastReplInput("1.ToString()");
            VisualStudio.SendKeys.Send(VirtualKey.Enter);
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("\"1\"");
        }

        [Fact]
        public void VerifyMaybeExecuteInput()
        {
            VisualStudio.InteractiveWindow.InsertCode("2 + 3");
            VisualStudio.SendKeys.Send(VirtualKey.Enter);
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("5");
        }

        [Fact]
        public void VerifyNewLineAndIndent()
        {
            VisualStudio.InteractiveWindow.InsertCode("3 + ");
            VisualStudio.SendKeys.Send(VirtualKey.Enter);
            VisualStudio.InteractiveWindow.InsertCode("4");
            VisualStudio.SendKeys.Send(VirtualKey.Enter);
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("7");
        }

        [Fact]
        public void VerifyExecuteInput()
        {
            VisualStudio.InteractiveWindow.SubmitText("1 + ");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("CS1733");
        }

        [Fact]
        public void VerifyForceNewLineAndIndent()
        {
            VisualStudio.InteractiveWindow.InsertCode("1 + 2");
            VisualStudio.SendKeys.Send(VirtualKey.Enter);
            VisualStudio.InteractiveWindow.SubmitText("+ 3");
            VisualStudio.InteractiveWindow.WaitForLastReplOutputContains("3");
            VisualStudio.InteractiveWindow.Verify.ReplPromptConsistency("<![CDATA[1 + 2 + 3]]>", "6");
        }

        [Fact]
        public void VerifyCancelInput()
        {
            VisualStudio.InteractiveWindow.InsertCode("1 + 4");
            VisualStudio.SendKeys.Send(Shift(VirtualKey.Enter));
            VisualStudio.SendKeys.Send(VirtualKey.Escape);
            VisualStudio.InteractiveWindow.Verify.LastReplInput(string.Empty);
        }

        [Fact]
        public void VerifyUndoAndRedo()
        {
            VisualStudio.InteractiveWindow.ClearReplText();
            VisualStudio.InteractiveWindow.InsertCode(" 2 + 4 ");
            VisualStudio.SendKeys.Send(Ctrl(VirtualKey.Z));
            VisualStudio.InteractiveWindow.Verify.ReplPromptConsistency("< ![CDATA[]] >", string.Empty);
            VisualStudio.SendKeys.Send(Ctrl(VirtualKey.Y));
            VisualStudio.InteractiveWindow.Verify.LastReplInput(" 2 + 4 ");
            VisualStudio.SendKeys.Send(VirtualKey.Enter);
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("6");
        }

        [Fact]
        public void CutDeletePasteSelectAll()
        {
            ClearInteractiveWindow();
            VisualStudio.InteractiveWindow.InsertCode("Text");
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_LineStart);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_LineEnd);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_LineStartExtend);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_SelectionCancel);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_LineEndExtend);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_SelectAll);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_SelectAll);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_Copy);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_Cut);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_Paste);
            VisualStudio.InteractiveWindow.WaitForLastReplInputContains("Text");
            VisualStudio.InteractiveWindow.Verify.LastReplInput("Text");
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_Delete);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_LineUp);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_LineDown);
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_Paste);
            VisualStudio.InteractiveWindow.WaitForLastReplInputContains("TextText");
            VisualStudio.InteractiveWindow.Verify.LastReplInput("TextText");
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_Paste);
            VisualStudio.InteractiveWindow.WaitForLastReplInputContains("TextTextText");
            VisualStudio.InteractiveWindow.Verify.LastReplInput("TextTextText");
            VisualStudio.SendKeys.Send(VirtualKey.Escape);
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
            VisualStudio.InteractiveWindow.ClearScreen();
            VisualStudio.SendKeys.Send(" (");
            VisualStudio.SendKeys.Send(")");
            VisualStudio.SendKeys.Send(VirtualKey.Left);
            VisualStudio.SendKeys.Send(VirtualKey.Enter);
            VisualStudio.InteractiveWindow.Verify.CaretPosition(12);
        }
    }
}