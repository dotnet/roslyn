﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.ForegroundNotification;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Threading
{
    public class ForegroundNotificationServiceTests
    {
        private readonly ForegroundNotificationService _service;
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
            Assert.True(_service.IsEmpty_TestOnly);
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
                Assert.True(_service.IsEmpty_TestOnly);
            }
        }

        [WpfFact]
        public async Task Test_Delay()
        {
            // NOTE: Don't be tempted to use DateTime or Stopwatch to measure this
            // Switched to Environment.TickCount use the same clock as the notification
            // service, see: https://github.com/dotnet/roslyn/issues/7512.

            var asyncToken = EmptyAsyncToken.Instance;

            int startMilliseconds = Environment.TickCount;
            int? elapsedMilliseconds = null;

            _service.RegisterNotification(() =>
            {
                elapsedMilliseconds = Environment.TickCount - startMilliseconds;

                _done = true;
            }, 50, asyncToken, CancellationToken.None);

            await PumpWait();

            Assert.True(elapsedMilliseconds >= 50, $"Notification fired after {elapsedMilliseconds}, instead of 50.");
            Assert.True(_service.IsEmpty_TestOnly);
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
            Assert.True(_service.IsEmpty_TestOnly);
        }

        private async Task PumpWait()
        {
            while (!_done)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1));
            }
        }
    }
}
