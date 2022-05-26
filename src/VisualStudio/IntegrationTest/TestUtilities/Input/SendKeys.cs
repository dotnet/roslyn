// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
