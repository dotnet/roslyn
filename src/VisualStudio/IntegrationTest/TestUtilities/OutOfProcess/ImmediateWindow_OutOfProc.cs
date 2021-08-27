﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class ImmediateWindow_OutOfProc : OutOfProcComponent
    {
        private readonly ImmediateWindow_InProc _immediateWindowInProc;

        public ImmediateWindow_OutOfProc(VisualStudioInstance visualStudioInstance) : base(visualStudioInstance)
        {
            _immediateWindowInProc = CreateInProcComponent<ImmediateWindow_InProc>(visualStudioInstance);
        }

        public void ShowImmediateWindow(bool clearAll = false)
        {
            _immediateWindowInProc.ShowImmediateWindow();
            if (clearAll)
            {
                ClearAll();
            }
        }

        public string GetText()
        {
            return _immediateWindowInProc.GetText();
        }

        public void ClearAll()
        {
            _immediateWindowInProc.ClearAll();
        }
    }
}
