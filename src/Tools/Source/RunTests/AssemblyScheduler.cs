// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
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
                var workItemsByMethodCount = BuildWorkItems(
                    orderedTypeInfos,
                    getWeightFunc: static test => 1,
                    limit: s_maxMethodCount);

                LogWorkItems(workItemsByMethodCount);
                return workItemsByMethodCount;
            }

            LogLongTests(testHistory);

            // Now for our current set of test methods we got from the assemblies we built, match them to tests from our test run history
            // so that we can extract an estimate of the test execution time for each test.
            orderedTypeInfos = UpdateTestsWithExecutionTimes(orderedTypeInfos, testHistory);

            // Create work items by partitioning tests by historical execution time with the goal of running under our time limit.
            // While we do our best to run tests from the same assembly together (by building work items in assembly order) it is expected
            // that some work items will run tests from multiple assemblies due to large variances in test execution time.
            var workItems = BuildWorkItems(
                orderedTypeInfos,
                getWeightFunc: static test => test.ExecutionTime.TotalSeconds,
                limit: s_maxExecutionTime.TotalSeconds);
            LogWorkItems(workItems);
            return workItems;
        }

        private static void LogLongTests(ImmutableDictionary<string, TimeSpan> testHistory)
        {
            var longTests = testHistory
                .Where(kvp => kvp.Value > s_maxExecutionTime)
                .OrderBy(kvp => kvp.Key)
                .ToList();
            if (longTests.Count > 0)
            {
                ConsoleUtil.Warning($"There are {longTests.Count} tests have execution times greater than the maximum execution time of {s_maxExecutionTime}");
                foreach (var (test, time) in longTests)
                {
                    ConsoleUtil.WriteLine($"\t{test} - {time:hh\\:mm\\:ss}");
                }
            }
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
            Func<TestMethodInfo, TWeight> getWeightFunc,
            TWeight limit)
            where TWeight : struct, INumber<TWeight>
        {
            var workItems = new List<HelixWorkItem>();
            var currentWeight = TWeight.Zero;
            var currentFilters = new List<(string AssemblyFilePath, TestMethodInfo TestMethodInfo)>();

            foreach (var (assemblyFilePath, types) in typeInfos)
            {
                if (ShouldPartitionInSingleWorkItem(assemblyFilePath))
                {
                    AddWorkItem(types.SelectMany(x => x.Tests).Select(x => (assemblyFilePath, x)));
                    continue;
                }

                foreach (var type in types)
                {
                    foreach (var test in type.Tests)
                    {
                        var weight = getWeightFunc(test);

                        // When the single test is greater than the limit, give it a dedicated work item
                        if (weight > limit)
                        {
                            AddWorkItem([(assemblyFilePath, test)]);
                            continue;
                        }

                        currentWeight += weight;

                        // If the accumulated value is greater than the limit then we close off the current
                        // work item and start a new one
                        if (currentWeight > limit)
                        {
                            MaybeAddCurrentWorkItem();
                            currentWeight = weight;
                        }

                        currentFilters.Add((assemblyFilePath, test));
                    }
                }
            }

            MaybeAddCurrentWorkItem();
            return workItems.ToImmutableArray();

            void MaybeAddCurrentWorkItem()
            {
                if (currentFilters.Count > 0)
                {
                    AddWorkItem(currentFilters);
                    currentFilters.Clear();
                    currentWeight = TWeight.Zero;
                }
            }

            void AddWorkItem(params IEnumerable<(string AssemblyFilePath, TestMethodInfo TestMethodInfo)> tests)
            {
                Debug.Assert(tests.Any());
                var assemblyFilePaths = tests
                    .Select(x => x.AssemblyFilePath)
                    .Distinct()
                    .Order()
                    .ToImmutableArray();
                var testMethodNames = tests
                    .Select(x => x.TestMethodInfo.FullyQualifiedName)
                    .ToImmutableArray();
                var executionTime = tests
                    .Sum(x => x.TestMethodInfo.ExecutionTime.TotalSeconds);
                var workItem = new HelixWorkItem(
                    workItems.Count,
                    assemblyFilePaths,
                    testMethodNames,
                    TimeSpan.FromSeconds(executionTime));
                workItems.Add(workItem);
            }
        }

        private static void LogWorkItems(ImmutableArray<HelixWorkItem> workItems)
        {
            ConsoleUtil.WriteLine($"Built {workItems.Length} work items");
            foreach (var workItem in workItems)
            {
                ConsoleUtil.WriteLine($"- Work Item: {workItem.Id} Execution time: {workItem.EstimatedExecutionTime:hh\\:mm\\:ss}");
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
            if (assemblyFilePath != string.Empty)
            {
                return true;
            }

            return false;
        }
    }
}
