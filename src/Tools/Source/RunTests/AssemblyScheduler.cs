using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace RunTests
{
    internal struct AssemblyInfo
    {
        internal readonly string AssemblyPath;
        internal readonly string DisplayName;
        internal readonly string ResultsFileName;
        internal readonly string ExtraArguments;

        internal AssemblyInfo(
            string assemblyPath,
            string displayName,
            string resultsFileName,
            string extraArguments)
        {
            AssemblyPath = assemblyPath;
            DisplayName = displayName;
            ResultsFileName = resultsFileName;
            ExtraArguments = extraArguments;
        }

        internal AssemblyInfo(string assemblyPath, bool useHmtl)
        {
            AssemblyPath = assemblyPath;
            DisplayName = Path.GetFileName(assemblyPath);

            var suffix = useHmtl ? "html" : "xml";
            ResultsFileName = $"{DisplayName}.{suffix}";
            ExtraArguments = string.Empty;
        }

        public override string ToString() => DisplayName;
    }

    internal sealed class AssemblyScheduler
    {
        private struct TypeInfo
        {
            internal readonly string FullName;
            internal readonly int MethodCount;

            internal TypeInfo(string fullName, int methodCount)
            {
                FullName = fullName;
                MethodCount = methodCount;
            }
        }

        private struct Chunk
        {
            internal readonly string AssemblyPath;
            internal readonly int Id;
            internal List<TypeInfo> TypeInfoList;

            internal Chunk(string assemblyPath, int id, List<TypeInfo> typeInfoList)
            {
                AssemblyPath = assemblyPath;
                Id = id;
                TypeInfoList = typeInfoList;
            }
        }

        private sealed class AssemblyInfoBuilder
        {
            private readonly List<Chunk> _chunkList = new List<Chunk>();
            private readonly List<AssemblyInfo> _assemblyInfoList = new List<AssemblyInfo>();
            private readonly StringBuilder _builder = new StringBuilder();
            private readonly string _assemblyPath;
            private readonly int _methodLimit;
            private readonly bool _useHtml;
            private int _currentId;
            private List<TypeInfo> _currentTypeInfoList = new List<TypeInfo>();

            private AssemblyInfoBuilder(string assemblyPath, int methodLimit, bool useHtml)
            {
                _assemblyPath = assemblyPath;
                _useHtml = useHtml;
                _methodLimit = methodLimit;
            }

            internal static void Build(string assemblyPath, int methodLimit, bool useHtml, List<TypeInfo> typeInfoList, out List<Chunk> chunkList, out List<AssemblyInfo> assemblyInfoList)
            {
                var builder = new AssemblyInfoBuilder(assemblyPath, methodLimit, useHtml);
                builder.Build(typeInfoList);
                chunkList = builder._chunkList;
                assemblyInfoList = builder._assemblyInfoList;
            }

            private void Build(List<TypeInfo> typeInfoList)
            {
                foreach (var typeInfo in typeInfoList)
                {
                    _currentTypeInfoList.Add(typeInfo);
                    _builder.Append($@"-class ""{typeInfo.FullName}"" ");
                    CheckForChunkLimit(done: false);
                }

                CheckForChunkLimit(done: true);
            }

            private void CheckForChunkLimit(bool done)
            {
                if (done && _currentTypeInfoList.Count > 0)
                {
                    CreateChunk();
                    return;
                }

                // One item we have to consider here is the maximum command line length in 
                // Windows which is 32767 characters (XP is smaller but don't care).  Once
                // we get close then create a chunk and move on. 
                if (_currentTypeInfoList.Sum(x => x.MethodCount) >= _methodLimit ||
                    _builder.Length > 25000)
                {
                    CreateChunk();
                    return;
                }
            }

            private void CreateChunk()
            {
                var assemblyName = Path.GetFileName(_assemblyPath);
                var displayName = $"{assemblyName}.{_currentId}";
                var suffix = _useHtml ? "html" : "xml";
                var resultsFileName = $"{assemblyName}.{_currentId}.{suffix}";
                var assemblyInfo = new AssemblyInfo(
                    _assemblyPath,
                    displayName,
                    resultsFileName,
                    _builder.ToString());

                _chunkList.Add(new Chunk(_assemblyPath, _currentId, _currentTypeInfoList));
                _assemblyInfoList.Add(assemblyInfo);

                _currentId++;
                _currentTypeInfoList = new List<TypeInfo>();
                _builder.Length = 0;
            }
        }


        /// <summary>
        /// Default number of methods to include per chunk.
        /// </summary>
        internal const int DefaultMethodLimit = 2000;

        private readonly Options _options;
        private readonly int _methodLimit;

        internal AssemblyScheduler(Options options, int methodLimit = DefaultMethodLimit)
        {
            _options = options;
            _methodLimit = methodLimit;
        }

        internal IEnumerable<AssemblyInfo> Schedule(IEnumerable<string> assemblyPaths)
        {
            var list = new List<AssemblyInfo>();
            foreach (var assemblyPath in assemblyPaths)
            {
                list.AddRange(Schedule(assemblyPath));
            }

            return list;
        }

        public IEnumerable<AssemblyInfo> Schedule(string assemblyPath, bool force = false)
        {
            var typeInfoList = GetTypeInfoList(assemblyPath);
            var assemblyInfoList = new List<AssemblyInfo>();
            var chunkList = new List<Chunk>();
            AssemblyInfoBuilder.Build(assemblyPath, _methodLimit, _options.UseHtml, typeInfoList, out chunkList, out assemblyInfoList);

            // If the scheduling didn't actually produce multiple chunks then send back an unchunked
            // representation.
            if (assemblyInfoList.Count == 1 && !force)
            {
                Logger.Log($"Assembly schedule produced a single chunk {assemblyPath}");
                return new[] { CreateAssemblyInfo(assemblyPath) };
            }

            Logger.Log($"Assembly Schedule: {Path.GetFileName(assemblyPath)}");
            foreach (var chunk in chunkList)
            {
                var methodCount = chunk.TypeInfoList.Sum(x => x.MethodCount);
                var delta = methodCount - _methodLimit;
                Logger.Log($"  Chunk: {chunk.Id} method count {methodCount} delta {delta}");
                foreach (var typeInfo in chunk.TypeInfoList)
                {
                    Logger.Log($"    {typeInfo.FullName} {typeInfo.MethodCount}");
                }
            }

            return assemblyInfoList;
        }

        public AssemblyInfo CreateAssemblyInfo(string assemblyPath)
        {
            return new AssemblyInfo(assemblyPath, _options.UseHtml);
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
        /// Determine if this type should be one of the `class` values passed to xunit.  This
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
                TypeAttributes.Abstract == (type.Attributes & TypeAttributes.Abstract)  ||
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
            // because they clearly don't fit that category.
            return !(InheritsFromObject(reader, type) ?? false);
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

                if (MethodAttributes.Public != (methodDefinition.Attributes & MethodAttributes.Public))
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
            for (int i=  0; i < name.Length; i++)
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

        private static bool? InheritsFromObject(MetadataReader reader, TypeDefinition type)
        {
            if (type.BaseType.Kind != HandleKind.TypeReference)
            {
                return null;
            }

            var typeRef = reader.GetTypeReference((TypeReferenceHandle)type.BaseType);
            return 
                reader.GetString(typeRef.Namespace) == "System" && 
                reader.GetString(typeRef.Name) == "Object";
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
