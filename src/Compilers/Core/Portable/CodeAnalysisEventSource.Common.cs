// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.CodeAnalysis
{
    internal sealed partial class CodeAnalysisEventSource : EventSource
    {
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
            public const EventTask Compilation = (EventTask)4;
            public const EventTask AnalyzerAssemblyLoader = (EventTask)5;
        }

        private CodeAnalysisEventSource() { }

        [Event(1, Keywords = Keywords.Performance, Level = EventLevel.Informational, Opcode = EventOpcode.Start, Task = Tasks.GeneratorDriverRunTime)]
        internal void StartGeneratorDriverRunTime(string id) => WriteEvent(1, id);

        [Event(2, Message = "Generators ran for {0} ticks", Keywords = Keywords.Performance, Level = EventLevel.Informational, Opcode = EventOpcode.Stop, Task = Tasks.GeneratorDriverRunTime)]
        internal void StopGeneratorDriverRunTime(long elapsedTicks, string id) => WriteEvent(2, elapsedTicks, id);

        [Event(3, Keywords = Keywords.Performance, Level = EventLevel.Informational, Opcode = EventOpcode.Start, Task = Tasks.SingleGeneratorRunTime)]
        internal void StartSingleGeneratorRunTime(string generatorName, string assemblyPath, string id) => WriteEvent(3, generatorName, assemblyPath, id);

        [Event(4, Message = "Generator {0} ran for {2} ticks", Keywords = Keywords.Performance, Level = EventLevel.Informational, Opcode = EventOpcode.Stop, Task = Tasks.SingleGeneratorRunTime)]
        internal unsafe void StopSingleGeneratorRunTime(string generatorName, string assemblyPath, long elapsedTicks, string id)
        {
            if (IsEnabled())
            {
                fixed (char* generatorNameBytes = generatorName)
                fixed (char* assemblyPathBytes = assemblyPath)
                fixed (char* idBytes = id)
                {
                    Span<EventData> data = stackalloc EventData[]
                    {
                        GetEventDataForString(generatorName, generatorNameBytes),
                        GetEventDataForString(assemblyPath, assemblyPathBytes),
                        GetEventDataForInt64(&elapsedTicks),
                        GetEventDataForString(id, idBytes),
                    };

                    fixed (EventSource.EventData* dataPtr = data)
                    {
                        WriteEventCore(eventId: 4, data.Length, dataPtr);
                    }
                }
            }
        }

        [Event(5, Message = "Generator '{0}' failed with exception: {1}", Level = EventLevel.Error)]
        internal void GeneratorException(string generatorName, string exception) => WriteEvent(5, generatorName, exception);

        [Event(6, Message = "Node {0} transformed", Keywords = Keywords.Correctness, Level = EventLevel.Verbose, Task = Tasks.BuildStateTable)]
        internal unsafe void NodeTransform(int nodeHashCode, string name, string tableType, int previousTable, string previousTableContent, int newTable, string newTableContent, int input1, int input2)
        {
            if (IsEnabled())
            {
                fixed (char* nameBytes = name)
                fixed (char* tableTypeBytes = tableType)
                fixed (char* previousTableContentBytes = previousTableContent)
                fixed (char* newTableContentBytes = newTableContent)
                {
                    Span<EventData> data = stackalloc EventData[]
                    {
                        GetEventDataForInt32(&nodeHashCode),
                        GetEventDataForString(name, nameBytes),
                        GetEventDataForString(tableType, tableTypeBytes),
                        GetEventDataForInt32(&previousTable),
                        GetEventDataForString(previousTableContent, previousTableContentBytes),
                        GetEventDataForInt32(&newTable),
                        GetEventDataForString(newTableContent, newTableContentBytes),
                        GetEventDataForInt32(&input1),
                        GetEventDataForInt32(&input2),
                    };

                    fixed (EventSource.EventData* dataPtr = data)
                    {
                        WriteEventCore(eventId: 6, data.Length, dataPtr);
                    }
                }
            }
        }

        [Event(7, Message = "Server compilation {0} started", Keywords = Keywords.Performance, Level = EventLevel.Informational, Opcode = EventOpcode.Start, Task = Tasks.Compilation)]
        internal void StartServerCompilation(string name) => WriteEvent(7, name);

        [Event(8, Message = "Server compilation {0} completed", Keywords = Keywords.Performance, Level = EventLevel.Informational, Opcode = EventOpcode.Stop, Task = Tasks.Compilation)]
        internal void StopServerCompilation(string name) => WriteEvent(8, name);

        [Event(9, Message = "ALC for directory '{0}' created", Keywords = Keywords.Performance, Level = EventLevel.Informational, Opcode = EventOpcode.Start, Task = Tasks.AnalyzerAssemblyLoader)]
        internal void CreateAssemblyLoadContext(string directory) => WriteEvent(9, directory);

        [Event(10, Message = "ALC for directory '{0}' disposed", Keywords = Keywords.Performance, Level = EventLevel.Informational, Opcode = EventOpcode.Stop, Task = Tasks.AnalyzerAssemblyLoader)]
        internal void DisposeAssemblyLoadContext(string directory) => WriteEvent(10, directory);

        [Event(11, Message = "ALC for directory '{0}' disposal failed with exception '{1}'", Keywords = Keywords.Performance, Level = EventLevel.Error, Opcode = EventOpcode.Stop, Task = Tasks.AnalyzerAssemblyLoader)]
        internal void DisposeAssemblyLoadContextException(string directory, string errorMessage) => WriteEvent(11, directory, errorMessage);

        [Event(12, Message = "CreateNonLockingLoader", Keywords = Keywords.Performance, Level = EventLevel.Informational, Task = Tasks.AnalyzerAssemblyLoader)]
        internal void CreateNonLockingLoader(string directory) => WriteEvent(12, directory);

        private static unsafe EventData GetEventDataForString(string value, char* ptr)
        {
            fixed (char* ptr2 = value)
            {
                if (ptr2 != ptr)
                    throw new ArgumentException("Pinned value must match string.");
            }

            return new EventData()
            {
                DataPointer = (IntPtr)ptr,
                Size = (value.Length + 1) * sizeof(char),
            };
        }

        private static unsafe EventData GetEventDataForInt32(int* ptr)
        {
            return new EventData()
            {
                DataPointer = (IntPtr)ptr,
                Size = sizeof(int),
            };
        }

        private static unsafe EventData GetEventDataForInt64(long* ptr)
        {
            return new EventData()
            {
                DataPointer = (IntPtr)ptr,
                Size = sizeof(long),
            };
        }
    }
}
