// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities.InProcess;

namespace Roslyn.VisualStudio.Test.Utilities.OutOfProcess
{
    public class CSharpInteractiveWindow_OutOfProc : InteractiveWindow_OutOfProc
    {
        public CSharpInteractiveWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
        }

        internal override InteractiveWindow_InProc CreateInProcComponent(VisualStudioInstance visualStudioInstance)
        {
            return CreateInProcComponent<CSharpInteractiveWindow_InProc>(visualStudioInstance);
        }
    }
}
