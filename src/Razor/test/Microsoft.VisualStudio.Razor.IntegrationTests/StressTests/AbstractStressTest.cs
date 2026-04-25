// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

[IdeSettings(MinVersion = VisualStudioVersion.VS18, RootSuffix = "RoslynDev", MaxAttempts = 1)]
public abstract class AbstractStressTest(ITestOutputHelper testOutputHelper) : AbstractRazorEditorTest(testOutputHelper)
{
    protected override bool AllowDebugFails => true;

    protected Task RunStressTestAsync(Func<int, CancellationToken, Task> iterationFunc)
        => RunStressTestAsync(iterationFunc, TimeSpan.FromHours(1), TimeSpan.FromMinutes(1));

    protected async Task RunStressTestAsync(Func<int, CancellationToken, Task> iterationFunc, TimeSpan maxRunTime, TimeSpan iterationTimeout)
    {
        var min = long.MaxValue;
        var max = long.MinValue;
        var avg = 0L;

        var i = 0;
        var start = DateTime.Now;
        while (DateTime.Now.Subtract(maxRunTime) < start)
        {
            var iterationStart = Stopwatch.GetTimestamp();
            Logger.LogInformation($"**** Test iteration started: #{i} at {DateTime.Now}");

            using var iterationCts = new CancellationTokenSource(iterationTimeout);
            var iterationToken = iterationCts.Token;

            await iterationFunc(i, iterationToken);
            i++;

            var duration = Stopwatch.GetTimestamp() - iterationStart;
            min = Math.Min(min, duration);
            max = Math.Max(max, duration);
            avg = ((avg * (i - 1)) + duration) / i;
            Logger.LogInformation($"**** Test iteration finished: #{i} in {TimeSpan.FromTicks(duration).TotalMilliseconds}ms");
            Logger.LogInformation($"**** Test iteration duration: min={TimeSpan.FromTicks(min).TotalMilliseconds}ms, max={TimeSpan.FromTicks(max).TotalMilliseconds}ms, avg={TimeSpan.FromTicks(avg).TotalMilliseconds}ms");
        }
    }
}
