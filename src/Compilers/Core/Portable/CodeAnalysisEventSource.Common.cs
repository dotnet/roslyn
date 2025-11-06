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
            public const EventKeywords Performance = (EventKeywords)0b001;
            public const EventKeywords Correctness = (EventKeywords)0b010;
            public const EventKeywords AnalyzerLoading = (EventKeywords)0b100;
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
        internal unsafe void StopSingleGeneratorRunTime(string generatorName, string trackingName, string assemblyPath, long elapsedTicks, string id)
        {
            if (IsEnabled())
            {
                fixed (char* generatorNameBytes = generatorName)
                fixed (char* trackingNameBytes = trackingName)
                fixed (char* assemblyPathBytes = assemblyPath)
                fixed (char* idBytes = id)
                {
                    Span<EventData> data =
                    [
                        GetEventDataForString(generatorName, generatorNameBytes),
                        GetEventDataForString(trackingName, trackingNameBytes),
                        GetEventDataForString(assemblyPath, assemblyPathBytes),
                        GetEventDataForInt64(&elapsedTicks),
                        GetEventDataForString(id, idBytes),
                    ];

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

        [Event(9, Message = "ALC for directory '{0}' created", Keywords = Keywords.AnalyzerLoading, Level = EventLevel.Informational, Opcode = EventOpcode.Start, Task = Tasks.AnalyzerAssemblyLoader)]
        internal void CreateAssemblyLoadContext(string directory, string? alc) => WriteEvent(9, directory, alc);

        [Event(10, Message = "ALC for directory '{0}' disposed", Keywords = Keywords.AnalyzerLoading, Level = EventLevel.Informational, Opcode = EventOpcode.Stop, Task = Tasks.AnalyzerAssemblyLoader)]
        internal void DisposeAssemblyLoadContext(string directory, string? alc) => WriteEvent(10, directory, alc);

        [Event(11, Message = "ALC for directory '{0}' disposal failed with exception '{1}'", Keywords = Keywords.AnalyzerLoading, Level = EventLevel.Error, Opcode = EventOpcode.Stop, Task = Tasks.AnalyzerAssemblyLoader)]
        internal void DisposeAssemblyLoadContextException(string directory, string errorMessage, string? alc) => WriteEvent(11, directory, errorMessage, alc);

        [Event(12, Message = "CreateNonLockingLoader", Keywords = Keywords.AnalyzerLoading, Level = EventLevel.Informational, Task = Tasks.AnalyzerAssemblyLoader)]
        internal void CreateNonLockingLoader(string directory) => WriteEvent(12, directory);

        [Event(13, Message = "Request add Analyzer reference '{0}' to project '{1}'", Keywords = Keywords.AnalyzerLoading, Level = EventLevel.Informational)]
        internal void AnalyzerReferenceRequestAddToProject(string path, string projectName) => WriteEvent(13, path, projectName);

        [Event(14, Message = "Analyzer reference '{0}' was added to project '{1}'", Keywords = Keywords.AnalyzerLoading, Level = EventLevel.Informational)]
        internal void AnalyzerReferenceAddedToProject(string path, string projectName) => WriteEvent(14, path, projectName);

        [Event(15, Message = "Request remove Analyzer reference '{0}' from project '{1}'", Keywords = Keywords.AnalyzerLoading, Level = EventLevel.Informational)]
        internal void AnalyzerReferenceRequestRemoveFromProject(string path, string projectName) => WriteEvent(15, path, projectName);

        [Event(16, Message = "Analyzer reference '{0}' was removed from project '{1}'", Keywords = Keywords.AnalyzerLoading, Level = EventLevel.Informational)]
        internal void AnalyzerReferenceRemovedFromProject(string path, string projectName) => WriteEvent(16, path, projectName);

        [Event(17, Message = "Analyzer reference was redirected by '{0}' from '{1}' to '{2}' for project '{3}'", Keywords = Keywords.AnalyzerLoading, Level = EventLevel.Informational)]
        internal unsafe void AnanlyzerReferenceRedirected(string redirectorType, string originalPath, string newPath, string project)
        {
            if (IsEnabled())
            {
                fixed (char* redirectorTypeBytes = redirectorType)
                fixed (char* originalPathBytes = originalPath)
                fixed (char* newPathBytes = newPath)
                fixed (char* projectBytes = project)
                {
                    Span<EventData> data =
                    [
                        GetEventDataForString(redirectorType, redirectorTypeBytes),
                        GetEventDataForString(originalPath, originalPathBytes),
                        GetEventDataForString(newPath, newPathBytes),
                        GetEventDataForString(project, projectBytes),
                    ];

                    fixed (EventData* dataPtr = data)
                    {
                        WriteEventCore(eventId: 17, data.Length, dataPtr);
                    }
                }
            }
        }

        [Event(18, Message = "ALC for directory '{0}': Assembly '{1}' was resolved by '{2}' ", Keywords = Keywords.AnalyzerLoading, Level = EventLevel.Informational)]
        internal unsafe void ResolvedAssembly(string directory, string assemblyName, string resolver, string filePath, string alc)
        {
            if (IsEnabled())
            {
                fixed (char* directoryBytes = directory)
                fixed (char* assemblyNameBytes = assemblyName)
                fixed (char* resolverBytes = resolver)
                fixed (char* filePathBytes = filePath)
                fixed (char* alcBytes = alc)
                {
                    Span<EventData> data =
                    [
                        GetEventDataForString(directory, directoryBytes),
                        GetEventDataForString(assemblyName, assemblyNameBytes),
                        GetEventDataForString(resolver, resolverBytes),
                        GetEventDataForString(filePath, filePathBytes),
                        GetEventDataForString(alc, alcBytes),
                    ];

                    fixed (EventData* dataPtr = data)
                    {
                        WriteEventCore(eventId: 18, data.Length, dataPtr);
                    }
                }
            }
        }

        [Event(19, Message = "ALC for directory '{0}': Failed to resolve assembly '{1}' ", Keywords = Keywords.AnalyzerLoading, Level = EventLevel.Informational)]
        internal unsafe void ResolveAssemblyFailed(string directory, string assemblyName) => WriteEvent(19, directory, assemblyName);

        [Event(20, Message = "Project '{0}' created with file path '{1}'", Level = EventLevel.Informational)]
        internal void ProjectCreated(string projectSystemName, string? filePath) => WriteEvent(20, projectSystemName, filePath ?? string.Empty);

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
