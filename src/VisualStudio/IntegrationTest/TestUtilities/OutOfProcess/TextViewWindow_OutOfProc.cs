// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public abstract class TextViewWindow_OutOfProc : OutOfProcComponent
    {
        internal readonly TextViewWindow_InProc _textViewWindowInProc;

        internal TextViewWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _textViewWindowInProc = CreateInProcComponent(visualStudioInstance);
        }

        internal abstract TextViewWindow_InProc CreateInProcComponent(VisualStudioInstance visualStudioInstance);
    }
}
