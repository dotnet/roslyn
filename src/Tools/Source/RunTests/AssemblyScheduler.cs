// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace RunTests
{
    internal readonly struct AssemblyInfo
    {
        internal PartitionInfo PartitionInfo { get; }
        internal string TargetFramework { get; }
        internal string Architecture { get; }

        internal string AssemblyPath => PartitionInfo.AssemblyPath;
        internal string AssemblyName => Path.GetFileName(PartitionInfo.AssemblyPath);
        internal string DisplayName => PartitionInfo.DisplayName;

        internal AssemblyInfo(PartitionInfo partitionInfo, string targetFramework, string architecture)
        {
            PartitionInfo = partitionInfo;
            TargetFramework = targetFramework;
            Architecture = architecture;
        }
    }

    internal readonly struct PartitionInfo
    {
        internal int? AssemblyPartitionId { get; }
        internal string AssemblyPath { get; }
        internal string DisplayName { get; }

        /// <summary>
        /// Specific set of types to test in the assembly. Will be empty when testing the entire assembly
        /// </summary>
        internal readonly ImmutableArray<TypeInfo> TypeInfoList;

        internal PartitionInfo(
            int assemblyPartitionId,
            string assemblyPath,
            string displayName,
            ImmutableArray<TypeInfo> typeInfoList)
        {
            AssemblyPartitionId = assemblyPartitionId;
            AssemblyPath = assemblyPath;
            DisplayName = displayName;
            TypeInfoList = typeInfoList;
        }

        internal PartitionInfo(string assemblyPath)
        {
            AssemblyPartitionId = null;
            AssemblyPath = assemblyPath;
            DisplayName = Path.GetFileName(assemblyPath);
            TypeInfoList = ImmutableArray<TypeInfo>.Empty;
        }

        public override string ToString() => DisplayName;
    }

    public readonly struct TypeInfo
    {
        internal readonly string FullName;
        internal readonly int MethodCount;

        internal TypeInfo(string fullName, int methodCount)
        {
            FullName = fullName;
            MethodCount = methodCount;
        }
    }

    internal sealed class AssemblyScheduler
    {
        /// <summary>
        /// This is a test class inserted into assemblies to guard against a .NET desktop bug.  The tests
        /// inside of it counteract the underlying issue.  If this test is included in any assembly it 
        /// must be added to every partition to ensure the work around is present
        /// 
        /// https://github.com/dotnet/corefx/issues/3793
        /// https://github.com/dotnet/roslyn/issues/8936
        /// </summary>
        private const string EventListenerGuardFullName = "Microsoft.CodeAnalysis.UnitTests.EventListenerGuard";

        private static class AssemblyInfoBuilder
        {
            internal static void Build(string assemblyPath, int methodLimit, List<TypeInfo> typeInfoList, out ImmutableArray<PartitionInfo> partitionInfoList)
            {
                var list = new List<PartitionInfo>();
                var hasEventListenerGuard = typeInfoList.Any(x => x.FullName == EventListenerGuardFullName);
                var currentTypeInfoList = new List<TypeInfo>();
                var currentClassNameLengthSum = -1;
                var currentId = 0;

                BeginPartition();

                foreach (var typeInfo in typeInfoList)
                {
                    currentTypeInfoList.Add(typeInfo);
                    currentClassNameLengthSum += typeInfo.FullName.Length;
                    CheckForPartitionLimit(done: false);
                }

                CheckForPartitionLimit(done: true);

                partitionInfoList = ImmutableArray.CreateRange(list);

                void BeginPartition()
                {
                    currentId++;
                    currentTypeInfoList.Clear();
                    currentClassNameLengthSum = 0;

                    // Ensure the EventListenerGuard is in every partition.
                    if (hasEventListenerGuard)
                    {
                        currentClassNameLengthSum += EventListenerGuardFullName.Length;
                    }
                }

                void CheckForPartitionLimit(bool done)
                {
                    if (done)
                    {
                        // The builder is done looking at types.  If there are any TypeInfo that have not
                        // been added to a partition then do it now.
                        if (currentTypeInfoList.Count > 0)
                        {
                            FinishPartition();
                        }

                        return;
                    }

                    // One item we have to consider here is the maximum command line length in 
                    // Windows which is 32767 characters (XP is smaller but don't care).  Once
                    // we get close then create a partition and move on. 
                    if (currentTypeInfoList.Sum(x => x.MethodCount) >= methodLimit ||
                        currentClassNameLengthSum > 25000)
                    {
                        FinishPartition();
                        BeginPartition();
                    }

                    void FinishPartition()
                    {
                        var partitionInfo = new PartitionInfo(
                            currentId,
                            assemblyPath,
                            $"{Path.GetFileName(assemblyPath)}.{currentId}",
                            ImmutableArray.CreateRange(currentTypeInfoList));
                        list.Add(partitionInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Default number of methods to include per partition.
        /// </summary>
        internal const int DefaultMethodLimit = 2000;

        /// <summary>
        /// Number of methods to include per Helix work item.
        /// </summary>
        internal const int HelixMethodLimit = 500;

        private readonly Options _options;
        private readonly int _methodLimit;

        internal AssemblyScheduler(Options options)
        {
            _options = options;
            _methodLimit = options.UseHelix ? AssemblyScheduler.HelixMethodLimit : AssemblyScheduler.DefaultMethodLimit;
        }

        public ImmutableArray<PartitionInfo> Schedule(string assemblyPath, bool force = false)
        {
            if (_options.Sequential)
            {
                return ImmutableArray.Create(new PartitionInfo(assemblyPath));
            }

            var typeInfoList = GetTypeInfoList(assemblyPath);
            AssemblyInfoBuilder.Build(assemblyPath, _methodLimit, typeInfoList, out var partitionList);

            // If the scheduling didn't actually produce multiple partition then send back an unpartitioned
            // representation.
            if (partitionList.Length == 1 && !force)
            {
                Logger.Log($"Assembly schedule produced a single partition {assemblyPath}");
                return ImmutableArray.Create(new PartitionInfo(assemblyPath));
            }

            Logger.Log($"Assembly Schedule: {Path.GetFileName(assemblyPath)}");
            foreach (var partition in partitionList)
            {
                var methodCount = partition.TypeInfoList.Sum(x => x.MethodCount);
                var delta = methodCount - _methodLimit;
                Logger.Log($"  Partition: {partition.AssemblyPartitionId} method count {methodCount} delta {delta}");
                foreach (var typeInfo in partition.TypeInfoList)
                {
                    Logger.Log($"    {typeInfo.FullName} {typeInfo.MethodCount}");
                }
            }

            return partitionList;
        }

        private static List<TypeInfo> GetTypeInfoList(string assemblyPath)
        {
            using (var stream = File.OpenRead(assemblyPath))
            using (var peReader = new PEReader(stream))
            {
                var metadataReader = peReader.GetMetadataReader();
                return GetTypeInfoList(metadataReader);
            }
        }

        private static List<TypeInfo> GetTypeInfoList(MetadataReader reader)
        {
            var list = new List<TypeInfo>();
            foreach (var handle in reader.TypeDefinitions)
            {
                var type = reader.GetTypeDefinition(handle);
                if (!IsValidIdentifier(reader, type.Name))
                {
                    continue;
                }

                var methodCount = GetMethodCount(reader, type);
                if (!ShouldIncludeType(reader, type, methodCount))
                {
                    continue;
                }

                var fullName = GetFullName(reader, type);
                list.Add(new TypeInfo(fullName, methodCount));
            }

            // Ensure we get classes back in a deterministic order.
            list.Sort((x, y) => x.FullName.CompareTo(y.FullName));
            return list;
        }

        /// <summary>
        /// Determine if this type should be one of the <c>class</c> values passed to xunit.  This
        /// code doesn't actually resolve base types or trace through inherrited Fact attributes
        /// hence we have to error on the side of including types with no tests vs. excluding them.
        /// </summary>
        private static bool ShouldIncludeType(MetadataReader reader, TypeDefinition type, int testMethodCount)
        {
            // xunit only handles public, non-abstract classes
            var isPublic =
                TypeAttributes.Public == (type.Attributes & TypeAttributes.Public) ||
                TypeAttributes.NestedPublic == (type.Attributes & TypeAttributes.NestedPublic);
            if (!isPublic ||
                TypeAttributes.Abstract == (type.Attributes & TypeAttributes.Abstract) ||
                TypeAttributes.Class != (type.Attributes & TypeAttributes.Class))
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

        private static string GetFullName(MetadataReader reader, TypeDefinition type)
        {
            var typeName = reader.GetString(type.Name);

            if (TypeAttributes.NestedPublic == (type.Attributes & TypeAttributes.NestedPublic))
            {
                // Need to take into account the containing type.
                var declaringType = reader.GetTypeDefinition(type.GetDeclaringType());
                var declaringTypeFullName = GetFullName(reader, declaringType);
                return $"{declaringTypeFullName}+{typeName}";
            }

            var namespaceName = reader.GetString(type.Namespace);
            if (string.IsNullOrEmpty(namespaceName))
            {
                return typeName;
            }

            return $"{namespaceName}.{typeName}";
        }
    }
}
