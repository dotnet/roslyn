// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
        private bool _done;

        public ForegroundNotificationServiceTests()
        {
            TestWorkspace.ResetThreadAffinity();
            _service = new ForegroundNotificationService();
        }

        [ConditionalWpfFact(typeof(x86))]
        public async Task Test_Enqueue()
        {
            var asyncToken = EmptyAsyncToken.Instance;
            var ran = false;

            _service.RegisterNotification(() => { Thread.Sleep(100); }, asyncToken, CancellationToken.None);
            _service.RegisterNotification(() => { /* do nothing */ }, asyncToken, CancellationToken.None);
            _service.RegisterNotification(() => { ran = true; _done = true; }, asyncToken, CancellationToken.None);

            await PumpWait();

            Assert.True(_done);
            Assert.True(ran);
            Assert.True(Empty(_service));
        }

        [WpfFact]
        public async Task Test_Cancellation()
        {
            using (var waitEvent = new AutoResetEvent(initialState: false))
            {
                var asyncToken = EmptyAsyncToken.Instance;
                var ran = false;

                var source = new CancellationTokenSource();
                source.Cancel();

                _service.RegisterNotification(() => { waitEvent.WaitOne(); }, asyncToken, CancellationToken.None);
                _service.RegisterNotification(() => { ran = true; }, asyncToken, source.Token);
                _service.RegisterNotification(() => { _done = true; }, asyncToken, CancellationToken.None);

                waitEvent.Set();
                await PumpWait();

                Assert.False(ran);
                Assert.True(Empty(_service));
            }
        }

        [WpfFact]
        public async Task Test_Delay()
        {
            var asyncToken = EmptyAsyncToken.Instance;

            Stopwatch watch = Stopwatch.StartNew();

            _service.RegisterNotification(() =>
            {
                watch.Stop();
                _done = true;
            }, 50, asyncToken, CancellationToken.None);

            await PumpWait();

            Assert.False(watch.IsRunning);
            Assert.True(watch.ElapsedMilliseconds >= 50);
            Assert.True(Empty(_service));
        }

        [WpfFact]
        public async Task Test_HeavyMultipleCall()
        {
            var asyncToken = EmptyAsyncToken.Instance;
            var count = 0;

            var loopCount = 100000;

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
                        _service.RegisterNotification(() => { _done = true; }, asyncToken, CancellationToken.None);
                    }

                    return false;
                }, asyncToken, CancellationToken.None);
            }

            await PumpWait().ConfigureAwait(false);
            Assert.True(_done);
            Assert.Equal(count, 9000000);
            Assert.True(Empty(_service));
        }

        private async Task PumpWait()
        {
            while (!_done)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1));
            }
        }

        private static bool Empty(IForegroundNotificationService service)
        {
            var temp = (ForegroundNotificationService)service;
            return temp.IsEmpty_TestOnly;
        }
    }
}
