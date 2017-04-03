// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class ImmediateWindow_OutOfProc : TextViewWindow_OutOfProc
    {
        private readonly ImmediateWindow_InProc _interactiveWindowInProc;

        internal ImmediateWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base (visualStudioInstance)
        {
            _interactiveWindowInProc = (ImmediateWindow_InProc)_textViewWindowInProc;
        }

        public void ShowWindow()
            => _interactiveWindowInProc.ShowWindow();

        public void SendKeys(params object[] keys)
        {
            ShowWindow();
            VisualStudioInstance.SendKeys.Send(keys);
        }

        internal override TextViewWindow_InProc CreateInProcComponent(VisualStudioInstance visualStudioInstance)
            => CreateInProcComponent<ImmediateWindow_InProc>(visualStudioInstance);

        public void Clear()
        {
            ShowWindow();
            _interactiveWindowInProc.Clear();
        }
    }
}
