// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class CSharpInteractiveWindow_OutOfProc : InteractiveWindow_OutOfProc
    {
        public CSharpInteractiveWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance) { }

        internal override TextViewWindow_InProc CreateInProcComponent(VisualStudioInstance visualStudioInstance)
            => CreateInProcComponent<CSharpInteractiveWindow_InProc>(visualStudioInstance);
    }
}
