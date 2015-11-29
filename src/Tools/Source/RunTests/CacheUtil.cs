// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RunTests
{
    internal sealed class CacheUtil
    {
        private readonly Options _options;
        private readonly MD5 _hash = MD5.Create();
        private readonly Dictionary<string, string> _fileToChecksumMap = new Dictionary<string, string>();

        internal CacheUtil(Options options)
        {
            _options = options;
        }

        /// <summary>
        /// Get the cache string for the given test assembly file.
        /// </summary>
        internal string GetCacheKey(string assemblyPath)
        {
            try
            {
                var fileContents = BuildAssemblyCacheFile(assemblyPath);
                return GetHashString(fileContents);
            }
            catch
            {
                // Lots of file IO that can fail.  When it fails return a value
                // that's unique and won't result in a cache hit.
                return Guid.NewGuid().ToString();
            }
        }

        internal string GetCacheFile(string assemblyPath)
        {
            try
            {
                return BuildAssemblyCacheFile(assemblyPath);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error creating log file {ex.Message} {ex.StackTrace}");
                return string.Empty;
            }
        }

        private string BuildAssemblyCacheFile(string assemblyPath)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Assembly: {Path.GetFileName(assemblyPath)} {GetFileChecksum(assemblyPath)}");
            builder.AppendLine($"Xunit: {_options.XunitPath} {GetFileChecksum(_options.XunitPath)}");
            AppendReferences(builder, assemblyPath);
            builder.AppendLine("Options:");
            builder.AppendLine($"\t{nameof(_options.Test64)} - {_options.Test64}");
            builder.AppendLine($"\t{nameof(_options.UseHtml)} - {_options.UseHtml}");
            builder.AppendLine($"\t{nameof(_options.Trait)} - {_options.Trait}");
            builder.AppendLine($"\t{nameof(_options.NoTrait)} - {_options.NoTrait}");

            // TODO: Need to include dependency information here, option data, etc ...
            // Test file alone isn't enough.  Makes it easy to test though.
            return builder.ToString();
        }

        private void AppendFileLine(StringBuilder builder, string assemblyPath)
        {
            // TODO: Use something like a /pathmap option to normalize this when we 
            // want to share across developer machines. 
            var fileHash = GetFileChecksum(assemblyPath);
            builder.AppendFormat($"{assemblyPath} {fileHash}");
            builder.AppendLine();
        }

        private void AppendReferences(StringBuilder builder, string assemblyPath)
        {
            builder.AppendLine("References:");

            var initialAssembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
            var set = new HashSet<string>();
            var toVisit = new Queue<AssemblyName>(initialAssembly.GetReferencedAssemblies());
            var binariesPath = Path.GetDirectoryName(initialAssembly.Location);

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
                    builder.AppendLine($"\t{current.Name} {currentHash}");
                }
                catch
                {
                    builder.AppendLine($"\t{current.Name} <could not calculate checksum>");
                }
            }
        }

        private string GetHashString(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = _hash.ComputeHash(inputBytes);
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
