﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
