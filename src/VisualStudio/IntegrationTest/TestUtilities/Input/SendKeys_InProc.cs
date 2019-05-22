// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
