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
        private readonly MD5 _hash = MD5.Create();

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

        internal bool TryGetTestResult(string cacheKey, out TestResult testResult)
        {
            testResult = default(TestResult);
            return false;
        }

        internal void AddTestResult(string cacheKey, TestResult testResult)
        {
            
        }

        private string BuildAssemblyCacheFile(string assemblyPath)
        {
            var builder = new StringBuilder();
            AppendFileLine(builder, assemblyPath);

            // TODO: Need to include dependency information here, option data, etc ...
            // Test file alone isn't enough.  Makes it easy to test though.
            return builder.ToString();
        }

        private void AppendFileLine(StringBuilder builder, string assemblyPath)
        {
            // TODO: Use something like a /pathmap option to normalize this when we 
            // want to share across developer machines. 
            var fileHash = GetFileChecksum(assemblyPath);
            builder.AppendFormat($"{assemblyPath} {Encoding.UTF8.GetString(fileHash)}");
        }

        private string GetHashString(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = _hash.ComputeHash(inputBytes);
            return Encoding.UTF8.GetString(hashBytes);
        }

        private byte[] GetFileChecksum(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            return _hash.ComputeHash(bytes);
        }
    }
}
