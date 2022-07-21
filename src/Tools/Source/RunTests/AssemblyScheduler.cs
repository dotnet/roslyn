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

    /// <summary>
    /// Defines a single work item to run.  The work item may contain tests from multiple assemblies
    /// that should be run together in the same work item.
    /// </summary>
    internal readonly record struct WorkItemInfo(ImmutableArray<AssemblyTypeGroup> TypesToTest, int PartitionIndex)
    {
        internal string DisplayName
        {
            get
            {
                var assembliesString = string.Join("_", TypesToTest.Select(group => Path.GetFileNameWithoutExtension(group.Assembly.AssemblyName).Replace(".", string.Empty)));
                return $"{assembliesString}_{PartitionIndex}";
            }
        }

        internal static WorkItemInfo CreateFullAssembly(AssemblyInfo assembly, int partitionIndex)
            => new(ImmutableArray<AssemblyTypeGroup>.Empty.Add(new(assembly, ImmutableArray<TypeInfo>.Empty, true)), partitionIndex);
    }

    internal readonly record struct AssemblyTypeGroup(AssemblyInfo Assembly, ImmutableArray<TypeInfo> Types, bool ContainsAllTypesInAssembly);


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
            if (_options.Sequential)
            {
                Logger.Log("Building sequential work items");
                // return individual work items per assembly that contain all the tests in that assembly.
                return assemblies
                    .Select(WorkItemInfo.CreateFullAssembly)
                    .ToImmutableArray();
            }

            var orderedTypeInfos = assemblies.ToImmutableSortedDictionary(assembly => assembly, GetTypeInfoList);
            ConsoleUtil.WriteLine($"Found {orderedTypeInfos.Values.SelectMany(t => t).Count()} test types to run in {orderedTypeInfos.Keys.Count()} assemblies");

            // Retrieve test runtimes from azure devops historical data.
            var testHistory = await TestHistoryManager.GetTestHistoryPerTypeAsync(cancellationToken);
            if (testHistory.IsEmpty)
            {
                // We didn't have any test history from azure devops, just partition by assembly.
                return assemblies
                    .Select(WorkItemInfo.CreateFullAssembly)
                    .ToImmutableArray();
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

            ConsoleUtil.WriteLine($"Built {workItems.Length} work items");
            LogWorkItems(workItems);
            return workItems;
        }

        private static ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<TypeInfo>> UpdateTestsWithExecutionTimes(
            ImmutableSortedDictionary<AssemblyInfo, ImmutableArray<TypeInfo>> assemblyTypes,
            ImmutableDictionary<string, TimeSpan> testHistory)
        {
            // Determine the average execution time so that we can use it for tests that do not have any history.
            var averageExecutionTime = TimeSpan.FromMilliseconds(testHistory.Values.Average(t => t.TotalMilliseconds));

            // Store the types from our assemblies that we couldn't find a history for to log.
            var extraLocalTypes = new HashSet<string>();

            // Store the types that we were able to match to historical data.
            var matchedLocalTypes = new HashSet<string>();

            var updated = assemblyTypes.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(typeInfo => WithExecutionTime(typeInfo)).ToImmutableArray());
            LogResults(matchedLocalTypes, extraLocalTypes);
            return updated;

            TypeInfo WithExecutionTime(TypeInfo typeInfo)
            {
                // Match by fully qualified test method name to azure devops historical data.
                // Note for combinatorial tests, azure devops helpfully groups all sub-runs under a top level method (with combined test run times) with the same fully qualified method name
                //  that we'll get from looking at all the test methods in the assembly with SRM.
                if (testHistory.TryGetValue(typeInfo.FullyQualifiedName, out var executionTime))
                {
                    matchedLocalTypes.Add(typeInfo.FullyQualifiedName);
                    return typeInfo with { ExecutionTime = executionTime };
                }

                // We didn't find the local type from our assembly in test run historical data.
                // This can happen if our SRM heuristic incorrectly counted a normal method as a test method (which it can do often).
                extraLocalTypes.Add(typeInfo.FullyQualifiedName);
                return typeInfo with { ExecutionTime = averageExecutionTime };
            }

            void LogResults(HashSet<string> matchedLocalTypes, HashSet<string> extraLocalTypes)
            {
                foreach (var extraLocalType in extraLocalTypes)
                {
                    Logger.Log($"Could not find test execution history for types in {extraLocalType}");
                }

                var extraRemoteTypes = testHistory.Keys.Where(type => !matchedLocalTypes.Contains(type));
                foreach (var extraRemoteType in extraRemoteTypes)
                {
                    Logger.Log($"Found historical data for types in {extraRemoteType} that were not present in local assemblies");
                }

                var totalExpectedRunTime = TimeSpan.FromMilliseconds(updated.Values.SelectMany(types => types).Sum(test => test.ExecutionTime.TotalMilliseconds));
                ConsoleUtil.WriteLine($"Matched {matchedLocalTypes.Count} types with historical data.  {extraLocalTypes.Count} types were missing historical data.  {extraRemoteTypes.Count()} types were missing in local assemblies.  Estimate of total execution time for tests is {totalExpectedRunTime}.");
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
            var currentAssemblyTests = new Dictionary<AssemblyInfo, AssemblyTypeGroup>();

            // Iterate through each assembly and type and build up the work items to run.
            // We add types from assemblies one by one until we hit our execution time limit,
            // at which point we create a new work item with the current types and start a new one.
            foreach (var (assembly, types) in typeInfos)
            {
                // See if we can just add all types from this assembly to the current work item without going over our execution time limit.
                var executionTimeForAllTypesInAssembly = TimeSpan.FromMilliseconds(types.Sum(t => t.ExecutionTime.TotalMilliseconds));
                if (executionTimeForAllTypesInAssembly + currentExecutionTime >= executionTimeLimit)
                {
                    // We can't add every type - go type by type to add what we can and end the work item where we need to.
                    foreach (var type in types)
                    {
                        if (type.ExecutionTime + currentExecutionTime >= executionTimeLimit)
                        {
                            // Adding this type would put us over the time limit for this partition.
                            // Add our accumulated tests and types and assemblies and end the work item.
                            CreateWorkItemWithCurrentAssemblies();
                        }

                        // Update the current group in the work item with this new type.  This is a partial group since we couldn't add all types in the assembly before.
                        var typeList = currentAssemblyTests.TryGetValue(assembly, out var result) ? result.Types.Add(type) : ImmutableArray.Create(type);
                        currentAssemblyTests[assembly] = new AssemblyTypeGroup(assembly, typeList, ContainsAllTypesInAssembly: false);
                        currentExecutionTime += type.ExecutionTime;
                    }
                }
                else
                {
                    // All the types in this assembly can safely be added to the current work item.
                    // Add them and update our work item execution time with the total execution time of tests in the assembly.
                    currentAssemblyTests.Add(assembly, new AssemblyTypeGroup(assembly, types, ContainsAllTypesInAssembly: true));
                    currentExecutionTime += executionTimeForAllTypesInAssembly;
                }
            }

            // Add any remaining tests to the work item.
            CreateWorkItemWithCurrentAssemblies();
            return workItems.ToImmutableArray();

            void CreateWorkItemWithCurrentAssemblies()
            {
                if (currentAssemblyTests.Any())
                {
                    workItems.Add(new WorkItemInfo(currentAssemblyTests.Values.ToImmutableArray(), workItemIndex));
                    workItemIndex++;
                }

                currentExecutionTime = TimeSpan.Zero;
                currentAssemblyTests = new();
            }
        }


        private static void LogWorkItems(ImmutableArray<WorkItemInfo> workItems)
        {
            Logger.Log("==== Work Item List ====");
            foreach (var workItem in workItems)
            {
                var types = workItem.TypesToTest.SelectMany(group => group.Types);
                var totalRuntime = TimeSpan.FromMilliseconds(types.Sum(type => type.ExecutionTime.TotalMilliseconds));
                Logger.Log($"- Work Item ({types.Count()} types, runtime {totalRuntime})");
                foreach (var group in workItem.TypesToTest)
                {
                    var typeExecutionTime = TimeSpan.FromMilliseconds(group.Types.Sum(test => test.ExecutionTime.TotalMilliseconds));
                    Logger.Log($"    - {group.Assembly.AssemblyName} with {group.Types.Length} types, runtime {typeExecutionTime}");
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

                list.Add(new TypeInfo(typeName, fullyQualifiedTypeName, methodCount, TimeSpan.Zero));
            }

            // Ensure we get classes back in a deterministic order.
            list.Sort((x, y) => x.FullyQualifiedName.CompareTo(y.FullyQualifiedName));
            return list.ToImmutableArray();
        }

        /// <summary>
        /// Determine if this type should be one of the <c>class</c> values passed to xunit.  This
        /// code doesn't actually resolve base types or trace through inherrited Fact attributes
        /// hence we have to error on the side of including types with no tests vs. excluding them.
        /// </summary>
        private static bool ShouldIncludeType(MetadataReader reader, TypeDefinition type, int testMethodCount)
        {
            // See https://docs.microsoft.com/en-us/dotnet/api/system.reflection.typeattributes?view=net-6.0#examples
            // for extracting this information from the TypeAttributes.
            var visibility = type.Attributes & TypeAttributes.VisibilityMask;
            var isPublic = visibility == TypeAttributes.Public || visibility == TypeAttributes.NestedPublic;

            var classSemantics = type.Attributes & TypeAttributes.ClassSemanticsMask;
            var isClass = classSemantics == TypeAttributes.Class;

            var isAbstract = (type.Attributes & TypeAttributes.Abstract) != 0;

            // xunit only handles public, non-abstract classes
            if (!isPublic || isAbstract || !isClass)
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

        private static int GetMethodCount(MetadataReader reader, TypeDefinition type)
        {
            var count = 0;
            foreach (var handle in type.GetMethods())
            {
                var methodDefinition = reader.GetMethodDefinition(handle);
                if (methodDefinition.GetCustomAttributes().Count == 0 ||
                    !IsValidIdentifier(reader, methodDefinition.Name))
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
