﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class StartPage_OutOfProc : OutOfProcComponent
    {
        private readonly StartPage_InProc _inProc;
        private readonly VisualStudioInstance _instance;

        public StartPage_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _instance = visualStudioInstance;
            _inProc = CreateInProcComponent<StartPage_InProc>(visualStudioInstance);
        }

        public bool IsEnabled()
            => _inProc.IsEnabled();

        public void SetEnabled(bool enabled)
            => _inProc.SetEnabled(enabled);

        public bool CloseWindow()
            => _inProc.CloseWindow();
    }
}
