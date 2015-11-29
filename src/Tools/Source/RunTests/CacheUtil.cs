// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RunTests
{
    internal sealed class CacheUtil
    {
        private readonly Options _options;
        private readonly MD5 _hash = MD5.Create();

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
            catch
            {
                return string.Empty;
            }
        }

        private string BuildAssemblyCacheFile(string assemblyPath)
        {
            var builder = new StringBuilder();
            AppendFileLine(builder, assemblyPath);
            AppendFileLine(builder, _options.XunitPath);
            builder.AppendLine($"{nameof(_options.Test64)} - {_options.Test64}");
            builder.AppendLine($"{nameof(_options.UseHtml)} - {_options.UseHtml}");
            builder.AppendLine($"{nameof(_options.Trait)} - {_options.Trait}");
            builder.AppendLine($"{nameof(_options.NoTrait)} - {_options.NoTrait}");

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
            var bytes = File.ReadAllBytes(filePath);
            var hashBytes = _hash.ComputeHash(bytes);
            return HashBytesToString(hashBytes);
        }
    }
}
