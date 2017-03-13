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
            SendKeys(new KeyPress(VirtualKey.Up, ShiftState.Alt));
            Wait();
            VerifyLastReplInput("1.ToString()");
            SendKeys(VirtualKey.Enter);
            Wait();
            VerifyLastReplOutput("\"1\"");
            SendKeys(new KeyPress(VirtualKey.Up, ShiftState.Alt));
            Wait();
            VerifyLastReplInput("1.ToString()");
            SendKeys(new KeyPress(VirtualKey.Up, ShiftState.Alt));
            Wait();
            VerifyLastReplInput("1 + 2");
            SendKeys(VirtualKey.Enter);
            Wait();
            VerifyLastReplOutput("3");
            SendKeys(new KeyPress(VirtualKey.Down, ShiftState.Alt));
            Wait();
            VerifyLastReplInput("1.ToString()");
            SendKeys(VirtualKey.Enter);
            Wait();
            VerifyLastReplOutput("\"1\"");
        }

        [Fact]
        public void VerifyMaybeExecuteInput()
        {
            SetText("2 + 3");
            SendKeys(VirtualKey.Enter);
            Wait();
            VerifyLastReplOutput("5");
        }

        [Fact]
        public void VerifyNewLineAndIndent()
        {
            SetText("3 + ");
            SendKeys(VirtualKey.Enter);
            SetText("4");
            SendKeys(VirtualKey.Enter);
            Wait();
            VerifyLastReplOutput("7");
        }

        [Fact]
        public void VerifyExecuteInput()
        {
            SubmitText("1 + ");
            VerifyLastReplOutputContains("CS1733");
        }

        [Fact]
        public void VerifyForceNewLineAndIndent()
        {
            SetText("1 + 2");
            SendKeys(VirtualKey.Enter);
            Wait();
            SubmitText("+ 3");
            VerifyReplPromptConsistency("<![CDATA[1 + 2 + 3]]>", "6");
        }

        [Fact]
        public void VerifyCancelInput()
        {
            SetText("1 + 4");
            SendKeys(new KeyPress(VirtualKey.Enter, ShiftState.Shift));
            SendKeys(VirtualKey.Escape);
            Wait();
            VerifyLastReplInput(string.Empty);
        }

        [Fact]
        public void VerifyUndoAndRedo()
        {
            ClearReplText();
            SetText(" 2 + 4 ");
            SendKeys(new KeyPress(VirtualKey.Z, ShiftState.Ctrl));
            //  < VerifyReplInput >
            //    < ![CDATA[]] >
            //  </ VerifyReplInput >
            VerifyLastReplInput(string.Empty);
            SendKeys(new KeyPress(VirtualKey.Y, ShiftState.Ctrl));
            VerifyLastReplInput(" 2 + 4 ");
            SendKeys(VirtualKey.Enter);
            Wait();
            VerifyLastReplOutput("6");
        }

        [Fact]
        public void CutDeletePasteSelectAll()
        {
            SetText("Text");
            ExecuteCommand("Edit.LineStart");
            ExecuteCommand("Edit.LineEnd");
            ExecuteCommand("Edit.LineStartExtend");
            ExecuteCommand("Edit.SelectionCancel");
            ExecuteCommand("Edit.LineEndExtend");
            ExecuteCommand("Edit.SelectAll");
            ExecuteCommand("Edit.SelectAll");
            ExecuteCommand("Edit.Copy");
            ExecuteCommand("Edit.Cut");
            ExecuteCommand("Edit.Paste");
            ExecuteCommand("Edit.Delete");
            ExecuteCommand("Edit.LineUp");
            ExecuteCommand("Edit.LineDown");
            ExecuteCommand("Edit.Paste");
            ExecuteCommand("Edit.Paste");
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
            SendKeys(" (");
            SendKeys(")");
            SendKeys(VirtualKey.Left);
            SendKeys(VirtualKey.Enter);
            VerifyCaretPositionColumn(6);
        }

        private void Wait()
        {
            Wait(seconds: 5);
        }
    }
}