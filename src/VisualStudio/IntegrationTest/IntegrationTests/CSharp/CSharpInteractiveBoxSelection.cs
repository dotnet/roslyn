// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveBoxSelection : AbstractIdeInteractiveWindowTest
    {
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await VisualStudio.InteractiveWindow.SubmitTextAsync("#cls");
        }

        public override async Task DisposeAsync()
        {
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_SelectionCancel);
            await base.DisposeAsync();
        }

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/28357")]
        public async Task TopLeftBottomRightPromptToSymbolAsync()
        {
            InsertInputWithXAtLeft();

            await VisualStudio.InteractiveWindow.PlaceCaretAsync(">", 1);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("x", 0, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/28357")]
        public async Task BottomRightTopLeftPromptToSymbolAsync()
        {
            InsertInputWithXAtLeft();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("x", 0);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync(">", 1, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/28357")]
        public async Task TopRightBottomLeftPromptToSymbolAsync()
        {
            InsertInputWithXAtLeft();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync(">", 3);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("x", -2, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/28357")]
        public async Task BottomLeftTopRightPromptToSymbolAsync()
        {
            InsertInputWithXAtLeft();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("x", -2);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync(">", 3, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/28357")]
        public async Task TopLeftBottomRightSymbolToSymbolAsync()
        {
            InsertInputWithSAndEAtLeft();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("s", -1);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("e", 1, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/28357")]
        public async Task BottomRightTopLeftSymbolToSymbolAsync()
        {
            InsertInputWithSAndEAtLeft();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("e", 1);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("s", -1, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/28357")]
        public async Task TopRightBottomLeftSymbolToSymbolAsync()
        {
            InsertInputWithSAndEAtLeft();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("s", 1);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("e", -1, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }


        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/28357")]
        public async Task BottomLeftTopRightSymbolToSymbolAsync()
        {
            InsertInputWithSAndEAtLeft();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("e", -1);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("s", 1, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [IdeFact]
        public async Task TopLeftBottomRightSelection1Async()
        {
            InsertInputWithSAndEAtLeft();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("s", -3);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("e", 2, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("_");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
1234567890ABCDEF");
        }

        [IdeFact]
        public async Task TopLeftBottomRightSelection2Async()
        {
            InsertInputWithSAndEAtLeft();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("e", -2);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("s", -3, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [IdeFact]
        public async Task TopRightBottomLeftSelectionAsync()
        {
            InsertInputWithSAndEAtLeft();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("s", -2);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("e", -3, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [IdeFact]
        public async Task BottomLeftTopRightSelectionAsync()
        {
            InsertInputWithSAndEAtLeft();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("e", -3);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("s", -2, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [IdeFact]
        public async Task SelectionTouchingSubmissionBufferAsync()
        {
            InsertInputWithSAndEAtLeft();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("s", -2);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("e", -1, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("__");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__s234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__e234567890ABCDEF
1234567890ABCDEF");
        }

        [IdeFact]
        public async Task PrimaryPromptLongerThanSecondaryZeroWidthNextToPromptSelectionAsync()
        {
            InsertInputWithSAndEAtLeft();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("s", -1);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("e", -1, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("__");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__s234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__e234567890ABCDEF
1234567890ABCDEF");
        }

        [IdeFact]
        public async Task BackspaceAsync()
        {
            InsertInputWithSAndEInTheMiddle();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("s", -1);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("e", 0, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Backspace, VirtualKey.Backspace);

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1234567890ABCDEF");
        }

        [IdeFact]
        public async Task BackspaceBehavesLikeDeleteAsync()
        {
            InsertInputWithEInTheMiddle();
            await VisualStudio.InteractiveWindow.PlaceCaretAsync(">", 0);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("e", 0, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Backspace, VirtualKey.Backspace);

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"CDEF
CDEF
CDEF
CDEF
CDEF
CDEF
CDEF
1234567890ABCDEF");
        }

        [IdeFact]
        public async Task LeftToRightReversedBackspaceAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("1234567890ABCDEF");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("2", -5);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync(">", 8, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Backspace);

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"7890ABCDEF");
        }

        [IdeFact]
        public async Task LeftToRightReversedDeleteAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("1234567890ABCDEF");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("1", -1);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync(">", 5, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Delete);

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"4567890ABCDEF");
        }

        [IdeFact]
        public async Task LeftToRightReversedTypeCharacterAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("1234567890ABCDEF");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("1", -1);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync(">", 5, extendSelection: true, selectBlock: true);
            await VisualStudio.SendKeys.SendAsync("__");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"__4567890ABCDEF");
        }

        private void InsertInputWithXAtLeft()
        {
            VisualStudio.InteractiveWindow.InsertCode(@"1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
x234567890ABCDEF
1234567890ABCDEF");
        }

        private void InsertInputWithSAndEAtLeft()
        {
            VisualStudio.InteractiveWindow.InsertCode(@"1234567890ABCDEF
1234567890ABCDEF
s234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
e234567890ABCDEF
1234567890ABCDEF");
        }

        private void InsertInputWithSAndEInTheMiddle()
        {
            VisualStudio.InteractiveWindow.InsertCode(@"12s4567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890AeCDEF
1234567890ABCDEF");
        }

        private void InsertInputWithEInTheMiddle()
        {
            VisualStudio.InteractiveWindow.InsertCode(@"1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890AeCDEF
1234567890ABCDEF");
        }

        private void VerifyOriginalCodeWithSAndEAtLeft()
        {
            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
s234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
e234567890ABCDEF
1234567890ABCDEF");
        }
    }
}
