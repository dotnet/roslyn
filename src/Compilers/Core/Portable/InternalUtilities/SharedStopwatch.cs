// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    internal struct SharedStopwatch
    {
        private static readonly Stopwatch s_stopwatch = Stopwatch.StartNew();

        private TimeSpan _accumulated;
        private TimeSpan _started;

        public readonly TimeSpan Elapsed
        {
            get
            {
                if (IsRunning)
                    return _accumulated + s_stopwatch.Elapsed - _started;

                return _accumulated;
            }
        }

        public readonly long ElapsedMilliseconds => (long)Elapsed.TotalMilliseconds;
        public readonly bool IsRunning => _started != TimeSpan.Zero;

        public static SharedStopwatch StartNew()
        {
            var result = new SharedStopwatch();
            result.Start();
            return result;
        }

        public void Reset()
        {
            Stop();
            _accumulated = TimeSpan.Zero;
        }

        public void Restart()
        {
            Reset();
            Start();
        }

        public void Start()
        {
            if (!IsRunning)
            {
                _started = s_stopwatch.Elapsed;
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                _accumulated += s_stopwatch.Elapsed - _started;
                _started = TimeSpan.Zero;
            }
        }
    }
}
