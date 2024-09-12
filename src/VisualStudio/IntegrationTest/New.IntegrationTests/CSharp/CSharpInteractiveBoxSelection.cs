// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpInteractiveBoxSelection : AbstractInteractiveWindowTest
    {
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await TestServices.InteractiveWindow.SubmitTextAsync("#cls", HangMitigatingCancellationToken);
        }

        public override async Task DisposeAsync()
        {
            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.SelectionCancel, HangMitigatingCancellationToken);
            await base.DisposeAsync();
        }

        [IdeFact]
        public async Task TopLeftBottomRightPromptToSymbol()
        {
            await InsertInputWithXAtLeftAsync(HangMitigatingCancellationToken);

            await TestServices.InteractiveWindow.PlaceCaretAsync(">", 1, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("x", 0, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(["__", VirtualKeyCode.ESCAPE, "|"], HangMitigatingCancellationToken);

            Assert.Equal(@"__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task BottomRightTopLeftPromptToSymbol()
        {
            await InsertInputWithXAtLeftAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("x", 0, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync(">", 1, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(["__", VirtualKeyCode.ESCAPE, "|"], HangMitigatingCancellationToken);

            Assert.Equal(@"__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task TopRightBottomLeftPromptToSymbol()
        {
            await InsertInputWithXAtLeftAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync(">", 3, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("x", -2, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(["__", VirtualKeyCode.ESCAPE, "|"], HangMitigatingCancellationToken);

            Assert.Equal(@"__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task BottomLeftTopRightPromptToSymbol()
        {
            await InsertInputWithXAtLeftAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("x", -2, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync(">", 3, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(["__", VirtualKeyCode.ESCAPE, "|"], HangMitigatingCancellationToken);

            Assert.Equal(@"__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task TopLeftBottomRightSymbolToSymbol()
        {
            await InsertInputWithSAndEAtLeftAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("s", -1, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("e", 1, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(["__", VirtualKeyCode.ESCAPE, "|"], HangMitigatingCancellationToken);

            Assert.Equal(@"1234567890ABCDEF
1234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task BottomRightTopLeftSymbolToSymbol()
        {
            await InsertInputWithSAndEAtLeftAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("e", 1, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("s", -1, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(["__", VirtualKeyCode.ESCAPE, "|"], HangMitigatingCancellationToken);

            Assert.Equal(@"1234567890ABCDEF
1234567890ABCDEF
__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task TopRightBottomLeftSymbolToSymbol()
        {
            await InsertInputWithSAndEAtLeftAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("s", 1, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("e", -1, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(["__", VirtualKeyCode.ESCAPE, "|"], HangMitigatingCancellationToken);

            Assert.Equal(@"1234567890ABCDEF
1234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task BottomLeftTopRightSymbolToSymbol()
        {
            await InsertInputWithSAndEAtLeftAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("e", -1, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("s", 1, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(["__", VirtualKeyCode.ESCAPE, "|"], HangMitigatingCancellationToken);

            Assert.Equal(@"1234567890ABCDEF
1234567890ABCDEF
__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task TopLeftBottomRightSelection1()
        {
            await InsertInputWithSAndEAtLeftAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("s", -3, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("e", 2, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync("_", HangMitigatingCancellationToken);

            Assert.Equal(@"1234567890ABCDEF
1234567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task TopLeftBottomRightSelection2()
        {
            await InsertInputWithSAndEAtLeftAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("e", -2, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("s", -3, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync("_", HangMitigatingCancellationToken);

            await VerifyOriginalCodeWithSAndEAtLeftAsync(HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task TopRightBottomLeftSelection()
        {
            await InsertInputWithSAndEAtLeftAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("s", -2, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("e", -3, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync("_", HangMitigatingCancellationToken);

            await VerifyOriginalCodeWithSAndEAtLeftAsync(HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task BottomLeftTopRightSelection()
        {
            await InsertInputWithSAndEAtLeftAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("e", -3, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("s", -2, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync("_", HangMitigatingCancellationToken);

            await VerifyOriginalCodeWithSAndEAtLeftAsync(HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task SelectionTouchingSubmissionBuffer()
        {
            await InsertInputWithSAndEAtLeftAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("s", -2, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("e", -1, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync("__", HangMitigatingCancellationToken);

            Assert.Equal(@"1234567890ABCDEF
1234567890ABCDEF
__s234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__e234567890ABCDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task PrimaryPromptLongerThanSecondaryZeroWidthNextToPromptSelection()
        {
            await InsertInputWithSAndEAtLeftAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("s", -1, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("e", -1, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync("__", HangMitigatingCancellationToken);

            Assert.Equal(@"1234567890ABCDEF
1234567890ABCDEF
__s234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__e234567890ABCDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task Backspace()
        {
            await InsertInputWithSAndEInTheMiddleAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("s", -1, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("e", 0, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.BACK, VirtualKeyCode.BACK], HangMitigatingCancellationToken);

            Assert.Equal(@"1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task BackspaceBehavesLikeDelete()
        {
            await InsertInputWithEInTheMiddleAsync(HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync(">", 0, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("e", 0, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.BACK, VirtualKeyCode.BACK], HangMitigatingCancellationToken);

            Assert.Equal(@"CDEF
CDEF
CDEF
CDEF
CDEF
CDEF
CDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task LeftToRightReversedBackspace()
        {
            await TestServices.InteractiveWindow.InsertCodeAsync("1234567890ABCDEF", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("2", -5, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync(">", 8, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.BACK, HangMitigatingCancellationToken);

            Assert.Equal(@"7890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task LeftToRightReversedDelete()
        {
            await TestServices.InteractiveWindow.InsertCodeAsync("1234567890ABCDEF", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("1", -1, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync(">", 5, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.DELETE, HangMitigatingCancellationToken);

            Assert.Equal(@"4567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        [IdeFact]
        public async Task LeftToRightReversedTypeCharacter()
        {
            await TestServices.InteractiveWindow.InsertCodeAsync("1234567890ABCDEF", HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync("1", -1, HangMitigatingCancellationToken);
            await TestServices.InteractiveWindow.PlaceCaretAsync(">", 5, occurrence: 0, extendSelection: true, selectBlock: true, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync("__", HangMitigatingCancellationToken);

            Assert.Equal(@"__4567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(HangMitigatingCancellationToken));
        }

        private async Task InsertInputWithXAtLeftAsync(CancellationToken cancellationToken)
        {
            await TestServices.InteractiveWindow.InsertCodeAsync(@"1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
x234567890ABCDEF
1234567890ABCDEF", cancellationToken);
        }

        private async Task InsertInputWithSAndEAtLeftAsync(CancellationToken cancellationToken)
        {
            await TestServices.InteractiveWindow.InsertCodeAsync(@"1234567890ABCDEF
1234567890ABCDEF
s234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
e234567890ABCDEF
1234567890ABCDEF", cancellationToken);
        }

        private async Task InsertInputWithSAndEInTheMiddleAsync(CancellationToken cancellationToken)
        {
            await TestServices.InteractiveWindow.InsertCodeAsync(@"12s4567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890AeCDEF
1234567890ABCDEF", cancellationToken);
        }

        private async Task InsertInputWithEInTheMiddleAsync(CancellationToken cancellationToken)
        {
            await TestServices.InteractiveWindow.InsertCodeAsync(@"1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890AeCDEF
1234567890ABCDEF", cancellationToken);
        }

        private async Task VerifyOriginalCodeWithSAndEAtLeftAsync(CancellationToken cancellationToken)
        {
            Assert.Equal(@"1234567890ABCDEF
1234567890ABCDEF
s234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
1234567890ABCDEF
e234567890ABCDEF
1234567890ABCDEF", await TestServices.InteractiveWindow.GetLastReplInputAsync(cancellationToken));
        }
    }
}
