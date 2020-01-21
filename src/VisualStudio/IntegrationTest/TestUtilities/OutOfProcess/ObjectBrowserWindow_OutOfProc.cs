// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class ObjectBrowserWindow_OutOfProc : OutOfProcComponent
    {
        internal readonly ObjectBrowserWindow_InProc _inProc;

        internal ObjectBrowserWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<ObjectBrowserWindow_InProc>(visualStudioInstance);
        }

        public void CloseWindow()
            => _inProc.CloseWindow();
    }
}
