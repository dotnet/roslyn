// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveBoxSelection : AbstractInteractiveWindowTest
    {
        public CSharpInteractiveBoxSelection(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            VisualStudio.InteractiveWindow.SubmitText("#cls");
        }

        public override Task DisposeAsync()
        {
            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_SelectionCancel);
            return base.DisposeAsync();
        }

        [WpfFact]
        public void TopLeftBottomRightPromptToSymbol()
        {
            InsertInputWithXAtLeft();

            VisualStudio.InteractiveWindow.PlaceCaret(">", 1);
            VisualStudio.InteractiveWindow.PlaceCaret("x", 0, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }

        [WpfFact]
        public void BottomRightTopLeftPromptToSymbol()
        {
            InsertInputWithXAtLeft();
            VisualStudio.InteractiveWindow.PlaceCaret("x", 0);
            VisualStudio.InteractiveWindow.PlaceCaret(">", 1, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [WpfFact]
        public void TopRightBottomLeftPromptToSymbol()
        {
            InsertInputWithXAtLeft();
            VisualStudio.InteractiveWindow.PlaceCaret(">", 3);
            VisualStudio.InteractiveWindow.PlaceCaret("x", -2, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }

        [WpfFact]
        public void BottomLeftTopRightPromptToSymbol()
        {
            InsertInputWithXAtLeft();
            VisualStudio.InteractiveWindow.PlaceCaret("x", -2);
            VisualStudio.InteractiveWindow.PlaceCaret(">", 3, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [WpfFact]
        public void TopLeftBottomRightSymbolToSymbol()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudio.InteractiveWindow.PlaceCaret("s", -1);
            VisualStudio.InteractiveWindow.PlaceCaret("e", 1, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }

        [WpfFact]
        public void BottomRightTopLeftSymbolToSymbol()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudio.InteractiveWindow.PlaceCaret("e", 1);
            VisualStudio.InteractiveWindow.PlaceCaret("s", -1, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [WpfFact]
        public void TopRightBottomLeftSymbolToSymbol()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudio.InteractiveWindow.PlaceCaret("s", 1);
            VisualStudio.InteractiveWindow.PlaceCaret("e", -1, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }

        [WpfFact]
        public void BottomLeftTopRightSymbolToSymbol()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudio.InteractiveWindow.PlaceCaret("e", -1);
            VisualStudio.InteractiveWindow.PlaceCaret("s", 1, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [WpfFact]
        public void TopLeftBottomRightSelection1()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudio.InteractiveWindow.PlaceCaret("s", -3);
            VisualStudio.InteractiveWindow.PlaceCaret("e", 2, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("_");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
1234567890ABCDEF");
        }

        [WpfFact]
        public void TopLeftBottomRightSelection2()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudio.InteractiveWindow.PlaceCaret("e", -2);
            VisualStudio.InteractiveWindow.PlaceCaret("s", -3, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [WpfFact]
        public void TopRightBottomLeftSelection()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudio.InteractiveWindow.PlaceCaret("s", -2);
            VisualStudio.InteractiveWindow.PlaceCaret("e", -3, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [WpfFact]
        public void BottomLeftTopRightSelection()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudio.InteractiveWindow.PlaceCaret("e", -3);
            VisualStudio.InteractiveWindow.PlaceCaret("s", -2, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [WpfFact]
        public void SelectionTouchingSubmissionBuffer()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudio.InteractiveWindow.PlaceCaret("s", -2);
            VisualStudio.InteractiveWindow.PlaceCaret("e", -1, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("__");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__s234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__e234567890ABCDEF
1234567890ABCDEF");
        }

        [WpfFact]
        public void PrimaryPromptLongerThanSecondaryZeroWidthNextToPromptSelection()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudio.InteractiveWindow.PlaceCaret("s", -1);
            VisualStudio.InteractiveWindow.PlaceCaret("e", -1, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("__");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__s234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__e234567890ABCDEF
1234567890ABCDEF");
        }

        [WpfFact]
        public void Backspace()
        {
            InsertInputWithSAndEInTheMiddle();
            VisualStudio.InteractiveWindow.PlaceCaret("s", -1);
            VisualStudio.InteractiveWindow.PlaceCaret("e", 0, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send(VirtualKey.Backspace, VirtualKey.Backspace);

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1234567890ABCDEF");
        }

        [WpfFact]
        public void BackspaceBehavesLikeDelete()
        {
            InsertInputWithEInTheMiddle();
            VisualStudio.InteractiveWindow.PlaceCaret(">", 0);
            VisualStudio.InteractiveWindow.PlaceCaret("e", 0, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send(VirtualKey.Backspace, VirtualKey.Backspace);

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"CDEF
CDEF
CDEF
CDEF
CDEF
CDEF
CDEF
1234567890ABCDEF");
        }

        [WpfFact]
        public void LeftToRightReversedBackspace()
        {
            VisualStudio.InteractiveWindow.InsertCode("1234567890ABCDEF");
            VisualStudio.InteractiveWindow.PlaceCaret("2", -5);
            VisualStudio.InteractiveWindow.PlaceCaret(">", 8, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send(VirtualKey.Backspace);

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"7890ABCDEF");
        }

        [WpfFact]
        public void LeftToRightReversedDelete()
        {
            VisualStudio.InteractiveWindow.InsertCode("1234567890ABCDEF");
            VisualStudio.InteractiveWindow.PlaceCaret("1", -1);
            VisualStudio.InteractiveWindow.PlaceCaret(">", 5, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send(VirtualKey.Delete);

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"4567890ABCDEF");
        }

        [WpfFact]
        public void LeftToRightReversedTypeCharacter()
        {
            VisualStudio.InteractiveWindow.InsertCode("1234567890ABCDEF");
            VisualStudio.InteractiveWindow.PlaceCaret("1", -1);
            VisualStudio.InteractiveWindow.PlaceCaret(">", 5, extendSelection: true, selectBlock: true);
            VisualStudio.SendKeys.Send("__");

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
