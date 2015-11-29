// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    internal sealed class CachingTestExecutor : ITestExecutor
    {
        private readonly ITestExecutor _testExecutor;
        private readonly CacheUtil _cacheUtil;
        private readonly IDataStorage _dataStorage;

        internal CachingTestExecutor(Options options, ITestExecutor testExecutor, IDataStorage dataStorage)
        {
            _testExecutor = testExecutor;
            _dataStorage = dataStorage;
            _cacheUtil = new CacheUtil(options);
        }

        public async Task<TestResult> RunTest(string assemblyPath, CancellationToken cancellationToken)
        {
            var cacheKey = _cacheUtil.GetCacheKey(assemblyPath);
            var builder = new StringBuilder();
            builder.AppendLine($"{Path.GetFileName(assemblyPath)} - {cacheKey}");
            builder.AppendLine("===");
            builder.AppendLine(_cacheUtil.GetCacheFile(assemblyPath));
            builder.AppendLine("===");
            Logger.Log(builder.ToString());

            TestResult testResult;
            if (!_dataStorage.TryGetTestResult(cacheKey, out testResult))
            {
                Logger.Log($"{Path.GetFileName(assemblyPath)} - running");
                testResult = await _testExecutor.RunTest(assemblyPath, cancellationToken);
                Logger.Log($"{Path.GetFileName(assemblyPath)} - caching");
                _dataStorage.AddTestResult(cacheKey, testResult);
            }
            else
            {
                testResult = Migrate(testResult);
                Logger.Log($"{Path.GetFileName(assemblyPath)} - cache hit");
            }

            return testResult;
        }

        /// <summary>
        /// The results file is specified in terms of the cache storage.  Need to make it local
        /// to the current output folder
        /// </summary>
        /// <param name="testResult"></param>
        /// <returns></returns>
        private static TestResult Migrate(TestResult testResult)
        {
            if (string.IsNullOrEmpty(testResult.ResultsFilePath))
            {
                return testResult;
            }

            var resultsDir = Path.Combine(Path.GetDirectoryName(testResult.AssemblyPath), Constants.ResultsDirectoryName);
            FileUtil.EnsureDirectory(resultsDir);
            var resultsFilePath = Path.Combine(resultsDir, Path.GetFileName(testResult.ResultsFilePath));
            File.Copy(testResult.ResultsFilePath, resultsFilePath, overwrite: true);

            return new TestResult(
                exitCode: testResult.ExitCode,
                assemblyPath: testResult.AssemblyName,
                resultsFilePath: resultsFilePath,
                commandLine: testResult.CommandLine,
                elapsed: testResult.Elapsed,
                standardOutput: testResult.StandardOutput,
                errorOutput: testResult.ErrorOutput);
        }
    }
}
