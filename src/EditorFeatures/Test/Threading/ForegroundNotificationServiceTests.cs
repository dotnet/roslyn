// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.ForegroundNotification;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Threading
{
    public class ForegroundNotificationServiceTests
    {
        private readonly IForegroundNotificationService _service;

        public ForegroundNotificationServiceTests()
        {
            TestWorkspace.ResetThreadAffinity();
            _service = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExportedValue<IForegroundNotificationService>();
        }

        [WpfFact]
        public void Test_Enqueue()
        {
            var asyncToken = EmptyAsyncToken.Instance;
            var done = false;
            var ran = false;

            _service.RegisterNotification(() => { Thread.Sleep(100); }, asyncToken, CancellationToken.None);
            _service.RegisterNotification(() => { /* do nothing */ }, asyncToken, CancellationToken.None);
            _service.RegisterNotification(() => { ran = true; done = true; }, asyncToken, CancellationToken.None);

            PumpWait(ref done);

            Assert.True(ran);
            Assert.True(Empty(_service));
        }

        [WpfFact]
        public void Test_Cancellation()
        {
            using (var waitEvent = new AutoResetEvent(initialState: false))
            {
                var asyncToken = EmptyAsyncToken.Instance;
                var done = false;
                var ran = false;

                var source = new CancellationTokenSource();
                source.Cancel();

                _service.RegisterNotification(() => { waitEvent.WaitOne(); }, asyncToken, CancellationToken.None);
                _service.RegisterNotification(() => { ran = true; }, asyncToken, source.Token);
                _service.RegisterNotification(() => { done = true; }, asyncToken, CancellationToken.None);

                waitEvent.Set();
                PumpWait(ref done);

                Assert.False(ran);
                Assert.True(Empty(_service));
            }
        }

        [WpfFact]
        public void Test_Delay()
        {
            var asyncToken = EmptyAsyncToken.Instance;

            bool done = false;
            DateTime now = DateTime.UtcNow;
            DateTime set = DateTime.UtcNow;

            _service.RegisterNotification(() =>
            {
                set = DateTime.UtcNow;
                done = true;
            }, 50, asyncToken, CancellationToken.None);

            PumpWait(ref done);

            Assert.True(set.Subtract(now).TotalMilliseconds > 50);
            Assert.True(Empty(_service));
        }

        [WpfFact]
        public void Test_HeavyMultipleCall()
        {
            var asyncToken = EmptyAsyncToken.Instance;
            var count = 0;

            var loopCount = 100000;
            var done = false;

            for (var i = 0; i < loopCount; i++)
            {
                var index = i;
                var retry = false;

                _service.RegisterNotification(() =>
                {
                    if (retry)
                    {
                        return false;
                    }

                    var source = new CancellationTokenSource();

                    _service.RegisterNotification(() =>
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            count++;
                        }
                    }, asyncToken, source.Token);

                    if ((index % 10) == 0)
                    {
                        source.Cancel();

                        retry = true;
                        return retry;
                    }

                    if (index == loopCount - 1)
                    {
                        _service.RegisterNotification(() => { done = true; }, asyncToken, CancellationToken.None);
                    }

                    return false;
                }, asyncToken, CancellationToken.None);
            }

            PumpWait(ref done);

            Assert.True(done);
            Assert.Equal(count, 9000000);
            Assert.True(Empty(_service));
        }

        private static void PumpWait(ref bool done)
        {
            while (!done)
            {
                new FrameworkElement().Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            }
        }

        private static bool Empty(IForegroundNotificationService service)
        {
            var temp = (ForegroundNotificationService)service;
            return temp.IsEmpty_TestOnly;
        }
    }
}
