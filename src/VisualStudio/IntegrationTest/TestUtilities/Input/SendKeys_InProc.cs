// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Input
{
    internal class SendKeys_InProc : AbstractSendKeys
    {
        private readonly VisualStudio_InProc _visualStudioInstance;

        public SendKeys_InProc(VisualStudio_InProc visualStudioInstance)
        {
            _visualStudioInstance = visualStudioInstance;
        }

        protected override void ActivateMainWindow()
        {
            _visualStudioInstance.ActivateMainWindow();
        }

        protected override void WaitForApplicationIdle(CancellationToken cancellationToken)
        {
            _visualStudioInstance.WaitForApplicationIdle(Helper.HangMitigatingTimeout);
        }
    }
}
