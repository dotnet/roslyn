// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace RunTests
{
    internal sealed class AssemblyScheduler
    {
        /// <summary>
        /// We attempt to partition our tests into work items that execute in under 2 minutes 30s.  This is a derived limit based on a goal of running all tests
        /// in under 5 minutes.  However because of overhead in setting up the test run, e.g.
        ///   1.  Test discovery.
        ///   2.  Downloading assets to the helix machine.
        ///   3.  Setting up the test host for each assembly.
        ///   
        /// </summary>
        private static readonly TimeSpan s_maxExecutionTime = TimeSpan.FromSeconds(60 * 10);

        /// <summary>
        /// If we were unable to find the test execution history, we fall back to partitioning by just method count.
        /// </summary>
        private static readonly int s_maxMethodCount = 500;

        public static ImmutableArray<HelixWorkItem> Schedule(
            IEnumerable<string> assemblyFilePaths,
            ImmutableDictionary<string, TimeSpan> testHistory)
        {
            var orderedTypeInfos = assemblyFilePaths.ToImmutableSortedDictionary(x => x, GetTypeInfoList);
            ConsoleUtil.WriteLine($"Scheduling {orderedTypeInfos.Count} assemblies");
            foreach (var kvp in orderedTypeInfos)
            {
                var typeCount = kvp.Value.Length;
                var testCount = kvp.Value.Sum(t => t.Tests.Length);
                ConsoleUtil.WriteLine($"\tAssembly: {Path.GetFileName(kvp.Key)}, Test Type Count: {typeCount}, Test Count: {testCount}");
            }

            if (testHistory.IsEmpty)
            {
                // We didn't have any test history from azure devops, just partition by test count.
                ConsoleUtil.Warning($"Could not look up test history - partitioning based on test count instead");
                var workItemsByMethodCount = BuildWorkItems<int>(
                    orderedTypeInfos,
                    isOverLimitFunc: static (accumulatedMethodCount) => accumulatedMethodCount >= s_maxMethodCount,
                    addFunc: static (currentTest, accumulatedMethodCount) => accumulatedMethodCount + 1);

                LogWorkItems(workItemsByMethodCount);
                return workItemsByMethodCount;
            }

            // Now for our current set of test methods we got from the assemblies we built, match them to tests from our test run history
            // so that we can extract an estimate of the test execution time for each test.
            orderedTypeInfos = UpdateTestsWithExecutionTimes(orderedTypeInfos, testHistory);

            // Create work items by partitioning tests by historical execution time with the goal of running under our time limit.
            // While we do our best to run tests from the same assembly together (by building work items in assembly order) it is expected
            // that some work items will run tests from multiple assemblies due to large variances in test execution time.
            var workItems = BuildWorkItems<TimeSpan>(
                orderedTypeInfos,
                isOverLimitFunc: static (accumulatedExecutionTime) => accumulatedExecutionTime >= s_maxExecutionTime,
                addFunc: static (currentTest, accumulatedExecutionTime) => currentTest.ExecutionTime + accumulatedExecutionTime);
            LogWorkItems(workItems);
            return workItems;
        }

        private static ImmutableSortedDictionary<string, ImmutableArray<TypeInfo>> UpdateTestsWithExecutionTimes(
            ImmutableSortedDictionary<string, ImmutableArray<TypeInfo>> assemblyTypes,
            ImmutableDictionary<string, TimeSpan> testHistory)
        {
            // Determine the average execution time so that we can use it for tests that do not have any history.
            var averageExecutionTime = TimeSpan.FromMilliseconds(testHistory.Values.Average(t => t.TotalMilliseconds));

            // Store the tests we found locally that were missing remote historical data.
            var unmatchedLocalTests = new HashSet<string>();

            // Store the tests we found in the remote historical data so we can report any we didn't find locally.
            var matchedRemoteTests = new HashSet<string>();

            var updated = assemblyTypes.ToImmutableSortedDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(WithTypeExecutionTime).ToImmutableArray());

            WriteResults();
            return updated;

            TypeInfo WithTypeExecutionTime(TypeInfo typeInfo)
            {
                var tests = typeInfo.Tests.Select(WithTestExecutionTime).ToImmutableArray();
                return typeInfo with { Tests = tests };
            }

            TestMethodInfo WithTestExecutionTime(TestMethodInfo methodInfo)
            {
                // Match by fully qualified test method name to azure devops historical data.
                // Note for combinatorial tests, azure devops helpfully groups all sub-runs under a top level method (with combined test run times) with the same fully qualified method name
                // that we get during test discovery.  Since we only filter by the single method name (and not individual combinatorial runs) we do want the combined execution time.
                if (testHistory.TryGetValue(methodInfo.FullyQualifiedName, out var executionTime))
                {
                    matchedRemoteTests.Add(methodInfo.FullyQualifiedName);
                    return methodInfo with { ExecutionTime = executionTime };
                }

                // We didn't find the local type from our assembly in test run historical data.
                // This usually occurs when tests have been added in between the last passing branch run and this PR.
                unmatchedLocalTests.Add(methodInfo.FullyQualifiedName);
                return methodInfo with { ExecutionTime = averageExecutionTime };
            }

            void WriteResults()
            {
                foreach (var unmatchedLocalTest in unmatchedLocalTests)
                {
                    ConsoleUtil.WriteLine($"Could not find test execution history for test {unmatchedLocalTest}");
                }

                var unmatchedRemoteTests = testHistory.Keys.Where(type => !matchedRemoteTests.Contains(type));
                foreach (var unmatchedRemoteTest in unmatchedRemoteTests)
                {
                    ConsoleUtil.WriteLine($"Found historical data for test {unmatchedRemoteTest} that was not present in local assemblies");
                }

                var allTests = assemblyTypes.Values.SelectMany(v => v).SelectMany(v => v.Tests).Select(t => t.FullyQualifiedName).ToList();

                var totalExpectedRunTime = TimeSpan.FromMilliseconds(updated.Values.SelectMany(types => types).SelectMany(type => type.Tests).Sum(test => test.ExecutionTime.TotalMilliseconds));
                ConsoleUtil.WriteLine($"{unmatchedLocalTests.Count} tests were missing historical data.  {unmatchedRemoteTests.Count()} tests were missing in local assemblies.  Estimate of total execution time for tests is {totalExpectedRunTime}.");
            }
        }

        private static ImmutableArray<HelixWorkItem> BuildWorkItems<TWeight>(
            ImmutableSortedDictionary<string, ImmutableArray<TypeInfo>> typeInfos,
            Func<TWeight, bool> isOverLimitFunc,
            Func<TestMethodInfo, TWeight, TWeight> addFunc) where TWeight : struct
        {
            var workItems = new List<HelixWorkItem>();

            // Keep track of the limit of the current work item we are adding to.
            var accumulatedValue = default(TWeight);

            // Keep track of the types we're planning to add to the current work item. The key 
            // is the file path of the assembly
            var currentFilters = new SortedDictionary<string, List<TestMethodInfo>>();

            // First find any assemblies we need to run in single assembly work items (due to state sharing concerns).
            var singlePartitionAssemblies = typeInfos.Where(kvp => ShouldPartitionInSingleWorkItem(kvp.Key));
            typeInfos = typeInfos.RemoveRange(singlePartitionAssemblies.Select(kvp => kvp.Key));
            foreach (var (assemblyFilePaths, types) in singlePartitionAssemblies)
            {
                ConsoleUtil.WriteLine($"Building single assembly work item {workItems.Count} for {assemblyFilePaths}");
                types.SelectMany(t => t.Tests).ToList().ForEach(test => AddFilter(assemblyFilePaths, test));

                // End the work item so we don't include anything after this assembly.
                AddCurrentWorkItem();
            }

            // Iterate through each assembly and type and build up the work items to run.
            // We add types from assemblies one by one until we hit our limit,
            // at which point we create a work item with the current types and start a new one.
            foreach (var (assemblyFilePath, types) in typeInfos)
            {
                foreach (var type in types)
                {
                    foreach (var test in type.Tests)
                    {
                        // Get a new value representing the value from the test plus the accumulated value in the work item.
                        var newAccumulatedValue = addFunc(test, accumulatedValue);

                        // If the new accumulated value is greater than the limit
                        if (isOverLimitFunc(newAccumulatedValue))
                        {
                            // Adding this type would put us over the time limit for this partition.
                            // Add the current work item to our list and start a new one.
                            AddCurrentWorkItem();
                        }

                        // Update the current group in the work item with this new type.
                        AddFilter(assemblyFilePath, test);
                    }
                }
            }

            // Add any remaining tests to the work item.
            AddCurrentWorkItem();
            return workItems.ToImmutableArray();

            void AddCurrentWorkItem()
            {
                if (currentFilters.Any())
                {
                    var e = currentFilters.Values
                        .SelectMany(v => v)
                        .Sum(v => v.ExecutionTime.TotalSeconds);
                    var workItemInfo = new HelixWorkItem(
                        workItems.Count,
                        currentFilters.Keys.ToImmutableArray(),
                        currentFilters.Values.SelectMany(v => v).Select(x => x.FullyQualifiedName).ToImmutableArray(),
                        TimeSpan.FromSeconds(e));
                    workItems.Add(workItemInfo);
                }

                currentFilters.Clear();
                accumulatedValue = default;
            }

            void AddFilter(string assemblyFilePath, TestMethodInfo test)
            {
                if (!currentFilters.TryGetValue(assemblyFilePath, out var assemblyFilters))
                {
                    assemblyFilters = new List<TestMethodInfo>();
                    currentFilters.Add(assemblyFilePath, assemblyFilters);
                }

                assemblyFilters.Add(test);
                accumulatedValue = addFunc(test, accumulatedValue);
            }
        }

        private static void LogWorkItems(ImmutableArray<HelixWorkItem> workItems)
        {
            ConsoleUtil.WriteLine($"Built {workItems.Length} work items");
            ConsoleUtil.WriteLine("==== Work Item List ====");
            foreach (var workItem in workItems)
            {
                ConsoleUtil.WriteLine($"- Work Item {workItem.Id} (Execution time {workItem.EstimatedExecutionTime})");
                if (workItem.EstimatedExecutionTime > s_maxExecutionTime == true)
                {
                    // Log a warning to the console with work item details when we were not able to partition in under our limit.
                    // This can happen when a single specific test exceeds our execution time limit.
                    ConsoleUtil.Warning($"Estimated execution {workItem.EstimatedExecutionTime} time exceeds max execution time {s_maxExecutionTime}.");
                }
            }
        }

        private static ImmutableArray<TypeInfo> GetTypeInfoList(string assemblyFilePath)
        {
            var assemblyDirectory = Path.GetDirectoryName(assemblyFilePath);
            var testListPath = Path.Combine(assemblyDirectory!, "testlist.json");
            if (!File.Exists(testListPath))
            {
                throw new ArgumentException($"{testListPath} does not exist");
            }

            var deserialized = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(testListPath));
            if (deserialized == null)
            {
                throw new InvalidOperationException($"Could not deserialize {testListPath}");
            }

            var tests = deserialized.GroupBy(GetTypeName)
                .Select(group => new TypeInfo(GetName(group.Key), group.Key, group.Select(test => new TestMethodInfo(GetName(test), test, TimeSpan.Zero)).ToImmutableArray()))
                .ToImmutableArray();
            return tests;

            static string GetTypeName(string fullyQualifiedTestName)
            {
                var periodBeforeMethod = fullyQualifiedTestName.LastIndexOf(".");
                return fullyQualifiedTestName[..periodBeforeMethod];
            }

            static string GetName(string fullyQualifiedName)
            {
                var lastPeriod = fullyQualifiedName.LastIndexOf(".");
                return fullyQualifiedName[(lastPeriod + 1)..];
            }
        }

        /// <summary>
        /// Looks for the assembly marker attribute <see cref="RunTestsInSinglePartitionAttribute"/>
        /// that signifies tests in the assembly must be run separately.
        /// </summary>
        private static bool ShouldPartitionInSingleWorkItem(string assemblyFilePath)
        {
            using var stream = File.OpenRead(assemblyFilePath);
            using var peReader = new PEReader(stream);

            var metadataReader = peReader.GetMetadataReader();
            var attributes = metadataReader.GetAssemblyDefinition().GetCustomAttributes();
            foreach (var attributeHandle in attributes)
            {
                var attribute = metadataReader.GetCustomAttribute(attributeHandle);
                if (attribute.Constructor.Kind is HandleKind.MemberReference)
                {
                    var ctor = metadataReader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                    if (ctor.Parent.Kind is HandleKind.TypeReference)
                    {
                        var typeNameHandle = metadataReader.GetTypeReference((TypeReferenceHandle)ctor.Parent).Name;
                        var typeName = metadataReader.GetString(typeNameHandle);
                        if (typeName == nameof(RunTestsInSinglePartitionAttribute))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
