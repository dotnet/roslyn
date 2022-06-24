// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using PrepareTests;
using TypeInfo = PrepareTests.TypeInfo;

namespace RunTests
{

    /// <summary>
    /// Defines a single work item to run.  The work item may contain tests from multiple assemblies
    /// that should be run together in the same work item.
    /// </summary>
    internal readonly record struct WorkItemInfo(ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<TypeInfo>> TypesToTest, int PartitionIndex)
    {
        internal string DisplayName
        {
            get
            {
                var assembliesString = string.Join("_", TypesToTest.Keys.Select(assembly => Path.GetFileNameWithoutExtension(assembly.AssemblyName).Replace(".", string.Empty)));
                return $"{assembliesString}_{PartitionIndex}";
            }
        }

        internal static WorkItemInfo CreateFullAssembly(AssemblyInfo assembly, int partitionIndex)
            => new(ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<TypeInfo>>.Empty.Add(assembly, ImmutableArray<TypeInfo>.Empty), partitionIndex);
    }

    internal sealed class AssemblyScheduler
    {
        /// <summary>
        /// Default number of methods to include per partition.
        /// </summary>
        internal const int DefaultMethodLimit = 2000;

        private readonly Options _options;

        internal AssemblyScheduler(Options options)
        {
            _options = options;
        }

        public ImmutableArray<WorkItemInfo> Schedule(ImmutableArray<AssemblyInfo> assemblies)
        {
            Logger.Log($"Scheduling {assemblies.Length} assemblies");
            if (_options.Sequential)
            {
                Logger.Log("Building sequential work items");
                // return individual work items per assembly that contain all the tests in that assembly.
                return assemblies
                    .Select(WorkItemInfo.CreateFullAssembly)
                    .ToImmutableArray();
            }

            var orderedTypeInfos = assemblies.ToImmutableSortedDictionary(assembly => assembly, GetTestData);

            var testsPerPartition = DefaultMethodLimit;
            var totalTestCount = orderedTypeInfos.Values.SelectMany(type => type).Sum(type => type.TestCount);

            // We've calculated the optimal number of helix partitions based on the queuing time and test run times.
            // Currently that number is 56 - TODO fill out details of computing this number.
            var partitionCount = 56;

            // We then determine how many tests we need to run in each partition based on the total number of tests to run.
            if (_options.UseHelix)
            {
                testsPerPartition = totalTestCount / partitionCount;
            }

            ConsoleUtil.WriteLine($"Found {totalTestCount} tests to run, building {partitionCount} partitions with {testsPerPartition} each");

            // Build work items by adding tests from each type until we hit the limit of tests for each partition.
            // This won't always result in exactly the desired number of partitions with exactly the same number of tests - this is because
            // we only filter by type name in arguments to dotnet test to avoid command length errors.
            //
            // While we do our best to run tests from the same assembly together (by building work items in assembly order) it is expected
            // that some work items will run tests from multiple assemblies due to large variances in the number of tests per assembly.
            var workItems = BuildWorkItems(orderedTypeInfos, testsPerPartition);
            LogWorkItems(workItems);
            return workItems;
        }

        private static ImmutableArray<TypeInfo> GetTestData(AssemblyInfo assembly)
        {
            var testDataFile = ListTests.GetTestDataFilePath(assembly);
            Logger.Log($"Reading assembly test data for {assembly.AssemblyName} from {testDataFile}");

            using var readStream = File.OpenRead(testDataFile);
            var testData = JsonSerializer.Deserialize<ImmutableArray<TypeInfo>>(readStream);
            return testData;
        }

        private static ImmutableArray<WorkItemInfo> BuildWorkItems(ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<TypeInfo>> typeInfos, int methodLimit)
        {
            var workItems = new List<WorkItemInfo>();

            var currentClassNameLengthSum = 0;
            var currentTestCount = 0;
            var workItemIndex = 0;

            var currentAssemblies = new Dictionary<AssemblyInfo, List<TypeInfo>>();

            // Iterate through each assembly and type and build up the work items to run.
            // We add types from assemblies 1 by 1 until we hit the work item test limits / command line length limits.
            foreach (var (assembly, types) in typeInfos)
            {
                Logger.Log($"Building commands for {assembly.AssemblyName} with {types.Length} types and {types.Sum(type => type.TestCount)} tests");
                foreach (var type in types)
                {
                    if (type.TestCount + currentTestCount >= methodLimit)
                    {
                        // Adding this type would put us over the method limit for this partition.
                        // Add our accumulated types and assemblies and end the work item
                        AddWorkItem(currentAssemblies);
                        currentAssemblies = new Dictionary<AssemblyInfo, List<TypeInfo>>();
                    }
                    else if (currentClassNameLengthSum > 25000)
                    {
                        // One item we have to consider here is the maximum command line length in 
                        // Windows which is 32767 characters (XP is smaller but don't care).
                        // Once we get close we start a new work item.
                        AddWorkItem(currentAssemblies);
                        currentAssemblies = new Dictionary<AssemblyInfo, List<TypeInfo>>();
                    }

                    AddType(currentAssemblies, assembly, type);
                }
            }

            // Add any remaining tests to the work item.
            AddWorkItem(currentAssemblies);
            return workItems.ToImmutableArray();

            void AddWorkItem(Dictionary<AssemblyInfo, List<TypeInfo>> typesToTest)
            {
                if (typesToTest.Any())
                {
                    workItems.Add(new WorkItemInfo(typesToTest.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray()), workItemIndex));
                }

                currentTestCount = 0;
                currentClassNameLengthSum = 0;
                workItemIndex++;
            }

            void AddType(Dictionary<AssemblyInfo, List<TypeInfo>> dictionary, AssemblyInfo assemblyInfo, TypeInfo typeInfo)
            {
                var list = dictionary.TryGetValue(assemblyInfo, out var result) ? result : new List<TypeInfo>();
                list.Add(typeInfo);
                dictionary[assemblyInfo] = list;

                currentTestCount += typeInfo.TestCount;
                currentClassNameLengthSum += typeInfo.Name.Length;
            }
        }


        private static void LogWorkItems(ImmutableArray<WorkItemInfo> workItems)
        {
            Logger.Log("==== Work Item List ====");
            foreach (var workItem in workItems)
            {
                Logger.Log($"- Work Item ({workItem.TypesToTest.Values.SelectMany(w => w).Sum(assembly => assembly.TestCount)} tests)");
                foreach (var assembly in workItem.TypesToTest)
                {
                    Logger.Log($"    - {assembly.Key.AssemblyName} with {assembly.Value.Sum(type => type.TestCount)} tests");
                }
            }
        }
    }
}
