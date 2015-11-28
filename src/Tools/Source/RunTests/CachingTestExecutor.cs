// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    internal sealed class CachingTestExecutor : ITestExecutor
    {
        private readonly ITestExecutor _testExecutor;
        private readonly CacheUtil _cacheUtil;

        internal CachingTestExecutor(ITestExecutor testExecutor)
        {
            _testExecutor = testExecutor;
            _cacheUtil = new CacheUtil();
        }

        public async Task<TestResult> RunTest(string assemblyPath, CancellationToken cancellationToken)
        {
            var cacheKey = _cacheUtil.GetCacheKey(assemblyPath);
            TestResult testResult;
            if (!_cacheUtil.TryGetTestResult(cacheKey, out testResult))
            {
                testResult = await _testExecutor.RunTest(assemblyPath, cancellationToken);
                _cacheUtil.AddTestResult(cacheKey, testResult);
            }

            return testResult;
        }
    }
}
