// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Roslyn.Utilities
{
    internal readonly struct SharedStopwatch
    {
        static SharedStopwatch()
        {
            // CRITICAL: The size of this struct is the size of a TimeSpan (which itself is the size of a long).  This
            // allows stopwatches to be atomically overwritten, without a concern for torn writes, as long as we're
            // running on 64bit machines.  Make sure this value doesn't change as that will cause these current
            // consumers to be invalid.
            RoslynDebug.Assert(Marshal.SizeOf(typeof(SharedStopwatch)) == 8);
        }

        private static readonly Stopwatch s_stopwatch = Stopwatch.StartNew();

        private readonly TimeSpan _started;

        private SharedStopwatch(TimeSpan started)
        {
            _started = started;
        }

        public TimeSpan Elapsed => s_stopwatch.Elapsed - _started;

        public static SharedStopwatch StartNew()
        {
            // This call to StartNewCore isn't required, but is included to avoid measurement errors
            // which can occur during periods of high allocation activity. In some cases, calls to Stopwatch
            // operations can block at their return point on the completion of a background GC operation. When
            // this occurs, the GC wait time ends up included in the measured time span. In the event the first
            // call to StartNewCore blocked on a GC operation, the second call will most likely occur when the
            // GC is no longer active. In practice, a substantial improvement to the consistency of analyzer
            // timing data was observed.
            //
            // Note that the call to SharedStopwatch.Elapsed is not affected, because the GC wait will occur
            // after the timer has already recorded its stop time.
            _ = StartNewCore();
            return StartNewCore();
        }

        private static SharedStopwatch StartNewCore()
            => new SharedStopwatch(s_stopwatch.Elapsed);
    }
}
