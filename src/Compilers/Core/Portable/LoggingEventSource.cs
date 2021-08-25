// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    [EventSource(Name = "Microsoft-CodeAnalysis-General")]
    internal sealed class LoggingEventSource : EventSource
    {
        public static readonly LoggingEventSource Instance = new LoggingEventSource();

        public class Keywords
        {
            public const EventKeywords Performance = (EventKeywords)1;
        }

        public class Tasks
        {
            public const EventTask GeneratorDriverRunTime = (EventTask)1;
            public const EventTask SingleGeneratorRunTime = (EventTask)2;

        }

        [NonEvent]
        public void ReportGeneratorDriverRunTime(TimeSpan elapsed) => ReportGeneratorDriverRunTime(elapsed.Ticks);

        [Event(1, Message = "Generators ran for {0} ticks", Keywords = Keywords.Performance, Level = EventLevel.Informational, Task = Tasks.GeneratorDriverRunTime)]
        private void ReportGeneratorDriverRunTime(long elapsedTicks) => WriteEvent(1, elapsedTicks);

        [NonEvent]
        public void ReportSingleGeneratorRunTime(ISourceGenerator generator, TimeSpan elapsed)
        {
            var type = generator.GetGeneratorType();
            ReportSingleGeneratorRunTime(type.FullName!, type.Assembly.Location, elapsed.Ticks);
        }

        [Event(2, Message = "Generator {0} ran for {2} ticks", Keywords = Keywords.Performance, Level = EventLevel.Informational, Task = Tasks.SingleGeneratorRunTime)]
        private void ReportSingleGeneratorRunTime(string generatorName, string assemblyPath, long elapsedTicks) => WriteEvent(2, generatorName, assemblyPath, elapsedTicks);

    }
}
