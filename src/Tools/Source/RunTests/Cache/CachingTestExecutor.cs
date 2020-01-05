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

        public IDataStorage DataStorage => _dataStorage;
        public TestExecutionOptions Options => _testExecutor.Options;

        internal CachingTestExecutor(ITestExecutor testExecutor, IDataStorage dataStorage)
        {
            _testExecutor = testExecutor;
            _dataStorage = dataStorage;
            _contentUtil = new ContentUtil(_testExecutor.Options);
        }

        public string GetCommandLine(AssemblyInfo assemblyInfo)
        {
            return _testExecutor.GetCommandLine(assemblyInfo);
        }

        public async Task<TestResult> RunTestAsync(AssemblyInfo assemblyInfo, CancellationToken cancellationToken)
        {
            ContentFile contentFile;
            try
            {
                contentFile = _contentUtil.GetTestResultContentFile(assemblyInfo);
            }
            catch (Exception ex)
            {
                var msg = $"Unable to calculate content file for {assemblyInfo.AssemblyPath}";
                Logger.LogError(ex, msg + Environment.NewLine + ex.Message);
                contentFile = null;

                var testResult = await _testExecutor.RunTestAsync(assemblyInfo, cancellationToken);
                return new TestResult(
                    testResult.AssemblyInfo,
                    testResult.TestResultInfo,
                    testResult.CommandLine,
                    isFromCache: false,
                    diagnostics: msg);
            }

            return await RunTestWithCachingAsync(assemblyInfo, contentFile, cancellationToken);
        }

        private async Task<TestResult> RunTestWithCachingAsync(AssemblyInfo assemblyInfo, ContentFile contentFile, CancellationToken cancellationToken)
        {
            var assemblyPath = assemblyInfo.AssemblyPath;
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
                    return Migrate(assemblyInfo, cachedTestResult.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading cache {ex}");
            }

            Logger.Log($"{Path.GetFileName(assemblyPath)} - running");
            var testResult = await _testExecutor.RunTestAsync(assemblyInfo, cancellationToken);
            await CacheTestResult(contentFile, testResult).ConfigureAwait(true);
            return testResult;
        }

        private string GetResultsFilePath(AssemblyInfo assemblyInfo)
            => Path.Combine(_testExecutor.Options.OutputDirectory, assemblyInfo.ResultsFileName);

        /// <summary>
        /// Recreate the on disk artifacts for the cached data and return the correct <see cref="TestResult"/>
        /// value.
        /// </summary>
        private TestResult Migrate(AssemblyInfo assemblyInfo, CachedTestResult cachedTestResult)
        {
            var resultsFilePath = GetResultsFilePath(assemblyInfo);
            FileUtil.EnsureDirectory(Path.GetDirectoryName(resultsFilePath));
            File.WriteAllText(resultsFilePath, cachedTestResult.ResultsFileContent);
            var testResultInfo = new TestResultInfo(
                exitCode: cachedTestResult.ExitCode,
                resultsFilePath: resultsFilePath,
                elapsed: TimeSpan.FromMilliseconds(0),
                standardOutput: cachedTestResult.StandardOutput,
                errorOutput: cachedTestResult.ErrorOutput);

            var commandLine = _testExecutor.GetCommandLine(assemblyInfo);
            return new TestResult(
                assemblyInfo,
                testResultInfo,
                commandLine,
                isFromCache: true);
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
                    resultsFileContent: resultFileContent,
                    elapsed: testResult.Elapsed);
                await _dataStorage.AddCachedTestResult(testResult.AssemblyInfo, contentFile, cachedTestResult).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to create cached", ex);
            }
        }
    }
}
