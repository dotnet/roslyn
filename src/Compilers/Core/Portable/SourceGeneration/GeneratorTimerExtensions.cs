// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeAnalysisEventSource;

namespace Microsoft.CodeAnalysis
{
    internal static class GeneratorTimerExtensions
    {
        public static RunTimer CreateGeneratorDriverRunTimer(this CodeAnalysisEventSource eventSource)
        {
            if (eventSource.IsEnabled(EventLevel.Informational, Keywords.Performance))
            {
                var id = Guid.NewGuid().ToString();
                eventSource.StartGeneratorDriverRunTime(id);
                return new RunTimer(t => eventSource.StopGeneratorDriverRunTime(t.Ticks, id));
            }
            else
            {
                return new RunTimer();
            }
        }

        public static RunTimer CreateSingleGeneratorRunTimer(this CodeAnalysisEventSource eventSource, ISourceGenerator generator)
        {
            if (eventSource.IsEnabled(EventLevel.Informational, Keywords.Performance))
            {
                var id = Guid.NewGuid().ToString();
                var type = generator.GetGeneratorType();
                eventSource.StartSingleGeneratorRunTime(type.FullName!, type.Assembly.Location, id);
                return new RunTimer(t => eventSource.StopSingleGeneratorRunTime(type.FullName!, type.Assembly.Location, t.Ticks, id));
            }
            else
            {
                return new RunTimer();
            }
        }

        internal readonly struct RunTimer : IDisposable
        {
            private readonly SharedStopwatch _timer;
            private readonly Action<TimeSpan>? _callback;

            public TimeSpan Elapsed => _timer.Elapsed;

            public RunTimer()
            {
                // start twice to improve accuracy. See AnalyzerExecutor.ExecuteAndCatchIfThrows for more details 
                _ = SharedStopwatch.StartNew();
                _timer = SharedStopwatch.StartNew();
                _callback = null;
            }

            public RunTimer(Action<TimeSpan> callback)
                : this()
            {
                _callback = callback;
            }

            public void Dispose()
            {
                if (_callback is not null)
                {
                    var elapsed = _timer.Elapsed;
                    _callback(elapsed);
                }
            }
        }
    }
}
