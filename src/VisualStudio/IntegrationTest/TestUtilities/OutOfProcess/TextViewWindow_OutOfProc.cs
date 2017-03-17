// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public abstract class TextViewWindow_OutOfProc : OutOfProcComponent
    {
        public TextViewWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
        }

        internal abstract TextViewWindow_InProc InProc { get; }

        public int GetCaretPosition()
            => InProc.GetCaretPosition();

        public string[] GetCompletionItems()
        {
            WaitForCompletionSet();
            return InProc.GetCompletionItems();
        }

        public void PlaceCaret(
            string marker,
            int charsOffset,
            int occurrence,
            bool extendSelection,
            bool selectBlock)
            => InProc.PlaceCaret(
                marker,
                charsOffset,
                occurrence,
                extendSelection,
                selectBlock);

        public string[] GetCurrentClassifications()
            => InProc.GetCurrentClassifications();
    }
}
