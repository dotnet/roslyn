// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;

namespace RunTests
{
    internal record struct WorkItemInfo(ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<ITestFilter>> Filters, int PartitionIndex)
    {
        internal string DisplayName => $"{string.Join("_", Filters.Keys.Select(a => Path.GetFileNameWithoutExtension(a.AssemblyName)))}_{PartitionIndex}";
    }

    internal interface ITestFilter
    {
        internal string GetFilterString();

        internal TimeSpan GetExecutionTime();
    }

    internal record struct AssemblyTestFilter(ImmutableArray<TypeInfo> TypesInAssembly) : ITestFilter
    {
        TimeSpan ITestFilter.GetExecutionTime() => TimeSpan.FromMilliseconds(TypesInAssembly.SelectMany(type => type.Tests).Sum(test => test.ExecutionTime.TotalMilliseconds));

        /// <summary>
        /// TODO - NOT FINE IF THERE ARE OTHER FILTERS - 
        /// </summary>
        /// <returns></returns>
        string ITestFilter.GetFilterString() => string.Empty;
    }

    internal record struct TypeTestFilter(TypeInfo Type) : ITestFilter
    {
        TimeSpan ITestFilter.GetExecutionTime() => TimeSpan.FromMilliseconds(Type.Tests.Sum(test => test.ExecutionTime.TotalMilliseconds));

        string ITestFilter.GetFilterString()
        {
            // https://docs.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=mstest#syntax
            // We want to avoid matching other test classes whose names are prefixed with this test class's name.
            // For example, avoid running 'AttributeTests_WellKnownMember', when the request here is to run 'AttributeTests'.
            // We append a '.', assuming that all test methods in the class *will* match it, but not methods in other classes.
            return $"{Type.Name}.";
        }
    }

    internal record struct MethodTestFilter(TestMethodInfo Test) : ITestFilter
    {
        TimeSpan ITestFilter.GetExecutionTime() => Test.ExecutionTime;

        string ITestFilter.GetFilterString() => Test.FullyQualifiedName;
    }

    internal sealed class AssemblyScheduler
    {
        /// <summary>
        /// Our execution time limit is 3 minutes.  We really want to run tests under 5 minutes, but we need to limit test execution time
        /// to 3 minutes to account for overhead elsewhere in setting up the test run, for example
        ///   1.  Test discovery.
        ///   2.  Downloading assets to the helix machine.
        ///   3.  Setting up the test host for each assembly.
        /// </summary>
        private static readonly TimeSpan s_maxExecutionTime = TimeSpan.FromMinutes(3);

        private readonly Options _options;

        internal AssemblyScheduler(Options options)
        {
            _options = options;
        }

        public async Task<ImmutableArray<WorkItemInfo>> ScheduleAsync(ImmutableArray<AssemblyInfo> assemblies, CancellationToken cancellationToken)
        {
            Logger.Log($"Scheduling {assemblies.Length} assemblies");

            if (_options.Sequential || !_options.UseHelix)
            {
                Logger.Log("Building work items with one assembly each.");
                // return individual work items per assembly that contain all the tests in that assembly.
                return CreateWorkItemsForFullAssemblies(assemblies);
            }

            var orderedTypeInfos = assemblies.ToImmutableSortedDictionary(assembly => assembly, GetTypeInfoList);

            ConsoleUtil.WriteLine($"Found {orderedTypeInfos.Values.SelectMany(t => t).SelectMany(t => t.Tests).Count()} tests to run in {orderedTypeInfos.Keys.Count()} assemblies");

            // Retrieve test runtimes from azure devops historical data.
            var testHistory = await TestHistoryManager.GetTestHistoryAsync(cancellationToken);
            if (testHistory.IsEmpty)
            {
                // We didn't have any test history from azure devops, just partition by assembly.
                ConsoleUtil.WriteLine($"##[warning]Could not look up test history - building a single work item per assembly");
                return CreateWorkItemsForFullAssemblies(assemblies);
            }

            // Now for our current set of test methods we got from the assemblies we built, match them to tests from our test run history
            // so that we can extract an estimate of the test execution time for each test.
            orderedTypeInfos = UpdateTestsWithExecutionTimes(orderedTypeInfos, testHistory);

            // Create work items by partitioning tests by historical execution time with the goal of running under our time limit.
            // While we do our best to run tests from the same assembly together (by building work items in assembly order) it is expected
            // that some work items will run tests from multiple assemblies due to large variances in test execution time.
            var workItems = BuildWorkItems(orderedTypeInfos, s_maxExecutionTime);

            ConsoleUtil.WriteLine($"Built {workItems.Length} work items");
            LogWorkItems(workItems);
            return workItems;

            static ImmutableArray<WorkItemInfo> CreateWorkItemsForFullAssemblies(ImmutableArray<AssemblyInfo> assemblies)
            {
                var workItems = new List<WorkItemInfo>();
                var partitionIndex = 0;
                foreach (var assembly in assemblies)
                {
                    var currentWorkItem = ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<ITestFilter>>.Empty.Add(assembly, ImmutableArray<ITestFilter>.Empty);
                    workItems.Add(new WorkItemInfo(currentWorkItem, partitionIndex++));
                }

                return workItems.ToImmutableArray();
            }
        }

        private static ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<TypeInfo>> UpdateTestsWithExecutionTimes(
            ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<TypeInfo>> assemblyTypes,
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

            LogResults();
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
                // This can happen if our SRM heuristic incorrectly counted a normal method as a test method (which it can do often).
                unmatchedLocalTests.Add(methodInfo.FullyQualifiedName);
                return methodInfo with { ExecutionTime = averageExecutionTime };
            }

            void LogResults()
            {
                foreach (var unmatchedLocalTest in unmatchedLocalTests)
                {
                    Logger.Log($"Could not find test execution history for test {unmatchedLocalTest}");
                }

                var unmatchedRemoteTests = testHistory.Keys.Where(type => !matchedRemoteTests.Contains(type));
                foreach (var unmatchedRemoteTest in unmatchedRemoteTests)
                {
                    Logger.Log($"Found historical data for test {unmatchedRemoteTest} that was not present in local assemblies");
                }

                var allTests = assemblyTypes.Values.SelectMany(v => v).SelectMany(v => v.Tests).Select(t => t.FullyQualifiedName).ToList();

                var totalExpectedRunTime = TimeSpan.FromMilliseconds(updated.Values.SelectMany(types => types).SelectMany(type => type.Tests).Sum(test => test.ExecutionTime.TotalMilliseconds));
                ConsoleUtil.WriteLine($"{unmatchedLocalTests.Count} tests were missing historical data.  {unmatchedRemoteTests.Count()} tests were missing in local assemblies.  Estimate of total execution time for tests is {totalExpectedRunTime}.");
            }
        }

        private static ImmutableArray<WorkItemInfo> BuildWorkItems(ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<TypeInfo>> typeInfos, TimeSpan executionTimeLimit)
        {
            var workItems = new List<WorkItemInfo>();

            // Keep track of which work item we are creating - used to identify work items in names.
            var workItemIndex = 0;

            // Keep track of the execution time of the current work item we are adding to.
            var currentExecutionTime = TimeSpan.Zero;

            // Keep track of the types we're planning to add to the current work item.
            var currentFilters = new SortedDictionary<AssemblyInfo, List<ITestFilter>>();

            // Iterate through each assembly and type and build up the work items to run.
            // We add types from assemblies one by one until we hit our execution time limit,
            // at which point we create a new work item with the current types and start a new one.
            foreach (var (assembly, types) in typeInfos)
            {
                // See if we can just add all types from this assembly to the current work item without going over our execution time limit.
                var executionTimeForAllTypesInAssembly = TimeSpan.FromMilliseconds(types.SelectMany(type => type.Tests).Sum(t => t.ExecutionTime.TotalMilliseconds));
                if (executionTimeForAllTypesInAssembly + currentExecutionTime >= executionTimeLimit)
                {
                    // We can't add every type - go type by type to add what we can and end the work item where we need to.
                    foreach (var type in types)
                    {
                        // See if we can add every test in this type to the current work item without going over our execution time limit.
                        var executionTimeForAllTestsInType = TimeSpan.FromMilliseconds(type.Tests.Sum(method => method.ExecutionTime.TotalMilliseconds));
                        if (executionTimeForAllTestsInType + currentExecutionTime >= executionTimeLimit)
                        {
                            // We can't add every test, go test by test to add what we can and end the work item when we hit the limit.
                            foreach (var test in type.Tests)
                            {
                                if (test.ExecutionTime + currentExecutionTime >= executionTimeLimit)
                                {
                                    // Adding this type would put us over the time limit for this partition.
                                    // Add the current work item to our list and start a new one.
                                    AddCurrentWorkItem();
                                }

                                // Update the current group in the work item with this new type.
                                AddFilter(assembly, new MethodTestFilter(test));
                                currentExecutionTime += test.ExecutionTime;
                            }
                        }
                        else
                        {
                            // All the tests in this type can be safely added to the current work item.
                            // Add them and update our work item execution time with the total execution time of tests in the type.
                            AddFilter(assembly, new TypeTestFilter(type));
                            currentExecutionTime += executionTimeForAllTestsInType;
                        }
                    }
                }
                else
                {
                    // All the types in this assembly can safely be added to the current work item.
                    // Add them and update our work item execution time with the total execution time of tests in the assembly.
                    AddFilter(assembly, new AssemblyTestFilter(types));
                    currentExecutionTime += executionTimeForAllTypesInAssembly;
                }
            }

            // Add any remaining tests to the work item.
            AddCurrentWorkItem();
            return workItems.ToImmutableArray();

            void AddCurrentWorkItem()
            {
                if (currentFilters.Any())
                {
                    workItems.Add(new WorkItemInfo(currentFilters.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray()), workItemIndex));
                    workItemIndex++;
                }

                currentFilters = new();
                currentExecutionTime = TimeSpan.Zero;
            }

            void AddFilter(AssemblyInfo assembly, ITestFilter filter)
            {
                if (currentFilters.TryGetValue(assembly, out var assemblyFilters))
                {
                    assemblyFilters.Add(filter);
                }
                else
                {
                    var filterList = new List<ITestFilter>();
                    filterList.Add(filter);
                    currentFilters.Add(assembly, filterList);
                }
            }
        }


        private static void LogWorkItems(ImmutableArray<WorkItemInfo> workItems)
        {
            Logger.Log("==== Work Item List ====");
            foreach (var workItem in workItems)
            {
                var totalExecutionTime = TimeSpan.FromMilliseconds(workItem.Filters.Values.SelectMany(f => f).Sum(f => f.GetExecutionTime().TotalMilliseconds));
                Logger.Log($"- Work Item {workItem.PartitionIndex} (Execution time {totalExecutionTime})");
                if (totalExecutionTime > s_maxExecutionTime)
                {
                    ConsoleUtil.WriteLine($"##[warning]Work item {workItem.PartitionIndex} estimated execution {totalExecutionTime} time exceeds max execution time {s_maxExecutionTime}.  See runtests.log for details.");
                }

                foreach (var assembly in workItem.Filters)
                {
                    var assemblyRuntime = TimeSpan.FromMilliseconds(assembly.Value.Sum(f => f.GetExecutionTime().TotalMilliseconds));
                    Logger.Log($"    - {assembly.Key.AssemblyName} with execution time {assemblyRuntime}");

                    var typeFilters = assembly.Value.Where(f => f is TypeTestFilter);
                    if (typeFilters.Count() > 0)
                    {
                        Logger.Log($"        - {typeFilters.Count()} types: {string.Join(",", typeFilters)}");
                    }

                    var testFilters = assembly.Value.Where(f => f is MethodTestFilter);
                    if (testFilters.Count() > 0)
                    {
                        Logger.Log($"        - {testFilters.Count()} tests: {string.Join(",", testFilters)}");
                    }
                }
            }
        }

        private static ImmutableArray<TypeInfo> GetTypeInfoList(AssemblyInfo assemblyInfo)
        {
            var assemblyDirectory = Path.GetDirectoryName(assemblyInfo.AssemblyPath);
            var testListPath = Path.Combine(assemblyDirectory!, "testlist.json");
            Contract.Assert(File.Exists(testListPath));

            var deserialized = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(testListPath));

            Contract.Assert(deserialized != null);
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
    }
}
