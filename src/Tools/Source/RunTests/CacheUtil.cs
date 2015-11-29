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
    internal struct CacheFile
    {
        internal static readonly CacheFile Empty = new CacheFile(string.Empty, string.Empty);

        internal readonly string CacheKey;
        internal readonly string Contents;

        internal CacheFile(string cacheKey, string contents)
        {
            CacheKey = cacheKey;
            Contents = contents;
        }
    }

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
            return GetCacheFile(assemblyPath).CacheKey;
        }

        internal string GetCacheFileContents(string assemblyPath)
        {
            return BuildAssemblyCacheFile(assemblyPath);
        }

        internal CacheFile GetCacheFile(string assemblyPath)
        {
            try
            {
                var contents = GetCacheFileContents(assemblyPath);
                var checksum = GetHashString(contents);
                return new CacheFile(cacheKey: checksum, contents: contents);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error creating log file {ex.Message} {ex.StackTrace}");
                return CacheFile.Empty;
            }
        }

        private string BuildAssemblyCacheFile(string assemblyPath)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Assembly: {Path.GetFileName(assemblyPath)} {GetFileChecksum(assemblyPath)}");
            builder.AppendLine($"Xunit: {Path.GetFileName(_options.XunitPath)} {GetFileChecksum(_options.XunitPath)}");
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
