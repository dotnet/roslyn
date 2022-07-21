// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
        TimeSpan ITestFilter.GetExecutionTime()
        {
            return TimeSpan.FromMilliseconds(TypesInAssembly.SelectMany(type => type.Tests).Sum(test => test.ExecutionTime.TotalMilliseconds));
        }

        string ITestFilter.GetFilterString()
        {
            return string.Empty;
        }
    }

    internal record struct TypeTestFilter(TypeInfo Type) : ITestFilter
    {
        TimeSpan ITestFilter.GetExecutionTime()
        {
            return TimeSpan.FromMilliseconds(Type.Tests.Sum(test => test.ExecutionTime.TotalMilliseconds));
        }

        string ITestFilter.GetFilterString()
        {
            // https://docs.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=mstest#syntax
            // We want to avoid matching other test classes whose names are prefixed with this test class's name.
            // For example, avoid running 'AttributeTests_WellKnownMember', when the request here is to run 'AttributeTests'.
            // We append a '.', assuming that all test methods in the class *will* match it, but not methods in other classes.
            return $"{Type.Name}.";
        }
    }

    internal record struct MethodTestFilter(MethodInfo Test) : ITestFilter
    {
        TimeSpan ITestFilter.GetExecutionTime()
        {
            return Test.ExecutionTime;
        }

        string ITestFilter.GetFilterString()
        {
            return Test.FullyQualifiedName;
        }
    }

    internal sealed class AssemblyScheduler
    {
        private readonly Options _options;

        internal AssemblyScheduler(Options options)
        {
            _options = options;
        }

        public async Task<ImmutableArray<WorkItemInfo>> ScheduleAsync(ImmutableArray<AssemblyInfo> assemblies, CancellationToken cancellationToken)
        {
            Logger.Log($"Scheduling {assemblies.Length} assemblies");

            var orderedTypeInfos = assemblies.ToImmutableSortedDictionary(assembly => assembly, GetTypeInfoList);

            ConsoleUtil.WriteLine($"Found {orderedTypeInfos.Values.SelectMany(t => t).SelectMany(t => t.Tests).Count()} tests to run in {orderedTypeInfos.Keys.Count()} assemblies");

            if (_options.Sequential)
            {
                Logger.Log("Building sequential work items");
                // return individual work items per assembly that contain all the tests in that assembly.
                return CreateWorkItemsForFullAssemblies(orderedTypeInfos);
            }

            // Retrieve test runtimes from azure devops historical data.
            var testHistory = await TestHistoryManager.GetTestHistoryAsync(cancellationToken);
            if (testHistory.IsEmpty)
            {
                // We didn't have any test history from azure devops, just partition by assembly.
                return CreateWorkItemsForFullAssemblies(orderedTypeInfos);
            }

            // Now for our current set of test methods we got from the assemblies we built, match them to tests from our test run history
            // so that we can extract an estimate of the test execution time for each test.
            orderedTypeInfos = UpdateTestsWithExecutionTimes(orderedTypeInfos, testHistory);

            // Our execution time limit is 4 minutes.  We really want to run tests under 5 minutes, but we need to limit test execution time
            // to 4 minutes to account for overhead in things like downloading assets, setting up the test host for each assembly, etc.
            var executionTimeLimit = TimeSpan.FromMinutes(4);

            // Create work items by partitioning tests by historical execution time with the goal of running under our time limit.
            // While we do our best to run tests from the same assembly together (by building work items in assembly order) it is expected
            // that some work items will run tests from multiple assemblies due to large variances in test execution time.
            var workItems = BuildWorkItems(orderedTypeInfos, executionTimeLimit);

            //ConsoleUtil.WriteLine($"Built {workItems.Length} work items");
            LogWorkItems(workItems);
            return workItems;

            static ImmutableArray<WorkItemInfo> CreateWorkItemsForFullAssemblies(ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<TypeInfo>> orderedTypeInfos)
            {
                var workItems = new List<WorkItemInfo>();
                var partitionIndex = 0;
                foreach (var orderedTypeInfo in orderedTypeInfos)
                {
                    var currentWorkItem = ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<ITestFilter>>.Empty.Add(orderedTypeInfo.Key, ImmutableArray.Create((ITestFilter)new AssemblyTestFilter(orderedTypeInfo.Value)));
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

            // Store the tests from our assemblies that we couldn't find a history for to log.
            var extraLocalTests = new HashSet<string>();

            // Store the tests that we were able to match to historical data.
            var matchedLocalTests = new HashSet<string>();

            var updated = assemblyTypes.ToImmutableSortedDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(typeInfo => typeInfo with { Tests = typeInfo.Tests.Select(method => WithExecutionTime(method)).ToImmutableArray() })
                                .ToImmutableArray());
            LogResults(matchedLocalTests, extraLocalTests);
            return updated;

            MethodInfo WithExecutionTime(MethodInfo methodInfo)
            {
                // Match by fully qualified test method name to azure devops historical data.
                // Note for combinatorial tests, azure devops helpfully groups all sub-runs under a top level method (with combined test run times) with the same fully qualified method name
                //  that we'll get from looking at all the test methods in the assembly with SRM.
                if (testHistory.TryGetValue(methodInfo.FullyQualifiedName, out var executionTime))
                {
                    matchedLocalTests.Add(methodInfo.FullyQualifiedName);
                    return methodInfo with { ExecutionTime = executionTime };
                }

                // We didn't find the local type from our assembly in test run historical data.
                // This can happen if our SRM heuristic incorrectly counted a normal method as a test method (which it can do often).
                extraLocalTests.Add(methodInfo.FullyQualifiedName);
                return methodInfo with { ExecutionTime = averageExecutionTime };
            }

            void LogResults(HashSet<string> matchedLocalTests, HashSet<string> extraLocalTests)
            {
                foreach (var extraLocalTest in extraLocalTests)
                {
                    Logger.Log($"Could not find test execution history for test {extraLocalTest}");
                }

                var extraRemoteTests = testHistory.Keys.Where(type => !matchedLocalTests.Contains(type));
                foreach (var extraRemoteTest in extraRemoteTests)
                {
                    Logger.Log($"Found historical data for test {extraRemoteTest} that was not present in local assemblies");
                }

                var totalExpectedRunTime = TimeSpan.FromMilliseconds(updated.Values.SelectMany(types => types).SelectMany(type => type.Tests).Sum(test => test.ExecutionTime.TotalMilliseconds));
                ConsoleUtil.WriteLine($"Matched {matchedLocalTests.Count} tests with historical data.  {extraLocalTests.Count} tests were missing historical data.  {extraRemoteTests.Count()} tests were missing in local assemblies.  Estimate of total execution time for tests is {totalExpectedRunTime}.");
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
                var totalRuntime = TimeSpan.FromMilliseconds(workItem.Filters.Values.SelectMany(f => f).Sum(f => f.GetExecutionTime().TotalMilliseconds));
                Logger.Log($"- Work Item (Runtime {totalRuntime})");
                foreach (var assembly in workItem.Filters)
                {
                    var assemblyRuntime = TimeSpan.FromMilliseconds(assembly.Value.Sum(f => f.GetExecutionTime().TotalMilliseconds));
                    Logger.Log($"    - {assembly.Key.AssemblyName} with runtime {assemblyRuntime}");
                    foreach (var filter in assembly.Value)
                    {
                        Logger.Log($"        - {filter} with runtime {filter.GetExecutionTime()}");
                    }
                }
            }
        }

        private static ImmutableArray<TypeInfo> GetTypeInfoList(AssemblyInfo assemblyInfo)
        {
            using (var stream = File.OpenRead(assemblyInfo.AssemblyPath))
            using (var peReader = new PEReader(stream))
            {
                var metadataReader = peReader.GetMetadataReader();
                return GetTypeInfoList(metadataReader);
            }
        }

        private static ImmutableArray<TypeInfo> GetTypeInfoList(MetadataReader reader)
        {
            var list = new List<TypeInfo>();
            foreach (var handle in reader.TypeDefinitions)
            {
                var type = reader.GetTypeDefinition(handle);
                if (!IsValidIdentifier(reader, type.Name))
                {
                    continue;
                }

                var (typeName, fullyQualifiedTypeName) = GetTypeName(reader, type);
                var methodCount = GetMethodCount(reader, type);
                if (!ShouldIncludeType(reader, type, methodCount))
                {
                    continue;
                }

                var methodList = new List<MethodInfo>();
                GetMethods(reader, type, methodList, fullyQualifiedTypeName);

                list.Add(new TypeInfo(typeName, fullyQualifiedTypeName, methodList.ToImmutableArray()));
            }

            // Ensure we get classes back in a deterministic order.
            list.Sort((x, y) => x.FullyQualifiedName.CompareTo(y.FullyQualifiedName));
            return list.ToImmutableArray();
        }

        private static bool IsPublicType(TypeDefinition type)
        {
            // See https://docs.microsoft.com/en-us/dotnet/api/system.reflection.typeattributes?view=net-6.0#examples
            // for extracting this information from the TypeAttributes.
            var visibility = type.Attributes & TypeAttributes.VisibilityMask;
            var isPublic = visibility == TypeAttributes.Public || visibility == TypeAttributes.NestedPublic;
            return isPublic;
        }

        private static bool IsClass(TypeDefinition type)
        {
            var classSemantics = type.Attributes & TypeAttributes.ClassSemanticsMask;
            var isClass = classSemantics == TypeAttributes.Class;
            return isClass;
        }

        private static bool IsAbstract(TypeDefinition type)
        {
            var isAbstract = (type.Attributes & TypeAttributes.Abstract) != 0;
            return isAbstract;
        }

        /// <summary>
        /// Determine if this type should be one of the <c>class</c> values passed to xunit.  This
        /// code doesn't actually resolve base types or trace through inherrited Fact attributes
        /// hence we have to error on the side of including types with no tests vs. excluding them.
        /// </summary>
        private static bool ShouldIncludeType(MetadataReader reader, TypeDefinition type, int testMethodCount)
        {
            // xunit only handles public, non-abstract classes
            if (!IsPublicType(type) || IsAbstract(type) || !IsClass(type))
            {
                return false;
            }

            // Compiler generated types / methods have the shape of the heuristic that we are looking
            // at here.  Filter them out as well.
            if (!IsValidIdentifier(reader, type.Name))
            {
                return false;
            }

            if (testMethodCount > 0)
            {
                return true;
            }

            // The case we still have to consider at this point is a class with 0 defined methods, 
            // inheritting from a class with > 0 defined test methods.  That is a completely valid
            // xunit scenario.  For now we're just going to exclude types that inherit from object
            // or other built-in base types because they clearly don't fit that category.
            return !InheritsFromFrameworkBaseType(reader, type);
        }

        private static void GetMethods(MetadataReader reader, TypeDefinition type, List<MethodInfo> methodList, string originalFullyQualifiedTypeName)
        {
            var methods = type.GetMethods();

            foreach (var methodHandle in methods)
            {
                var method = reader.GetMethodDefinition(methodHandle);
                var methodInfo = GetMethodInfo(reader, method, originalFullyQualifiedTypeName);
                if (ShouldIncludeMethod(reader, method))
                {
                    methodList.Add(methodInfo);
                }
            }

            var baseTypeHandle = type.BaseType;
            if (!baseTypeHandle.IsNil && baseTypeHandle.Kind is HandleKind.TypeDefinition)
            {
                var baseType = reader.GetTypeDefinition((TypeDefinitionHandle)baseTypeHandle);

                // We only want to look for test methods in public base types.
                if (IsPublicType(baseType) && IsClass(baseType) && !InheritsFromFrameworkBaseType(reader, type))
                {
                    GetMethods(reader, baseType, methodList, originalFullyQualifiedTypeName);
                }
            }
        }

        private static bool ShouldIncludeMethod(MetadataReader reader, MethodDefinition method)
        {
            var visibility = method.Attributes & MethodAttributes.MemberAccessMask;
            var isPublic = visibility == MethodAttributes.Public;

            var hasMethodAttributes = method.GetCustomAttributes().Count > 0;

            var isValidIdentifier = IsValidIdentifier(reader, method.Name);

            return isPublic && hasMethodAttributes && isValidIdentifier;
        }

        private static MethodInfo GetMethodInfo(MetadataReader reader, MethodDefinition method, string fullyQualifiedTypeName)
        {
            var methodName = reader.GetString(method.Name);
            return new MethodInfo(methodName, $"{fullyQualifiedTypeName}.{methodName}", TimeSpan.Zero);
        }

        private static int GetMethodCount(MetadataReader reader, TypeDefinition type)
        {
            var count = 0;
            foreach (var handle in type.GetMethods())
            {
                var methodDefinition = reader.GetMethodDefinition(handle);
                if (!ShouldIncludeMethod(reader, methodDefinition))
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private static bool IsValidIdentifier(MetadataReader reader, StringHandle handle)
        {
            var name = reader.GetString(handle);
            for (int i = 0; i < name.Length; i++)
            {
                switch (name[i])
                {
                    case '<':
                    case '>':
                    case '$':
                        return false;
                }
            }

            return true;
        }

        private static bool InheritsFromFrameworkBaseType(MetadataReader reader, TypeDefinition type)
        {
            if (type.BaseType.Kind != HandleKind.TypeReference)
            {
                return false;
            }

            var typeRef = reader.GetTypeReference((TypeReferenceHandle)type.BaseType);
            return
                reader.GetString(typeRef.Namespace) == "System" &&
                reader.GetString(typeRef.Name) is "Object" or "ValueType" or "Enum";
        }

        private static (string Name, string FullyQualifiedName) GetTypeName(MetadataReader reader, TypeDefinition type)
        {
            var typeName = reader.GetString(type.Name);

            if (TypeAttributes.NestedPublic == (type.Attributes & TypeAttributes.NestedPublic))
            {
                // Need to take into account the containing type.
                var declaringType = reader.GetTypeDefinition(type.GetDeclaringType());
                var (declaringTypeName, declaringTypeFullName) = GetTypeName(reader, declaringType);
                return (typeName, $"{declaringTypeFullName}+{typeName}");
            }

            var namespaceName = reader.GetString(type.Namespace);
            if (string.IsNullOrEmpty(namespaceName))
            {
                return (typeName, typeName);
            }

            return (typeName, $"{namespaceName}.{typeName}");
        }
    }
}
