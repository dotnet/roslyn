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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace RunTests
{
    internal record struct WorkItemInfo(ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<TestMethodInfo>> Filters, int PartitionIndex)
    {
        internal readonly string DisplayName
        {
            get
            {
                var assembliesString = string.Join("_", Filters.Keys.Select(a => Path.GetFileNameWithoutExtension(a.AssemblyName)));

                // Currently some helix APIs don't work when the work item friendly name is more than 200 characters.
                // Until that is fixed we manually truncate the name ourselves to a reasonable limit.
                // https://github.com/dotnet/arcade/issues/11079
                assembliesString = assembliesString.Length > 150 ? $"{assembliesString[..150]}..." : assembliesString;
                return $"{assembliesString}_{PartitionIndex}";
            }
        }
    }

    internal sealed class AssemblyScheduler
    {
        /// <summary>
        /// We attempt to partition our tests into work items that execute in under 2 minutes 30s.  This is a derived limit based on a goal of running all tests
        /// in under 5 minutes.  However because of overhead in setting up the test run, e.g.
        ///   1.  Test discovery.
        ///   2.  Downloading assets to the helix machine.
        ///   3.  Setting up the test host for each assembly.
        ///   
        /// we need to actually build partitions that run in under 5 minutes, hence our limit here of 2m30s.
        /// </summary>
        private static readonly TimeSpan s_maxExecutionTime = TimeSpan.FromSeconds(150);

        /// <summary>
        /// If we were unable to find the test execution history, we fall back to partitioning by just method count.
        /// </summary>
        private static readonly int s_maxMethodCount = 500;

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
            var testHistory = await TestHistoryManager.GetTestHistoryAsync(_options, cancellationToken);
            if (testHistory.IsEmpty)
            {
                // We didn't have any test history from azure devops, just partition by test count.
                ConsoleUtil.Warning($"Could not look up test history - partitioning based on test count instead");
                var workItemsByMethodCount = BuildWorkItems<int>(
                    orderedTypeInfos,
                    isOverLimitFunc: (accumulatedMethodCount) => accumulatedMethodCount >= s_maxMethodCount,
                    addFunc: (currentTest, accumulatedMethodCount) => accumulatedMethodCount + 1);

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
                isOverLimitFunc: (accumulatedExecutionTime) => accumulatedExecutionTime >= s_maxExecutionTime,
                addFunc: (currentTest, accumulatedExecutionTime) => currentTest.ExecutionTime + accumulatedExecutionTime);
            LogWorkItems(workItems);
            return workItems;

            static ImmutableArray<WorkItemInfo> CreateWorkItemsForFullAssemblies(ImmutableArray<AssemblyInfo> assemblies)
            {
                var workItems = new List<WorkItemInfo>();
                var partitionIndex = 0;
                foreach (var assembly in assemblies)
                {
                    var currentWorkItem = ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<TestMethodInfo>>.Empty.Add(assembly, ImmutableArray<TestMethodInfo>.Empty);
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
                // This usually occurs when tests have been added in between the last passing branch run and this PR.
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

        private ImmutableArray<WorkItemInfo> BuildWorkItems<TWeight>(
            ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<TypeInfo>> typeInfos,
            Func<TWeight, bool> isOverLimitFunc,
            Func<TestMethodInfo, TWeight, TWeight> addFunc) where TWeight : struct
        {
            var workItems = new List<WorkItemInfo>();

            // Keep track of which work item we are creating - used to identify work items in names.
            var workItemIndex = 0;

            // Keep track of the limit of the current work item we are adding to.
            var accumulatedValue = default(TWeight);

            // Keep track of the types we're planning to add to the current work item.
            var currentFilters = new SortedDictionary<AssemblyInfo, List<TestMethodInfo>>();

            // First find any assemblies we need to run in single assembly work items (due to state sharing concerns).
            var singlePartitionAssemblies = typeInfos.Where(kvp => ShouldPartitionInSingleWorkItem(kvp.Key.AssemblyPath));
            typeInfos = typeInfos.RemoveRange(singlePartitionAssemblies.Select(kvp => kvp.Key));
            foreach (var (assembly, types) in singlePartitionAssemblies)
            {
                Logger.Log($"Building single assembly work item {workItemIndex} for {assembly.AssemblyPath}");
                types.SelectMany(t => t.Tests).ToList().ForEach(test => AddFilter(assembly, test));

                // End the work item so we don't include anything after this assembly.
                AddCurrentWorkItem();
            }

            // Iterate through each assembly and type and build up the work items to run.
            // We add types from assemblies one by one until we hit our limit,
            // at which point we create a work item with the current types and start a new one.
            foreach (var (assembly, types) in typeInfos)
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
                        AddFilter(assembly, test);
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
                    workItems.Add(new WorkItemInfo(currentFilters.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray()), workItemIndex));
                    workItemIndex++;
                }

                currentFilters.Clear();
                accumulatedValue = default;
            }

            void AddFilter(AssemblyInfo assembly, TestMethodInfo test)
            {
                if (currentFilters.TryGetValue(assembly, out var assemblyFilters))
                {
                    assemblyFilters.Add(test);
                }
                else
                {
                    var filterList = new List<TestMethodInfo>
                    {
                        test
                    };
                    currentFilters.Add(assembly, filterList);
                }

                accumulatedValue = addFunc(test, accumulatedValue);
            }
        }

        private static void LogWorkItems(ImmutableArray<WorkItemInfo> workItems)
        {
            ConsoleUtil.WriteLine($"Built {workItems.Length} work items");
            Logger.Log("==== Work Item List ====");
            foreach (var workItem in workItems)
            {
                var totalExecutionTime = TimeSpan.FromMilliseconds(workItem.Filters.Values.SelectMany(f => f).Sum(f => f.ExecutionTime.TotalMilliseconds));
                Logger.Log($"- Work Item {workItem.PartitionIndex} (Execution time {totalExecutionTime})");
                if (totalExecutionTime > s_maxExecutionTime)
                {
                    // Log a warning to the console with work item details when we were not able to partition in under our limit.
                    // This can happen when a single specific test exceeds our execution time limit.
                    ConsoleUtil.Warning($"Work item {workItem.PartitionIndex} estimated execution {totalExecutionTime} time exceeds max execution time {s_maxExecutionTime}.");
                    LogFilters(workItem, ConsoleUtil.WriteLine);
                }
                else
                {
                    LogFilters(workItem, Logger.Log);
                }
            }

            static void LogFilters(WorkItemInfo workItem, Action<string> logger)
            {
                foreach (var assembly in workItem.Filters)
                {
                    var assemblyRuntime = TimeSpan.FromMilliseconds(assembly.Value.Sum(f => f.ExecutionTime.TotalMilliseconds));
                    logger($"    - {assembly.Key.AssemblyName} with execution time {assemblyRuntime}");
                    var testFilters = assembly.Value;
                    if (testFilters.Length > 0)
                    {
                        logger($"        - {testFilters.Length} tests: {string.Join(",", testFilters.Select(t => t.FullyQualifiedName))}");
                    }
                }
            }
        }

        private static ImmutableArray<TypeInfo> GetTypeInfoList(AssemblyInfo assemblyInfo)
        {
            var assemblyDirectory = Path.GetDirectoryName(assemblyInfo.AssemblyPath);
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
        private static bool ShouldPartitionInSingleWorkItem(string assemblyPath)
        {
            using (var stream = File.OpenRead(assemblyPath))
            using (var peReader = new PEReader(stream))
            {
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
            }

            return false;
        }
    }
}
