// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp
{
    public sealed partial class CSharpCompilation
    {
        private SymbolCompletionWaitStatistics? _lazySymbolCompletionWaitStatistics;

        internal enum SymbolCompletionWaitOutcome
        {
            Completed,
            Canceled,
            TimedOut,
            Interrupted
        }

        internal void RecordSymbolCompletionWait(uint elapsedMilliseconds, SymbolCompletionWaitOutcome outcome)
        {
            var statistics = Volatile.Read(ref _lazySymbolCompletionWaitStatistics);
            if (statistics is null)
            {
                statistics = new SymbolCompletionWaitStatistics();
                statistics = Interlocked.CompareExchange(ref _lazySymbolCompletionWaitStatistics, statistics, null) ?? statistics;
            }

            statistics.Record(elapsedMilliseconds, outcome);
        }

        internal override void ReportSymbolCompletionWaits(TextWriter writer)
        {
            var statistics = Volatile.Read(ref _lazySymbolCompletionWaitStatistics) ?? new SymbolCompletionWaitStatistics();
            statistics.Report(writer, AssemblyName);
        }

        private sealed class SymbolCompletionWaitStatistics
        {
            private readonly object _gate = new object();
            private readonly long[] _buckets = new long[9];
            private readonly long[] _outcomes = new long[4];
            private long _totalMilliseconds;
            private uint _maxMilliseconds;

            internal void Record(uint elapsedMilliseconds, SymbolCompletionWaitOutcome outcome)
            {
                int bucket = elapsedMilliseconds switch
                {
                    0 => 0,
                    < 10 => 1,
                    < 100 => 2,
                    < 1_000 => 3,
                    < 5_000 => 4,
                    < 10_000 => 5,
                    < 30_000 => 6,
                    < 60_000 => 7,
                    _ => 8
                };

                lock (_gate)
                {
                    _buckets[bucket]++;
                    _outcomes[(int)outcome]++;
                    _totalMilliseconds += elapsedMilliseconds;
                    _maxMilliseconds = Math.Max(_maxMilliseconds, elapsedMilliseconds);
                }
            }

            internal void Report(TextWriter writer, string? assemblyName)
            {
                lock (_gate)
                {
                    // This is temporary CI instrumentation. Durations are quantized by TickCount;
                    // accumulated waits overlap across threads and are not compilation wall time.
                    writer.WriteLine(FormattableString.Invariant(
                        $"SymbolCompletionWaits: assembly=\"{assemblyName}\" completed={_outcomes[0]} canceled={_outcomes[1]} timedOut={_outcomes[2]} interrupted={_outcomes[3]} totalWaitMs={_totalMilliseconds} maxWaitMs={_maxMilliseconds} ms[0]={_buckets[0]} ms[1,10)={_buckets[1]} ms[10,100)={_buckets[2]} ms[100,1000)={_buckets[3]} ms[1000,5000)={_buckets[4]} ms[5000,10000)={_buckets[5]} ms[10000,30000)={_buckets[6]} ms[30000,60000)={_buckets[7]} ms[60000,inf)={_buckets[8]}"));
                }
            }
        }
    }
}
