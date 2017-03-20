// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BoxSelection : AbstractInteractiveWindowTest
    {
        public BoxSelection(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            SubmitText("#cls", waitForPrompt: false);
        }

        public new void Dispose()
        {
            ExecuteCommand("Edit.SelectionCancel");
            base.Dispose();
        }

        [Fact]
        public void TopLeftBottomRightPromptToSymbol()
        {
            InsertInputWithXAtLeft();

            PlaceCaret(">", 1);
            PlaceCaret("x", 0, extendSelection: true, selectBlock: true);
            SendKeys("__", VirtualKey.Escape, "|");

            VerifyLastReplInput(@"__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }

        [Fact]
        public void BottomRightTopLeftPromptToSymbol()
        {
            InsertInputWithXAtLeft();
            PlaceCaret("x", 0);
            PlaceCaret(">", 1, extendSelection: true, selectBlock: true);
            SendKeys("__", VirtualKey.Escape, "|");

            VerifyLastReplInput(@"__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [Fact]
        public void TopRightBottomLeftPromptToSymbol()
        {
            InsertInputWithXAtLeft();
            PlaceCaret(">", 3);
            PlaceCaret("x", -2, extendSelection: true, selectBlock: true);
            SendKeys("__", VirtualKey.Escape, "|");

            VerifyLastReplInput(@"__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }

        [Fact]
        public void BottomLeftTopRightPromptToSymbol()
        {
            InsertInputWithXAtLeft();
            PlaceCaret("x", -2);
            PlaceCaret(">", 3, extendSelection: true, selectBlock: true);
            SendKeys("__", VirtualKey.Escape, "|");

            VerifyLastReplInput(@"__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [Fact]
        public void TopLeftBottomRightSymbolToSymbol()
        {
            InsertInputWithSAndEAtLeft();
            PlaceCaret("s", -1);
            PlaceCaret("e", 1, extendSelection: true, selectBlock: true);
            SendKeys("__", VirtualKey.Escape, "|");

            VerifyLastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }

        [Fact]
        public void BottomRightTopLeftSymbolToSymbol()
        {
            InsertInputWithSAndEAtLeft();
            PlaceCaret("e", 1);
            PlaceCaret("s", -1, extendSelection: true, selectBlock: true);
            SendKeys("__", VirtualKey.Escape, "|");

            VerifyLastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [Fact]
        public void TopRightBottomLeftSymbolToSymbol()
        {
            InsertInputWithSAndEAtLeft();
            PlaceCaret("s", 1);
            PlaceCaret("e", -1, extendSelection: true, selectBlock: true);
            SendKeys("__", VirtualKey.Escape, "|");

            VerifyLastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__|234567890ABCDEF
1234567890ABCDEF");
        }


        [Fact]
        public void BottomLeftTopRightSymbolToSymbol()
        {
            InsertInputWithSAndEAtLeft();
            PlaceCaret("e", -1);
            PlaceCaret("s", 1, extendSelection: true, selectBlock: true);
            SendKeys("__", VirtualKey.Escape, "|");

            VerifyLastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__|234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
__234567890ABCDEF
1234567890ABCDEF");
        }

        [Fact]
        public void TopLeftBottomRightSelection1()
        {
            InsertInputWithSAndEAtLeft();
            PlaceCaret("s", -3);
            PlaceCaret("e", 2, extendSelection: true, selectBlock: true);
            SendKeys("_");

            VerifyLastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
_34567890ABCDEF
1234567890ABCDEF");
        }

        [Fact]
        public void TopLeftBottomRightSelection2()
        {
            InsertInputWithSAndEAtLeft();
            PlaceCaret("e", -2);
            PlaceCaret("s", -3, extendSelection: true, selectBlock: true);
            SendKeys("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [Fact]
        public void TopRightBottomLeftSelection()
        {
            InsertInputWithSAndEAtLeft();
            PlaceCaret("s", -2);
            PlaceCaret("e", -3, extendSelection: true, selectBlock: true);
            SendKeys("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [Fact]
        public void BottomLeftTopRightSelection()
        {
            InsertInputWithSAndEAtLeft();
            PlaceCaret("e", -3);
            PlaceCaret("s", -2, extendSelection: true, selectBlock: true);
            SendKeys("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [Fact]
        public void SelectionTouchingSubmissionBuffer()
        {
            InsertInputWithSAndEAtLeft();
            PlaceCaret("s", -2);
            PlaceCaret("e", -1, extendSelection: true, selectBlock: true);
            SendKeys("__");

            VerifyLastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__s234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__e234567890ABCDEF
1234567890ABCDEF");
        }

        [Fact]
        public void PrimaryPromptLongerThanSecondaryZeroWidthNextToPromptSelection()
        {
            InsertInputWithSAndEAtLeft();
            PlaceCaret("s", -1);
            PlaceCaret("e", -1, extendSelection: true, selectBlock: true);
            SendKeys("__");
            
            VerifyLastReplInput(@"1234567890ABCDEF
1234567890ABCDEF
__s234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__1234567890ABCDEF
__e234567890ABCDEF
1234567890ABCDEF");
        }

        [Fact]
        public void Backspace()
        {
            InsertInputWithSAndEInTheMiddle();
            PlaceCaret("s", -1);
            PlaceCaret("e", 0, extendSelection: true, selectBlock: true);
            SendKeys(VirtualKey.Backspace, VirtualKey.Backspace);

            VerifyLastReplInput(@"1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1CDEF
1234567890ABCDEF");

            
        }

        [Fact]
        public void BackspaceBehavesLikeDelete()
        {
            InsertInputWithEInTheMiddle();
            PlaceCaret(">", 0);
            PlaceCaret("e", 0, extendSelection: true, selectBlock: true);
            SendKeys(VirtualKey.Backspace, VirtualKey.Backspace);

            VerifyLastReplInput(@"CDEF
CDEF
CDEF
CDEF
CDEF
CDEF
CDEF
1234567890ABCDEF");
        }
    
        [Fact]
        public void LeftToRightReversedBackspace()
        {
            InsertCode("1234567890ABCDEF");
            PlaceCaret("2", -5);
            PlaceCaret(">", 8, extendSelection: true, selectBlock: true);
            SendKeys(VirtualKey.Backspace);

            VerifyLastReplInput(@"7890ABCDEF");
        }

        [Fact]
        public void LeftToRightReversedDelete()
        {
            InsertCode("1234567890ABCDEF");
            PlaceCaret("1", -1);
            PlaceCaret(">", 5, extendSelection: true, selectBlock: true);
            SendKeys(VirtualKey.Delete);

            VerifyLastReplInput(@"4567890ABCDEF");
        }

        [Fact]
        public void LeftToRightReversedTypeCharacter()
        {
            InsertCode("1234567890ABCDEF");
            PlaceCaret("1", -1);
            PlaceCaret(">", 5, extendSelection: true, selectBlock: true);
            SendKeys("__");

            VerifyLastReplInput(@"__4567890ABCDEF");
        }

        private void InsertInputWithXAtLeft()
        {
            InsertCode(@"1234567890ABCDEF
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
            InsertCode(@"1234567890ABCDEF
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
            InsertCode(@"12s4567890ABCDEF
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
            InsertCode(@"1234567890ABCDEF
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
            VerifyLastReplInput(@"1234567890ABCDEF
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