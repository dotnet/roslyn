// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests
{
    /// <summary>
    /// Data storage that works under %LOCALAPPDATA%
    /// </summary>
    internal sealed class LocalDataStorage : IDataStorage
    {
        public bool TryGetTestResult(string cacheKey, out TestResult testResult)
        {
            testResult = default(TestResult);
            return false;
        }

        public void AddTestResult(string cacheKey, TestResult testResult)
        {
            
        }
    }
}
