// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests.Cache
{
    internal sealed class CachingTestExecutor : ITestExecutor
    {
        private readonly ITestExecutor _testExecutor;
        private readonly ContentUtil _contentUtil;
        private readonly IDataStorage _dataStorage;

        internal CachingTestExecutor(Options options, ITestExecutor testExecutor, IDataStorage dataStorage)
        {
            _testExecutor = testExecutor;
            _dataStorage = dataStorage;
            _contentUtil = new ContentUtil(options);
        }

        public string GetCommandLine(string assemblyPath)
        {
            return _testExecutor.GetCommandLine(assemblyPath);
        }

        public async Task<TestResult> RunTestAsync(string assemblyPath, CancellationToken cancellationToken)
        {
            var contentFile = _contentUtil.GetTestResultContentFile(assemblyPath);
            var builder = new StringBuilder();
            builder.AppendLine($"{Path.GetFileName(assemblyPath)} - {contentFile.Checksum}");
            builder.AppendLine("===");
            builder.AppendLine(contentFile.Content);
            builder.AppendLine("===");
            Logger.Log(builder.ToString());

            try
            {
                var cachedTestResult = await _dataStorage.TryGetCachedTestResult(contentFile.Checksum);
                if (cachedTestResult.HasValue)
                {
                    Logger.Log($"{Path.GetFileName(assemblyPath)} - cache hit");
                    return Migrate(assemblyPath, cachedTestResult.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading cache {ex}");
            }

            Logger.Log($"{Path.GetFileName(assemblyPath)} - running");
            var testResult = await _testExecutor.RunTestAsync(assemblyPath, cancellationToken);
            await CacheTestResult(contentFile, testResult).ConfigureAwait(true);
            return testResult;
        }

        /// <summary>
        /// Recreate the on disk artifacts for the cached data and return the correct <see cref="TestResult"/>
        /// value.
        /// </summary>
        private TestResult Migrate(string assemblyPath, CachedTestResult cachedTestResult)
        {
            var resultsDir = Path.Combine(Path.GetDirectoryName(assemblyPath), Constants.ResultsDirectoryName);
            FileUtil.EnsureDirectory(resultsDir);
            var resultsFilePath = Path.Combine(resultsDir, cachedTestResult.ResultsFileName);
            File.WriteAllText(resultsFilePath, cachedTestResult.ResultsFileContent);
            var commandLine = _testExecutor.GetCommandLine(assemblyPath);

            return new TestResult(
                exitCode: cachedTestResult.ExitCode,
                assemblyPath: assemblyPath,
                resultDir: resultsDir,
                resultsFilePath: resultsFilePath,
                commandLine: commandLine,
                elapsed: TimeSpan.FromMilliseconds(0),
                standardOutput: cachedTestResult.StandardOutput,
                errorOutput: cachedTestResult.ErrorOutput,
                isResultFromCache: true);
        }

        private async Task CacheTestResult(ContentFile contentFile, TestResult testResult)
        {
            try
            {
                var resultFileContent = File.ReadAllText(testResult.ResultsFilePath);
                var cachedTestResult = new CachedTestResult(
                    exitCode: testResult.ExitCode,
                    standardOutput: testResult.StandardOutput,
                    errorOutput: testResult.ErrorOutput,
                    resultsFileName: Path.GetFileName(testResult.ResultsFilePath),
                    resultsFileContent: resultFileContent,
                    ellapsed: testResult.Elapsed);
                await _dataStorage.AddCachedTestResult(contentFile, cachedTestResult).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to create cached {ex}");
            }
        }
    }
}
