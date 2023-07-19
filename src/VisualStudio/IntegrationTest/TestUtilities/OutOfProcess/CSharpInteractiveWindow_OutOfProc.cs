// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
