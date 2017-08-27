// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RunTests.Cache
{
    internal sealed partial class WebDataStorage : IDataStorage
    {
        private readonly RestClient _restClient = new RestClient(Constants.DashboardUriString);

        public string Name => "web";

        public async Task AddCachedTestResult(AssemblyInfo assemblyInfo, ContentFile contentFile, CachedTestResult testResult)
        {
            try
            {
                var testCacheData = CreateTestCacheData(assemblyInfo, assemblyInfo.ResultsFileName, testResult);
                Logger.Log($"Source data for ${assemblyInfo.DisplayName}: {JsonConvert.SerializeObject(testCacheData.TestSourceData, Formatting.Indented)}");

                var request = new RestRequest($"api/testData/cache/{contentFile.Checksum}");
                request.Method = Method.PUT;
                request.RequestFormat = DataFormat.Json;
                request.AddParameter("text/json", JsonConvert.SerializeObject(testCacheData), ParameterType.RequestBody);

                var response = await _restClient.ExecuteTaskAsync(request);
                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    Logger.Log($"Error adding web cached result: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception uploading cached test result");
            }
        }

        public async Task<CachedTestResult?> TryGetCachedTestResult(string checksum)
        {
            try
            {
                var request = new RestRequest($"api/testData/cache/{checksum}");

                // Add query parameters the web service uses for additional tracking
                request.AddParameter("machineName", Environment.MachineName, ParameterType.QueryString);
                request.AddParameter("enlistmentRoot", Constants.EnlistmentRoot, ParameterType.QueryString);

                if (Constants.IsJenkinsRun)
                {
                    request.AddParameter("source", "jenkins", ParameterType.QueryString);
                }

                var response = await _restClient.ExecuteGetTaskAsync(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return null;
                }

                var testCacheData = JsonConvert.DeserializeObject<TestResultData>(response.Content);
                var result = new CachedTestResult(
                    exitCode: testCacheData.ExitCode,
                    standardOutput: testCacheData.OutputStandard,
                    errorOutput: testCacheData.OutputError,
                    resultsFileContent: testCacheData.ResultsFileContent,
                    elapsed: TimeSpan.FromSeconds(testCacheData.ElapsedSeconds));
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Exception downloading cached test result for {checksum}");
                return null;
            }
        }

        private static TestCacheData CreateTestCacheData(AssemblyInfo assemblyInfo, string resultsFileName, CachedTestResult testResult)
        {
            return new TestCacheData()
            {
                TestResultData = CreateTestResultData(resultsFileName, testResult),
                TestSourceData = CreateTestSourceData(assemblyInfo)
            };
        }

        private static TestResultData CreateTestResultData(string resultsFileName, CachedTestResult testResult)
        {
            var numbers = GetTestNumbers(resultsFileName, testResult) ?? Tuple.Create(-1, -1, -1);
            return new TestResultData()
            {
                ExitCode = testResult.ExitCode,
                OutputStandard = testResult.StandardOutput,
                OutputError = testResult.ErrorOutput,
                ResultsFileName = resultsFileName,
                ResultsFileContent = testResult.ResultsFileContent,
                ElapsedSeconds = (int)testResult.Elapsed.TotalSeconds,
                TestPassed = numbers.Item1,
                TestFailed = numbers.Item2,
                TestSkipped = numbers.Item3
            };
        }

        private static TestSourceData CreateTestSourceData(AssemblyInfo assemblyInfo)
        {
            var data = new TestSourceData()
            {
                MachineName = Environment.MachineName,
                EnlistmentRoot = Constants.EnlistmentRoot,
                AssemblyName = assemblyInfo.DisplayName,
                IsJenkins = Constants.IsJenkinsRun
            };

            if (data.IsJenkins)
            {
                // Add the core git information
                data.MergeCommitSha = Environment.GetEnvironmentVariable("GIT_COMMIT");
                data.Repository = Environment.GetEnvironmentVariable("ghprbGhRepository");

                // For PR runs include extra data about the PR.  This enables us to track down bugs server 
                // side when PRs are believed to hit test / cache issues.
                var idStr = Environment.GetEnvironmentVariable("ghprbPullId");
                if (idStr != null && int.TryParse(idStr, out var id))
                {
                    data.IsPullRequest = true;
                    data.PullRequestId = id;
                    data.PullRequestUserName = Environment.GetEnvironmentVariable("ghprbPullAuthorLogin");
                    data.CommitSha = Environment.GetEnvironmentVariable("ghprbActualCommit");
                }
                else
                {
                    data.CommitSha = data.MergeCommitSha;
                }
            }

            return data;
        }

        private static Tuple<int, int, int> GetTestNumbers(string resultsFileName, CachedTestResult testResult)
        {
            if (!resultsFileName.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            try
            {
                using (var reader = new StringReader(testResult.ResultsFileContent))
                {
                    var document = XDocument.Load(reader);
                    var assembly = document.Element("assemblies").Element("assembly");
                    var passed = int.Parse(assembly.Attribute("passed").Value);
                    var failed = int.Parse(assembly.Attribute("failed").Value);
                    var skipped = int.Parse(assembly.Attribute("skipped").Value);
                    return Tuple.Create(passed, failed, skipped);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error reading test numbers");
                return null;
            }
        }
    }
}
