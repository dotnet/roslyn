// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveCommands : AbstractIdeInteractiveWindowTest
    {
        [IdeFact]
        public async Task VerifyPreviousAndNextHistoryAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("1 + 2");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("1.ToString()");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("\"1\"");
            await VisualStudio.SendKeys.SendAsync(Alt(VirtualKey.Up));
            VisualStudio.InteractiveWindow.Verify.LastReplInput("1.ToString()");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Enter);
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("\"1\"");
            await VisualStudio.SendKeys.SendAsync(Alt(VirtualKey.Up));
            VisualStudio.InteractiveWindow.Verify.LastReplInput("1.ToString()");
            await VisualStudio.SendKeys.SendAsync(Alt(VirtualKey.Up));
            VisualStudio.InteractiveWindow.Verify.LastReplInput("1 + 2");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Enter);
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("3");
            await VisualStudio.SendKeys.SendAsync(Alt(VirtualKey.Down));
            VisualStudio.InteractiveWindow.Verify.LastReplInput("1.ToString()");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Enter);
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("\"1\"");
        }

        [IdeFact]
        public async Task VerifyMaybeExecuteInputAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("2 + 3");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Enter);
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("5");
        }

        [IdeFact]
        public async Task VerifyNewLineAndIndentAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("3 + ");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Enter);
            VisualStudio.InteractiveWindow.InsertCode("4");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Enter);
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("7");
        }

        [IdeFact]
        public async Task VerifyExecuteInputAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("1 + ");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("CS1733");
        }

        [IdeFact]
        public async Task VerifyForceNewLineAndIndentAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("1 + 2");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Enter);
            await VisualStudio.InteractiveWindow.SubmitTextAsync("+ 3");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputContainsAsync("3");
            VisualStudio.InteractiveWindow.Verify.ReplPromptConsistency("<![CDATA[1 + 2 + 3]]>", "6");
        }

        [IdeFact]
        public async Task VerifyCancelInputAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("1 + 4");
            await VisualStudio.SendKeys.SendAsync(Shift(VirtualKey.Enter));
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Escape);
            VisualStudio.InteractiveWindow.Verify.LastReplInput(string.Empty);
        }

        [IdeFact]
        public async Task VerifyUndoAndRedoAsync()
        {
            await VisualStudio.InteractiveWindow.ClearReplTextAsync();
            VisualStudio.InteractiveWindow.InsertCode(" 2 + 4 ");
            await VisualStudio.SendKeys.SendAsync(Ctrl(VirtualKey.Z));
            VisualStudio.InteractiveWindow.Verify.ReplPromptConsistency("< ![CDATA[]] >", string.Empty);
            await VisualStudio.SendKeys.SendAsync(Ctrl(VirtualKey.Y));
            VisualStudio.InteractiveWindow.Verify.LastReplInput(" 2 + 4 ");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Enter);
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("6");
        }

        [IdeFact]
        public async Task CutDeletePasteSelectAllAsync()
        {
            await ClearInteractiveWindowAsync();
            VisualStudio.InteractiveWindow.InsertCode("Text");
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_LineStart);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_LineEnd);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_LineStartExtend);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_SelectionCancel);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_LineEndExtend);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_SelectAll);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_SelectAll);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_Copy);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_Cut);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_Paste);
            await VisualStudio.InteractiveWindow.WaitForLastReplInputContainsAsync("Text");
            VisualStudio.InteractiveWindow.Verify.LastReplInput("Text");
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_Delete);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_LineUp);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_LineDown);
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_Paste);
            await VisualStudio.InteractiveWindow.WaitForLastReplInputContainsAsync("TextText");
            VisualStudio.InteractiveWindow.Verify.LastReplInput("TextText");
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_Paste);
            await VisualStudio.InteractiveWindow.WaitForLastReplInputContainsAsync("TextTextText");
            VisualStudio.InteractiveWindow.Verify.LastReplInput("TextTextText");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Escape);
        }

        //<!-- Regression test for bug 13731.
        //     Unfortunately we don't have good unit-test infrastructure to test InteractiveWindow.cs.
        //     For now, since we don't have coverage of InteractiveWindow.IndentCurrentLine at all,
        //     I'd rather have a quick integration test scenario rather than no coverage at all.
        //     At some point when we start investing in Interactive work again, we'll go through some
        //     of these tests and convert them to unit-tests.
        //     -->
        //<!-- TODO(https://github.com/dotnet/roslyn/issues/4235)
        [IdeFact]
        public async Task VerifyReturnIndentCurrentLineAsync()
        {
            await VisualStudio.InteractiveWindow.ClearScreenAsync();
            await VisualStudio.SendKeys.SendAsync(" (");
            await VisualStudio.SendKeys.SendAsync(")");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Left);
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Enter);
            await VisualStudio.InteractiveWindow.Verify.CaretPositionAsync(12);
        }
    }
}
