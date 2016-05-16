// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal sealed class WebDataStorage : IDataStorage
    {
        private const string NameExitCode = "exitCode";
        private const string NameOutputStandard = "outputStandard";
        private const string NameOutputError = "outputError";
        private const string NameResultsFileName = "resultsFileName";
        private const string NameResultsFileContent = "resultsFileContent";
        private const string NameElapsedSeconds = "elapsedSeconds";
        private const string NameTestPassed = "testPassed";
        private const string NameTestFailed = "testFailed";
        private const string NameTestSkipped = "testSkipped";

        private readonly RestClient _restClient = new RestClient(Constants.DashboardUriString);

        public string Name => "web";

        public async Task AddCachedTestResult(AssemblyInfo assemblyInfo, ContentFile contentFile, CachedTestResult testResult)
        {
            try
            {
                var obj = new JObject();
                obj["TestResultData"] = CreateTestResultData(assemblyInfo.ResultsFileName, testResult);
                obj["TestSourceData"] = CreateTestSourceData(assemblyInfo);

                var request = new RestRequest($"api/testcache/{contentFile.Checksum}");
                request.Method = Method.PUT;
                request.RequestFormat = DataFormat.Json;
                request.AddParameter("text/json", obj.ToString(), ParameterType.RequestBody);

                var response = await _restClient.ExecuteTaskAsync(request);
                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    Logger.Log($"Error adding web cached result: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception adding web cached result:  {ex}");
            }
        }

        public async Task<CachedTestResult?> TryGetCachedTestResult(string checksum)
        {
            try
            {
                var request = new RestRequest($"api/testcache/{checksum}");

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

                var obj = JObject.Parse(response.Content);
                var result = new CachedTestResult(
                    exitCode: obj.Value<int>(NameExitCode),
                    standardOutput: obj.Value<string>(NameOutputStandard),
                    errorOutput: obj.Value<string>(NameOutputError),
                    resultsFileContent: obj.Value<string>(NameResultsFileContent),
                    elapsed: TimeSpan.FromSeconds(obj.Value<int>(NameElapsedSeconds)));
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception retrieving cached test result {checksum}: {ex}");
                return null;
            }
        }

        private static JObject CreateTestResultData(string resultsFileName, CachedTestResult testResult)
        {
            var numbers = GetTestNumbers(resultsFileName, testResult) ?? Tuple.Create(-1, -1, -1);
            var obj = new JObject();
            obj[NameExitCode] = testResult.ExitCode;
            obj[NameOutputStandard] = testResult.StandardOutput;
            obj[NameOutputError] = testResult.ErrorOutput;
            obj[NameResultsFileName] = resultsFileName;
            obj[NameResultsFileContent] = testResult.ResultsFileContent;
            obj[NameElapsedSeconds] = (int)testResult.Elapsed.TotalSeconds;
            obj[NameTestPassed] = numbers.Item1;
            obj[NameTestFailed] = numbers.Item2;
            obj[NameTestSkipped] = numbers.Item3;

            return obj;
        }

        private JObject CreateTestSourceData(AssemblyInfo assemblyInfo)
        {
            var obj = new JObject();
            obj["MachineName"] = Environment.MachineName;
            obj["TestRoot"] = "";
            obj["AssemblyName"] = assemblyInfo.DisplayName;
            obj["IsJenkins"] = Constants.IsJenkinsRun;
            return obj;
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
                Logger.Log($"Exception reading test numbers: {ex}");
                return null;
            }
        }
    }
}
