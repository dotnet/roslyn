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
            var id = Guid.NewGuid().ToString();
            if (eventSource.IsEnabled(EventLevel.Informational, Keywords.Performance))
            {
                eventSource.StartGeneratorDriverRunTime(id);
            }
            return new RunTimer(t => eventSource.StopGeneratorDriverRunTime(t.Ticks, id), eventSource.IsEnabled(EventLevel.Informational, Keywords.Performance));
        }

        public static RunTimer CreateSingleGeneratorRunTimer(this CodeAnalysisEventSource eventSource, ISourceGenerator generator)
        {
            var id = Guid.NewGuid().ToString();
            var type = generator.GetGeneratorType();

            if (eventSource.IsEnabled(EventLevel.Informational, Keywords.Performance))
            {
                eventSource.StartSingleGeneratorRunTime(type.FullName!, type.Assembly.Location, id);
            }
            return new RunTimer(t => eventSource.StopSingleGeneratorRunTime(type.FullName!, type.Assembly.Location, t.Ticks, id), eventSource.IsEnabled(EventLevel.Informational, Keywords.Performance));
        }

        internal readonly struct RunTimer : IDisposable
        {
            private readonly SharedStopwatch _timer;
            private readonly Action<TimeSpan> _callback;
            private readonly bool _shouldExecuteCallback;

            public TimeSpan Elapsed => _timer.Elapsed;

            public RunTimer(Action<TimeSpan> callback, bool shouldExecuteCallback)
            {
                _callback = callback;
                _shouldExecuteCallback = shouldExecuteCallback;

                // start twice to improve accuracy. See AnalyzerExecutor::ExecuteAndCatchIfThrows for more details 
                _ = SharedStopwatch.StartNew();
                _timer = SharedStopwatch.StartNew();
            }

            public void Dispose()
            {
                if (_shouldExecuteCallback)
                {
                    var elapsed = _timer.Elapsed;
                    _callback(elapsed);
                }
            }
        }

    }
}
