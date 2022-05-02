// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.Threading;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    internal class InputInProcess : InProcComponent
    {
        public InputInProcess(TestServices testServices)
            : base(testServices)
        {
            SendKeys = new(testServices);
        }

        private SendKeysImpl SendKeys { get; }

        internal async Task SendAsync(params object[] keys)
        {
            // AbstractSendKeys runs synchronously, so switch to a background thread before the call
            await TaskScheduler.Default;

            SendKeys.Send(keys);
        }

        private class SendKeysImpl : AbstractSendKeys
        {
            public SendKeysImpl(TestServices testServices)
            {
                TestServices = testServices;
            }

            public TestServices TestServices { get; }

            protected override void ActivateMainWindow()
            {
                TestServices.JoinableTaskFactory.Run(async () =>
                {
                    await TestServices.Editor.ActivateAsync(CancellationToken.None);
                });
            }

            protected override void WaitForApplicationIdle(CancellationToken cancellationToken)
            {
                TestServices.JoinableTaskFactory.Run(async () =>
                {
                    await WaitForApplicationIdleAsync(cancellationToken);
                });
            }
        }
    }
}
