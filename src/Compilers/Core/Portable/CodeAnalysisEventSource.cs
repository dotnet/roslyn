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
            unsafe
            {
                fixed (char* generatorNameBytes = generatorName)
                fixed (char* assemblyPathBytes = assemblyPath)
                fixed (char* idBytes = id)
                {
                    EventSource.EventData* data = stackalloc EventSource.EventData[4];
                    data[0].DataPointer = (IntPtr)generatorNameBytes;
                    data[0].Size = ((generatorName.Length + 1) * 2);
                    data[1].DataPointer = (IntPtr)assemblyPathBytes;
                    data[1].Size = ((assemblyPath.Length + 1) * 2);
                    data[2].DataPointer = (IntPtr)(&elapsedTicks);
                    data[2].Size = 8;
                    data[3].DataPointer = (IntPtr)idBytes;
                    data[3].Size = ((id.Length + 1) * 2);
                    WriteEventCore(4, 4, data);
                }
            }
        }

        [Event(5, Message = "Generator '{0}' failed with exception: {1}", Level = EventLevel.Error)]
        internal void GeneratorException(string generatorName, string exception) => WriteEvent(5, generatorName, exception);

        [Event(6, Message = "Node {0} transformed", Keywords = Keywords.Correctness, Level = EventLevel.Verbose, Task = Tasks.BuildStateTable)]
        internal void NodeTransform(int nodeHashCode, string name, string tableType, int previousTable, string previousTableContent, int newTable, string newTableContent, int input1, int input2)
        {
            unsafe
            {
                fixed (char* nameBytes = name)
                fixed (char* tableTypeBytes = tableType)
                fixed (char* previousTableContentBytes = previousTableContent)
                fixed (char* newTableContentBytes = newTableContent)
                {
                    EventSource.EventData* data = stackalloc EventSource.EventData[9];
                    data[0].DataPointer = (IntPtr)(&nodeHashCode);
                    data[0].Size = 4;
                    data[1].DataPointer = (IntPtr)nameBytes;
                    data[1].Size = ((name.Length + 1) * 2);
                    data[2].DataPointer = (IntPtr)tableTypeBytes;
                    data[2].Size = ((tableType.Length + 1) * 2);
                    data[3].DataPointer = (IntPtr)(&previousTable);
                    data[3].Size = 4;
                    data[4].DataPointer = (IntPtr)previousTableContentBytes;
                    data[4].Size = ((previousTableContent.Length + 1) * 2);
                    data[5].DataPointer = (IntPtr)(&newTable);
                    data[5].Size = 4;
                    data[6].DataPointer = (IntPtr)newTableContentBytes;
                    data[6].Size = ((newTableContent.Length + 1) * 2);
                    data[7].DataPointer = (IntPtr)(&input1);
                    data[7].Size = 4;
                    data[8].DataPointer = (IntPtr)(&input2);
                    data[8].Size = 4;
                    WriteEventCore(6, 9, data);
                }
            }
        }
    }
}
