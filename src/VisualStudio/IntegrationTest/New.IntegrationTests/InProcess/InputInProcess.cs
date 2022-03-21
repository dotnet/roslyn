// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class InputInProcess
    {
        private SendKeysImpl? _lazySendKeys;
        private SendKeysToNavigateToImpl? _lazySendKeysToNavigateTo;
        private SendKeysWithoutActivateImpl? _lazySendKeysWithoutActivate;

        private SendKeysImpl SendKeys => _lazySendKeys ?? throw ExceptionUtilities.Unreachable;
        private SendKeysToNavigateToImpl SendKeysToNavigateTo => _lazySendKeysToNavigateTo ?? throw ExceptionUtilities.Unreachable;
        private SendKeysWithoutActivateImpl SendKeysWithoutActivate => _lazySendKeysWithoutActivate ?? throw ExceptionUtilities.Unreachable;

        protected override async Task InitializeCoreAsync()
        {
            await base.InitializeCoreAsync();
            _lazySendKeys = new SendKeysImpl(TestServices);
            _lazySendKeysToNavigateTo = new SendKeysToNavigateToImpl(TestServices);
            _lazySendKeysWithoutActivate = new SendKeysWithoutActivateImpl(TestServices);
        }

        internal async Task SendAsync(params object[] keys)
        {
            // AbstractSendKeys runs synchronously, so switch to a background thread before the call
            await TaskScheduler.Default;

            SendKeys.Send(keys);
        }

        internal async Task SendWithoutActivateAsync(params object[] keys)
        {
            // AbstractSendKeys runs synchronously, so switch to a background thread before the call
            await TaskScheduler.Default;

            SendKeysWithoutActivate.Send(keys);
        }

        internal async Task SendToNavigateToAsync(params object[] keys)
        {
            // AbstractSendKeys runs synchronously, so switch to a background thread before the call
            await TaskScheduler.Default;

            SendKeysToNavigateTo.Send(keys);
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

        private class SendKeysWithoutActivateImpl : SendKeysImpl
        {
            public SendKeysWithoutActivateImpl(TestServices testServices)
                : base(testServices)
            {
            }

            protected override void ActivateMainWindow()
            {
                // Take no direct action
            }
        }

        private class SendKeysToNavigateToImpl : SendKeysImpl
        {
            public SendKeysToNavigateToImpl(TestServices testServices)
                : base(testServices)
            {
            }

            protected override void ActivateMainWindow()
            {
                // Take no direct action, but assert the correct item already has focus
                TestServices.JoinableTaskFactory.Run(async () =>
                {
                    await TestServices.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var searchBox = Assert.IsAssignableFrom<TextBox>(Keyboard.FocusedElement);
                    Assert.Equal("PART_SearchBox", searchBox.Name);
                });
            }
        }
    }
}
