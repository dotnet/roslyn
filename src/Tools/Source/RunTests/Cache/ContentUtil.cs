// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RunTests.Cache
{
    internal sealed class ContentUtil
    {
        private readonly Options _options;
        private readonly MD5 _hash = MD5.Create();
        private readonly Dictionary<string, string> _fileToChecksumMap = new Dictionary<string, string>();

        internal ContentUtil(Options options)
        {
            _options = options;
        }

        internal ContentFile GetTestResultContentFile(AssemblyInfo assemblyInfo)
        {
            try
            {
                var content = BuildTestResultContent(assemblyInfo);
                var checksum = GetChecksum(content);
                return new ContentFile(checksum: checksum, content: content);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error creating log file {ex.Message} {ex.StackTrace}");
                return ContentFile.Empty;
            }
        }

        private string BuildTestResultContent(AssemblyInfo assemblyInfo)
        {
            var builder = new StringBuilder();
            var assemblyPath = assemblyInfo.AssemblyPath;
            builder.AppendLine($"Assembly: {Path.GetFileName(assemblyPath)} {GetFileChecksum(assemblyPath)}");
            builder.AppendLine($"Display Name: {assemblyInfo.DisplayName}");

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

            // TODO: Need to include dependency information here, option data, etc ...
            // Test file alone isn't enough.  Makes it easy to test though.
            return builder.ToString();
        }

        private void AppendReferences(StringBuilder builder, string assemblyPath)
        {
            builder.AppendLine("References:");

            var initialAssembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
            var set = new HashSet<string>();
            var toVisit = new Queue<AssemblyName>(initialAssembly.GetReferencedAssemblies());
            var binariesPath = Path.GetDirectoryName(initialAssembly.Location);
            var references = new List<Tuple<string, string>>();

            while (toVisit.Count > 0)
            {
                var current = toVisit.Dequeue();
                if (!set.Add(current.FullName))
                {
                    continue;
                }

                try
                {
                    var currentPath = Path.Combine(binariesPath, Path.ChangeExtension(current.Name, "dll"));
                    var currentAssembly = File.Exists(currentPath)
                        ? Assembly.ReflectionOnlyLoadFrom(currentPath)
                        : Assembly.ReflectionOnlyLoad(current.FullName);

                    foreach (var name in currentAssembly.GetReferencedAssemblies())
                    {
                        toVisit.Enqueue(name);
                    }

                    var currentHash = GetFileChecksum(currentAssembly.Location);
                    references.Add(Tuple.Create(current.Name, currentHash));
                }
                catch
                {
                    references.Add(Tuple.Create(current.Name, "<could not calculate checksum>"));
                }
            }

            references.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            foreach (var pair in references)
            {
                builder.AppendLine($"\t{pair.Item1} {pair.Item2}");
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
            string checksum;
            if (_fileToChecksumMap.TryGetValue(filePath, out checksum))
            {
                return checksum;
            }

            checksum = GetFileChecksumCore(filePath);
            _fileToChecksumMap.Add(filePath, checksum);
            return checksum;
        }

        private string GetFileChecksumCore(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            var hashBytes = _hash.ComputeHash(bytes);
            return HashBytesToString(hashBytes);
        }
    }
}
