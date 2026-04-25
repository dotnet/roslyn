// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Test.Utilities;

[CollectionDefinition(nameof(MemoryCachePerfTest), DisableParallelization = false)]
[Collection(nameof(MemoryCachePerfTest))]
public class MemoryCachePerfTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public async Task HighFrequencyAccess_MaintainsPerformance()
    {
        var cache = new MemoryCache<string, IReadOnlyList<int>>();
        const string Key = "hot-key";
        cache.Set(Key, [1, 2, 3]);

        const int AccessCount = 10_000;
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, Environment.ProcessorCount)
            .Select(x => Task.Run(() =>
            {
                for (var i = 0; i < AccessCount / Environment.ProcessorCount; i++)
                {
                    _ = cache.TryGetValue(Key, out _);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Should complete reasonably quickly (adjust threshold as needed)
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"High-frequency access took too long: {stopwatch.ElapsedMilliseconds}ms");
    }
}
