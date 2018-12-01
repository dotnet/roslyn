// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpInteractiveBoxSelection : AbstractInteractiveWindowTest
    {
        public CSharpInteractiveBoxSelection() : base() { }

        [TestInitialize]
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            VisualStudioInstance.InteractiveWindow.SubmitText("#cls");
        }

        public override Task DisposeAsync()
        {
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_SelectionCancel);
            return base.DisposeAsync();
        }

        [TestMethod]
        public void TopLeftBottomRightPromptToSymbol()
        {
            InsertInputWithXAtLeft();

            VisualStudioInstance.InteractiveWindow.PlaceCaret(">", 1);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("x", 0, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }

        [TestMethod]
        public void BottomRightTopLeftPromptToSymbol()
        {
            InsertInputWithXAtLeft();
            VisualStudioInstance.InteractiveWindow.PlaceCaret("x", 0);
            VisualStudioInstance.InteractiveWindow.PlaceCaret(">", 1, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [TestMethod]
        public void TopRightBottomLeftPromptToSymbol()
        {
            InsertInputWithXAtLeft();
            VisualStudioInstance.InteractiveWindow.PlaceCaret(">", 3);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("x", -2, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }

        [TestMethod]
        public void BottomLeftTopRightPromptToSymbol()
        {
            InsertInputWithXAtLeft();
            VisualStudioInstance.InteractiveWindow.PlaceCaret("x", -2);
            VisualStudioInstance.InteractiveWindow.PlaceCaret(">", 3, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [TestMethod]
        public void TopLeftBottomRightSymbolToSymbol()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudioInstance.InteractiveWindow.PlaceCaret("s", -1);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("e", 1, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }

        [TestMethod]
        public void BottomRightTopLeftSymbolToSymbol()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudioInstance.InteractiveWindow.PlaceCaret("e", 1);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("s", -1, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [TestMethod]
        public void TopRightBottomLeftSymbolToSymbol()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudioInstance.InteractiveWindow.PlaceCaret("s", 1);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("e", -1, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }


        [TestMethod]
        public void BottomLeftTopRightSymbolToSymbol()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudioInstance.InteractiveWindow.PlaceCaret("e", -1);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("s", 1, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("__", VirtualKey.Escape, "|");

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [TestMethod]
        public void TopLeftBottomRightSelection1()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudioInstance.InteractiveWindow.PlaceCaret("s", -3);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("e", 2, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("_");

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
1234567890ABCDEF");
        }

        [TestMethod]
        public void TopLeftBottomRightSelection2()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudioInstance.InteractiveWindow.PlaceCaret("e", -2);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("s", -3, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [TestMethod]
        public void TopRightBottomLeftSelection()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudioInstance.InteractiveWindow.PlaceCaret("s", -2);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("e", -3, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [TestMethod]
        public void BottomLeftTopRightSelection()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudioInstance.InteractiveWindow.PlaceCaret("e", -3);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("s", -2, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [TestMethod]
        public void SelectionTouchingSubmissionBuffer()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudioInstance.InteractiveWindow.PlaceCaret("s", -2);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("e", -1, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("__");

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__s234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__e234567890ABCDEF
1234567890ABCDEF");
        }

        [TestMethod]
        public void PrimaryPromptLongerThanSecondaryZeroWidthNextToPromptSelection()
        {
            InsertInputWithSAndEAtLeft();
            VisualStudioInstance.InteractiveWindow.PlaceCaret("s", -1);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("e", -1, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("__");

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__s234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__e234567890ABCDEF
1234567890ABCDEF");
        }

        [TestMethod]
        public void Backspace()
        {
            InsertInputWithSAndEInTheMiddle();
            VisualStudioInstance.InteractiveWindow.PlaceCaret("s", -1);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("e", 0, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send(VirtualKey.Backspace, VirtualKey.Backspace);

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1234567890ABCDEF");
        }

        [TestMethod]
        public void BackspaceBehavesLikeDelete()
        {
            InsertInputWithEInTheMiddle();
            VisualStudioInstance.InteractiveWindow.PlaceCaret(">", 0);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("e", 0, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send(VirtualKey.Backspace, VirtualKey.Backspace);

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"CDEF
CDEF
CDEF
CDEF
CDEF
CDEF
CDEF
1234567890ABCDEF");
        }

        [TestMethod]
        public void LeftToRightReversedBackspace()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("1234567890ABCDEF");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("2", -5);
            VisualStudioInstance.InteractiveWindow.PlaceCaret(">", 8, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send(VirtualKey.Backspace);

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"7890ABCDEF");
        }

        [TestMethod]
        public void LeftToRightReversedDelete()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("1234567890ABCDEF");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("1", -1);
            VisualStudioInstance.InteractiveWindow.PlaceCaret(">", 5, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send(VirtualKey.Delete);

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"4567890ABCDEF");
        }

        [TestMethod]
        public void LeftToRightReversedTypeCharacter()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("1234567890ABCDEF");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("1", -1);
            VisualStudioInstance.InteractiveWindow.PlaceCaret(">", 5, extendSelection: true, selectBlock: true);
            VisualStudioInstance.SendKeys.Send("__");

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"__4567890ABCDEF");
        }

        private void InsertInputWithXAtLeft()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode(@"1234567890ABCDEF
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
            VisualStudioInstance.InteractiveWindow.InsertCode(@"1234567890ABCDEF
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
            VisualStudioInstance.InteractiveWindow.InsertCode(@"12s4567890ABCDEF
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
            VisualStudioInstance.InteractiveWindow.InsertCode(@"1234567890ABCDEF
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
            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"1234567890ABCDEF
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
