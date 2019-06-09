// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

namespace RunTests.Cache
{
    internal sealed class ContentUtil
    {
        private readonly TestExecutionOptions _options;
        private readonly MD5 _hash = MD5.Create();

        /// <summary>
        /// Building up checksums for assembly values represents a significant amount of time when calculating
        /// the content for a given test.  As such we aggressively cache these results.
        /// 
        /// The cache is done by the MVID of the assembly.  This is guaranteed to be different for different
        /// content and it's efficient to read.  Hence it's an excellent key.
        /// </summary>
        private readonly Dictionary<Guid, string> _assemblyChecksumCacheMap = new Dictionary<Guid, string>();

        /// <summary>
        /// Stores a map between a unit test assembly and the reference section of the content file.  For 
        /// a number of assemblies the reference section is calculated multiple times.  That is a non-trivial
        /// cost due to the IO and processing
        /// </summary>
        private readonly Dictionary<string, (string content, bool isError)> _referenceSectionMap = new Dictionary<string, (string content, bool isError)>();

        internal ContentUtil(TestExecutionOptions options)
        {
            _options = options;
        }

        internal ContentFile GetTestResultContentFile(AssemblyInfo assemblyInfo)
        {
            var content = BuildTestResultContent(assemblyInfo);
            var checksum = GetChecksum(content);
            return new ContentFile(checksum: checksum, content: content);
        }

        private string BuildTestResultContent(AssemblyInfo assemblyInfo)
        {
            var builder = new StringBuilder();
            var assemblyPath = assemblyInfo.AssemblyPath;
            builder.AppendLine($"Assembly: {Path.GetFileName(assemblyPath)} {GetFileChecksum(assemblyPath)}");
            builder.AppendLine($"Display Name: {assemblyInfo.DisplayName}");
            builder.AppendLine($"Results File Name; {assemblyInfo.ResultsFileName}");

            var configFilePath = $"{assemblyPath}.config";
            var configFileChecksum = File.Exists(configFilePath)
                ? GetFileChecksum(configFilePath)
                : "<no config file>";
            builder.AppendLine($"Config: {Path.GetFileName(configFilePath)} {configFileChecksum}");

            builder.AppendLine($"Xunit: {Path.GetFileName(_options.XunitPath)} {GetFileChecksum(_options.XunitPath)}");
            AppendReferences(builder, assemblyPath);
            builder.AppendLine("Options:");
            builder.AppendLine($"\t{nameof(_options.Test64)} - {_options.Test64}");
            builder.AppendLine($"\t{nameof(_options.UseHtml)} - {_options.UseHtml}");
            builder.AppendLine($"\t{nameof(_options.Trait)} - {_options.Trait}");
            builder.AppendLine($"\t{nameof(_options.NoTrait)} - {_options.NoTrait}");
            builder.AppendLine($"Extra Options: {assemblyInfo.ExtraArguments}");
            AppendExtra(builder, assemblyPath);

            return builder.ToString();
        }

        private void AppendReferences(StringBuilder builder, string unitTestAssemblyPath)
        {
            if (!_referenceSectionMap.TryGetValue(unitTestAssemblyPath, out var tuple))
            {
                tuple = GetReferenceSectionCore(unitTestAssemblyPath);
                _referenceSectionMap[unitTestAssemblyPath] = tuple;
            }

            if (tuple.isError)
            {
                throw new Exception(tuple.content);
            }

            builder.AppendLine(tuple.content);
        }

        private (string content, bool isError) GetReferenceSectionCore(string unitTestAssemblyPath)
        {
            // This map is used for diagnostics and tracks the set of assemblies which bring in a given
            // name as a reference.
            var referenceMap = new Dictionary<string, List<AssemblyName>>();
            void noteReference(AssemblyName source, AssemblyName referenced)
            {
                var key = referenced.FullName;
                if (!referenceMap.TryGetValue(key, out var list))
                {
                    list = new List<AssemblyName>();
                    referenceMap[key] = list;
                }

                list.Add(source);
            }

            var unitTestAssemblyName = AssemblyName.GetAssemblyName(unitTestAssemblyPath);
            var binariesPath = Path.GetDirectoryName(unitTestAssemblyPath);
            var assemblyUtil = new AssemblyUtil(binariesPath);

            var toVisit = new Queue<AssemblyName>();
            void enqueueReferences(string assemblyPath)
            {
                var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
                foreach (var current in assemblyUtil.GetReferencedAssemblies(assemblyPath))
                {
                    noteReference(assemblyName, current);
                    toVisit.Enqueue(current);
                }
            }

            enqueueReferences(unitTestAssemblyPath);

            var missingSet = new HashSet<string>();
            var visitedSet = new HashSet<string>();
            var references = new List<(string assemblyName, string assemblyHash)>();
            while (toVisit.Count > 0)
            {
                var current = toVisit.Dequeue();
                if (!visitedSet.Add(current.FullName))
                {
                    continue;
                }

                if (assemblyUtil.TryGetAssemblyPath(current, out var currentPath))
                {
                    enqueueReferences(currentPath);
                    var currentHash = GetFileChecksum(currentPath);
                    references.Add((current.Name, currentHash));
                }
                else if (assemblyUtil.IsKnownMissingAssembly(current))
                {
                    references.Add((current.Name, "<missing light up reference>"));
                }
                else
                {
                    missingSet.Add(current.FullName);
                }
            }

            if (missingSet.Count == 0)
            {
                var builder = new StringBuilder();
                builder.AppendLine("References:");
                references.Sort((x, y) => x.assemblyName.CompareTo(y.assemblyName));
                foreach (var pair in references)
                {
                    builder.AppendLine($"\t{pair.assemblyName} {pair.assemblyHash}");
                }

                return (builder.ToString(), isError: false);
            }
            else
            {
                // Error if there are any referenced assemblies that we were unable to resolve.
                var errorBuilder = new StringBuilder();
                errorBuilder.AppendLine($"Unable to resolve {missingSet.Count} referenced assemblies");
                foreach (var item in missingSet.OrderBy(x => x))
                {
                    errorBuilder.AppendLine($"\t{item} referenced from");
                    var list = referenceMap[item];
                    foreach (var source in list.OrderBy(x => x))
                    {
                        errorBuilder.AppendLine($"\t\t{source.Name}");
                    }
                }

                return (errorBuilder.ToString(), isError: true);
            }
        }

        private void AppendExtra(StringBuilder builder, string assemblyPath)
        {
            builder.AppendLine("Extra Files:");
            var all = new[]
            {
                "*.targets",
                "*.props"
            };

            var binariesPath = Path.GetDirectoryName(assemblyPath);
            foreach (var ext in all)
            {
                foreach (var file in Directory.EnumerateFiles(binariesPath, ext))
                {
                    builder.AppendLine($"\t{Path.GetFileName(file)} - {GetFileChecksum(file)}");
                }
            }
        }

        private string GetChecksum(string content)
        {
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var hashBytes = _hash.ComputeHash(contentBytes);
            return HashBytesToString(hashBytes);
        }

        private static string HashBytesToString(byte[] hash)
        {
            var data = BitConverter.ToString(hash);
            return data.Replace("-", "");
        }

        private string GetFileChecksum(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            return (ext == ".dll" || ext == ".exe")
                ? GetAssemblyChecksum(filePath)
                : GetFileChecksumCore(filePath);
        }

        private string GetAssemblyChecksum(string filePath)
        {
            var mvid = GetAssemblyMvid(filePath);
            if (!_assemblyChecksumCacheMap.TryGetValue(mvid, out var checksum))
            {
                checksum = GetFileChecksumCore(filePath);
                _assemblyChecksumCacheMap[mvid] = checksum;
            }

            return checksum;
        }

        private Guid GetAssemblyMvid(string filePath)
        {
            using (var source = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new PEReader(source))
            {
                var metadataReader = reader.GetMetadataReader();
                return metadataReader.GetGuid(metadataReader.GetModuleDefinition().Mvid);
            }
        }

        private string GetFileChecksumCore(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            var hashBytes = _hash.ComputeHash(bytes);
            return HashBytesToString(hashBytes);
        }
    }
}
