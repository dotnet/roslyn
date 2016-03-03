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
        private readonly StringBuilder _builder;
        private readonly Options _options;

        internal AssemblyScheduler(Options options)
        {
            _builder = new StringBuilder();
            _options = options;
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

        public IEnumerable<AssemblyInfo> Schedule(string assemblyPath)
        {
            var scheduleList = new List<AssemblyInfo>();
            var id = 0;
            var count = 0;
            foreach (var tuple in GetClassNames(assemblyPath))
            {
                count += tuple.Item2;
                _builder.Append($@"-class ""{tuple.Item1}"" ");
                if (count > 700)
                {
                    scheduleList.Add(CreateAssemblyInfo(assemblyPath, id, _builder.ToString()));
                    _builder.Length = 0;
                    count = 0;
                    id++;
                }
            }

            if (_builder.Length > 0)
            {
                scheduleList.Add(CreateAssemblyInfo(assemblyPath, id, _builder.ToString()));
                _builder.Length = 0;
            }

            return scheduleList;
        }

        private AssemblyInfo CreateAssemblyInfo(string assemblyPath, int id, string arguments)
        {
            var assemblyName = Path.GetFileName(assemblyPath);
            var displayName = $"{assemblyName}.{id}";
            var suffix = _options.UseHtml ? "html" : "xml";
            var resultsFileName = $"{assemblyName}.{id}.{suffix}";
            return new AssemblyInfo(
                assemblyPath,
                displayName,
                resultsFileName,
                arguments);
        }

        private List<Tuple<string, int>> GetClassNames(string assemblyPath)
        { 
            using (var stream = File.OpenRead(assemblyPath))
            using (var peReader = new PEReader(stream))
            {
                var metadataReader = peReader.GetMetadataReader();
                return GetClassNames(metadataReader);
            }
        }

        private List<Tuple<string, int>> GetClassNames(MetadataReader reader)
        {
            var list = new List<Tuple<string, int>>();
            foreach (var handle in reader.TypeDefinitions)
            {
                var type = reader.GetTypeDefinition(handle);
                var methodCount = GetMethodCount(reader, type);
                if (methodCount == 0)
                {
                    continue;
                }

                var namespaceName = reader.GetString(type.Namespace);
                var typeName = reader.GetString(type.Name);
                var fullName = $"{namespaceName}.{typeName}";
                list.Add(Tuple.Create(fullName, methodCount));
            }

            return list;
        }

        private int GetMethodCount(MetadataReader reader, TypeDefinition type)
        {
            if (TypeAttributes.Public != (type.Attributes & TypeAttributes.Public))
            {
                return 0;
            }

            var count = 0;
            foreach (var handle in type.GetMethods())
            {
                var methodDefinition = reader.GetMethodDefinition(handle);
                if (methodDefinition.GetCustomAttributes().Count == 0)
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
    }
}
