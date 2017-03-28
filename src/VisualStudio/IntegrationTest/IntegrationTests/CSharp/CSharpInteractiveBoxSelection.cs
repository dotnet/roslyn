// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Interactive;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpInteractiveBoxSelection : AbstractInteractiveWindowTest
    {
        public CSharpInteractiveBoxSelection(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            this.SubmitText("#cls");
        }

        public new void Dispose()
        {
            this.ExecuteCommand(WellKnownCommandNames.Edit_SelectionCancel);
            base.Dispose();
        }

        [Fact]
        public void TopLeftBottomRightPromptToSymbol()
        {
            InsertInputWithXAtLeft();

            this.PlaceCaret(">", 1);
            this.PlaceCaret("x", 0, extendSelection: true, selectBlock: true);
            this.SendKeys("__", VirtualKey.Escape, "|");

            this.VerifyLastReplInput(@"__234567890ABCDEF
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
            this.PlaceCaret("x", 0);
            this.PlaceCaret(">", 1, extendSelection: true, selectBlock: true);
            this.SendKeys("__", VirtualKey.Escape, "|");

            this.VerifyLastReplInput(@"__|234567890ABCDEF
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
            this.PlaceCaret(">", 3);
            this.PlaceCaret("x", -2, extendSelection: true, selectBlock: true);
            this.SendKeys("__", VirtualKey.Escape, "|");

            this.VerifyLastReplInput(@"__234567890ABCDEF
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
            this.PlaceCaret("x", -2);
            this.PlaceCaret(">", 3, extendSelection: true, selectBlock: true);
            this.SendKeys("__", VirtualKey.Escape, "|");

            this.VerifyLastReplInput(@"__|234567890ABCDEF
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
            this.PlaceCaret("s", -1);
            this.PlaceCaret("e", 1, extendSelection: true, selectBlock: true);
            this.SendKeys("__", VirtualKey.Escape, "|");

            this.VerifyLastReplInput(@"1234567890ABCDEF
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
            this.PlaceCaret("e", 1);
            this.PlaceCaret("s", -1, extendSelection: true, selectBlock: true);
            this.SendKeys("__", VirtualKey.Escape, "|");

            this.VerifyLastReplInput(@"1234567890ABCDEF
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
            this.PlaceCaret("s", 1);
            this.PlaceCaret("e", -1, extendSelection: true, selectBlock: true);
            this.SendKeys("__", VirtualKey.Escape, "|");

            this.VerifyLastReplInput(@"1234567890ABCDEF
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
            this.PlaceCaret("e", -1);
            this.PlaceCaret("s", 1, extendSelection: true, selectBlock: true);
            this.SendKeys("__", VirtualKey.Escape, "|");

            this.VerifyLastReplInput(@"1234567890ABCDEF
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
            this.PlaceCaret("s", -3);
            this.PlaceCaret("e", 2, extendSelection: true, selectBlock: true);
            this.SendKeys("_");

            this.VerifyLastReplInput(@"1234567890ABCDEF
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
            this.PlaceCaret("e", -2);
            this.PlaceCaret("s", -3, extendSelection: true, selectBlock: true);
            this.SendKeys("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [Fact]
        public void TopRightBottomLeftSelection()
        {
            InsertInputWithSAndEAtLeft();
            this.PlaceCaret("s", -2);
            this.PlaceCaret("e", -3, extendSelection: true, selectBlock: true);
            this.SendKeys("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [Fact]
        public void BottomLeftTopRightSelection()
        {
            InsertInputWithSAndEAtLeft();
            this.PlaceCaret("e", -3);
            this.PlaceCaret("s", -2, extendSelection: true, selectBlock: true);
            this.SendKeys("_");

            VerifyOriginalCodeWithSAndEAtLeft();
        }

        [Fact]
        public void SelectionTouchingSubmissionBuffer()
        {
            InsertInputWithSAndEAtLeft();
            this.PlaceCaret("s", -2);
            this.PlaceCaret("e", -1, extendSelection: true, selectBlock: true);
            this.SendKeys("__");

            this.VerifyLastReplInput(@"1234567890ABCDEF
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
            this.PlaceCaret("s", -1);
            this.PlaceCaret("e", -1, extendSelection: true, selectBlock: true);
            this.SendKeys("__");

            this.VerifyLastReplInput(@"1234567890ABCDEF
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
            this.PlaceCaret("s", -1);
            this.PlaceCaret("e", 0, extendSelection: true, selectBlock: true);
            this.SendKeys(VirtualKey.Backspace, VirtualKey.Backspace);

            this.VerifyLastReplInput(@"1CDEF
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
            this.PlaceCaret(">", 0);
            this.PlaceCaret("e", 0, extendSelection: true, selectBlock: true);
            this.SendKeys(VirtualKey.Backspace, VirtualKey.Backspace);

            this.VerifyLastReplInput(@"CDEF
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
            this.InsertCode("1234567890ABCDEF");
            this.PlaceCaret("2", -5);
            this.PlaceCaret(">", 8, extendSelection: true, selectBlock: true);
            this.SendKeys(VirtualKey.Backspace);

            this.VerifyLastReplInput(@"7890ABCDEF");
        }

        [Fact]
        public void LeftToRightReversedDelete()
        {
            this.InsertCode("1234567890ABCDEF");
            this.PlaceCaret("1", -1);
            this.PlaceCaret(">", 5, extendSelection: true, selectBlock: true);
            this.SendKeys(VirtualKey.Delete);

            this.VerifyLastReplInput(@"4567890ABCDEF");
        }

        [Fact]
        public void LeftToRightReversedTypeCharacter()
        {
            this.InsertCode("1234567890ABCDEF");
            this.PlaceCaret("1", -1);
            this.PlaceCaret(">", 5, extendSelection: true, selectBlock: true);
            this.SendKeys("__");

            this.VerifyLastReplInput(@"__4567890ABCDEF");
        }

        private void InsertInputWithXAtLeft()
        {
            this.InsertCode(@"1234567890ABCDEF
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
            this.InsertCode(@"1234567890ABCDEF
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
            this.InsertCode(@"12s4567890ABCDEF
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
            this.InsertCode(@"1234567890ABCDEF
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
            this.VerifyLastReplInput(@"1234567890ABCDEF
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