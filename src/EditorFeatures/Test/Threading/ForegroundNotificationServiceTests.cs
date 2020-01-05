// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.ForegroundNotification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Threading
{
    [UseExportProvider]
    public class ForegroundNotificationServiceTests
    {
        private ForegroundNotificationService _service;
        private bool _done;

        private ForegroundNotificationService Service
        {
            get
            {
                if (_service is null)
                {
                    var threadingContext = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExportedValue<IThreadingContext>();
                    _service = new ForegroundNotificationService(threadingContext);
                }

                return _service;
            }
        }

        [ConditionalWpfFact(typeof(x86))]
        public async Task Test_Enqueue()
        {
            var asyncToken = EmptyAsyncToken.Instance;
            var ran = false;

            Service.RegisterNotification(() => { Thread.Sleep(100); }, asyncToken, CancellationToken.None);
            Service.RegisterNotification(() => { /* do nothing */ }, asyncToken, CancellationToken.None);
            Service.RegisterNotification(() => { ran = true; _done = true; }, asyncToken, CancellationToken.None);

            await PumpWait();

            Assert.True(_done);
            Assert.True(ran);
            Assert.True(Service.IsEmpty_TestOnly);
        }

        [WpfFact]
        public async Task Test_Cancellation()
        {
            using var waitEvent = new AutoResetEvent(initialState: false);
            var asyncToken = EmptyAsyncToken.Instance;
            var ran = false;

            var source = new CancellationTokenSource();
            source.Cancel();

            Service.RegisterNotification(() => { waitEvent.WaitOne(); }, asyncToken, CancellationToken.None);
            Service.RegisterNotification(() => { ran = true; }, asyncToken, source.Token);
            Service.RegisterNotification(() => { _done = true; }, asyncToken, CancellationToken.None);

            waitEvent.Set();
            await PumpWait();

            Assert.False(ran);
            Assert.True(Service.IsEmpty_TestOnly);
        }

        [WpfFact]
        public async Task Test_Delay()
        {
            // NOTE: Don't be tempted to use DateTime or Stopwatch to measure this
            // Switched to Environment.TickCount use the same clock as the notification
            // service, see: https://github.com/dotnet/roslyn/issues/7512.

            var asyncToken = EmptyAsyncToken.Instance;

            var startMilliseconds = Environment.TickCount;
            int? elapsedMilliseconds = null;

            Service.RegisterNotification(() =>
            {
                elapsedMilliseconds = Environment.TickCount - startMilliseconds;

                _done = true;
            }, 50, asyncToken, CancellationToken.None);

            await PumpWait();

            Assert.True(elapsedMilliseconds >= 50, $"Notification fired after {elapsedMilliseconds}, instead of 50.");
            Assert.True(Service.IsEmpty_TestOnly);
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

                Service.RegisterNotification(() =>
                {
                    if (retry)
                    {
                        return false;
                    }

                    var source = new CancellationTokenSource();

                    Service.RegisterNotification(() =>
                    {
                        for (var j = 0; j < 100; j++)
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
                        Service.RegisterNotification(() => { _done = true; }, asyncToken, CancellationToken.None);
                    }

                    return false;
                }, asyncToken, CancellationToken.None);
            }

            await PumpWait().ConfigureAwait(false);
            Assert.True(_done);
            Assert.Equal(9000000, count);
            Assert.True(Service.IsEmpty_TestOnly);
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
