// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal struct GeneratorTimer : IDisposable
    {
        private readonly Action<TimeSpan> _reportCallback;

        private readonly SharedStopwatch _timer;

        public TimeSpan Elapsed => _timer.Elapsed;

        public GeneratorTimer(Action<TimeSpan> reportCallback)
        {
            _reportCallback = reportCallback;

            // start twice to improve accuracy. See AnalyzerExecutor::ExecuteAndCatchIfThrows for more details 
            _ = SharedStopwatch.StartNew();
            _timer = SharedStopwatch.StartNew();
        }

        public void Dispose()
        {
            var elapsed = _timer.Elapsed;
            _reportCallback.Invoke(elapsed);
        }
    }
}
