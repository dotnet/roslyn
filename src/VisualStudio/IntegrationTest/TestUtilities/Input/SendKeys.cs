// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Input
{
    public class SendKeys : AbstractSendKeys
    {
        private readonly VisualStudioInstance _visualStudioInstance;

        public SendKeys(VisualStudioInstance visualStudioInstance)
        {
            _visualStudioInstance = visualStudioInstance;
        }

        protected override void ActivateMainWindow()
        {
            _visualStudioInstance.ActivateMainWindow();
        }

        protected override void WaitForApplicationIdle(CancellationToken cancellationToken)
        {
            _visualStudioInstance.WaitForApplicationIdle(cancellationToken);
        }
    }
}
