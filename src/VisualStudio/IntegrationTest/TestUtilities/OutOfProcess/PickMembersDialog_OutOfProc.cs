// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Handles interaction with the Pick Members Dialog.
    /// </summary>
    public class PickMembersDialog_OutOfProc : OutOfProcComponent
    {
        private readonly PickMembersDialog_InProc _inProc;

        public PickMembersDialog_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<PickMembersDialog_InProc>(visualStudioInstance);
        }

        public bool CloseWindow()
            => _inProc.CloseWindow();
    }
}
