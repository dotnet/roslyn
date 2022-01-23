// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;

namespace AnalyzerRunner
{
    internal sealed class PerformanceTracker
    {
        private readonly Stopwatch _stopwatch;
#if NETCOREAPP
        private readonly long _initialTotalAllocatedBytes;
#endif

        public PerformanceTracker(Stopwatch stopwatch, long initialTotalAllocatedBytes)
        {
#if NETCOREAPP
            _initialTotalAllocatedBytes = initialTotalAllocatedBytes;
#endif
            _stopwatch = stopwatch;
        }

        public static PerformanceTracker StartNew(bool preciseMemory = true)
        {
#if NETCOREAPP
            var initialTotalAllocatedBytes = GC.GetTotalAllocatedBytes(preciseMemory);
#else
            var initialTotalAllocatedBytes = 0L;
#endif

            return new PerformanceTracker(Stopwatch.StartNew(), initialTotalAllocatedBytes);
        }

        public TimeSpan Elapsed => _stopwatch.Elapsed;

#if NETCOREAPP
        public long AllocatedBytes => GC.GetTotalAllocatedBytes(true) - _initialTotalAllocatedBytes;
#else
        public long AllocatedBytes => 0;
#endif

        public string GetSummary(bool preciseMemory = true)
        {
#if NETCOREAPP
            var elapsedTime = Elapsed;
            var allocatedBytes = GC.GetTotalAllocatedBytes(preciseMemory) - _initialTotalAllocatedBytes;

            return $"{elapsedTime.TotalMilliseconds:0}ms ({allocatedBytes} bytes allocated)";
#else
            return $"{Elapsed.TotalMilliseconds:0}ms";
#endif
        }
    }
}
