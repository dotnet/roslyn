// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests
{
    internal sealed class CacheUtil
    {
        /// <summary>
        /// Get the cache string for the given test assembly file.
        /// </summary>
        internal string GetCacheKey(string assemblyPath)
        {
            return Guid.NewGuid().ToString();
        }

        internal bool TryGetTestResult(string cacheKey, out TestResult testResult)
        {
            testResult = default(TestResult);
            return false;
        }

        internal void AddTestResult(string cacheKey, TestResult testResult)
        {
            
        }
    }
}
