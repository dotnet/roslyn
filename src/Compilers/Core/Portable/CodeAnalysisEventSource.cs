// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.CodeAnalysis
{
    [EventSource(Name = "Microsoft-CodeAnalysis-General")]
    internal sealed class CodeAnalysisEventSource : EventSource
    {
        public static readonly CodeAnalysisEventSource Log = new CodeAnalysisEventSource();

        public static class Keywords
        {
            public const EventKeywords Performance = (EventKeywords)1;
            public const EventKeywords Correctness = (EventKeywords)2;
        }

        public static class Tasks
        {
            public const EventTask GeneratorDriverRunTime = (EventTask)1;
            public const EventTask SingleGeneratorRunTime = (EventTask)2;
            public const EventTask BuildStateTable = (EventTask)3;
        }

        private CodeAnalysisEventSource() { }

        [Event(1, Keywords = Keywords.Performance, Level = EventLevel.Informational, Opcode = EventOpcode.Start, Task = Tasks.GeneratorDriverRunTime)]
        internal void StartGeneratorDriverRunTime(string id) => WriteEvent(1, id);

        [Event(2, Message = "Generators ran for {0} ticks", Keywords = Keywords.Performance, Level = EventLevel.Informational, Opcode = EventOpcode.Stop, Task = Tasks.GeneratorDriverRunTime)]
        internal void StopGeneratorDriverRunTime(long elapsedTicks, string id) => WriteEvent(2, elapsedTicks, id);

        [Event(3, Keywords = Keywords.Performance, Level = EventLevel.Informational, Opcode = EventOpcode.Start, Task = Tasks.SingleGeneratorRunTime)]
        internal void StartSingleGeneratorRunTime(string generatorName, string assemblyPath, string id) => WriteEvent(3, generatorName, assemblyPath, id);

        [Event(4, Message = "Generator {0} ran for {2} ticks", Keywords = Keywords.Performance, Level = EventLevel.Informational, Opcode = EventOpcode.Stop, Task = Tasks.SingleGeneratorRunTime)]
        internal void StopSingleGeneratorRunTime(string generatorName, string assemblyPath, long elapsedTicks, string id)
        {
            Span<EventData> data = stackalloc EventData[]
            {
                GetEventDataForString(generatorName),
                GetEventDataForString(assemblyPath),
                GetEventDataForLong(elapsedTicks),
                GetEventDataForString(id),
            };

            unsafe
            {
                fixed (EventSource.EventData* dataPtr = data)
                {
                    WriteEventCore(eventId: 4, data.Length, dataPtr);
                }
            }
        }

        [Event(5, Message = "Generator '{0}' failed with exception: {1}", Level = EventLevel.Error)]
        internal void GeneratorException(string generatorName, string exception) => WriteEvent(5, generatorName, exception);

        [Event(6, Message = "Node {0} transformed", Keywords = Keywords.Correctness, Level = EventLevel.Verbose, Task = Tasks.BuildStateTable)]
        internal void NodeTransform(int nodeHashCode, string name, string tableType, int previousTable, string previousTableContent, int newTable, string newTableContent, int input1, int input2)
        {
            Span<EventData> data = stackalloc EventData[]
            {
                GetEventDataForInt(nodeHashCode),
                GetEventDataForString(name),
                GetEventDataForString(tableType),
                GetEventDataForInt(previousTable),
                GetEventDataForString(previousTableContent),
                GetEventDataForInt(newTable),
                GetEventDataForString(newTableContent),
                GetEventDataForInt(input1),
                GetEventDataForInt(input2),
            };

            unsafe
            {
                fixed (EventSource.EventData* dataPtr = data)
                {
                    WriteEventCore(eventId: 6, data.Length, dataPtr);
                }
            }
        }

        private static unsafe EventData GetEventDataForString(string value)
        {
            fixed (char* ptr = value)
            {
                return new EventData()
                {
                    DataPointer = (IntPtr)ptr,
                    Size = (value.Length + 1) * sizeof(char),
                };
            }
        }

        private static EventData GetEventDataForInt(int value)
        {
            return new EventData()
            {
                DataPointer = (IntPtr)value,
                Size = sizeof(int),
            };
        }

        private static EventData GetEventDataForLong(long value)
        {
            return new EventData()
            {
                DataPointer = (IntPtr)value,
                Size = sizeof(long),
            };
        }
    }
}
