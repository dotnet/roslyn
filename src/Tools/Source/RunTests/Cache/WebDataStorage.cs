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

namespace RunTests.Cache
{
    internal sealed class WebDataStorage : IDataStorage
    {
        private const string NameExitCode = "ExitCode";
        private const string NameOutputStandard = "OutputStandard";
        private const string NameOutputError = "OutputError";
        private const string NameResultsFileName = "ResultsFileName";
        private const string NameResultsFileContent = "ResultsFileContent";
        private const string NameEllapsedSeconds = "EllapsedSeconds";

        private readonly RestClient _restClient = new RestClient(Constants.DashboardUriString);

        public string Name => "web";

        public async Task AddCachedTestResult(AssemblyInfo assemblyInfo, ContentFile contentFile, CachedTestResult testResult)
        {
            var obj = new JObject();
            obj["TestResultData"] = CreateTestResultData(assemblyInfo.ResultsFileName, testResult);
            obj["TestSourceData"] = CreateTestSourceData(assemblyInfo);

            var request = new RestRequest($"api/testcache/{contentFile.Checksum}");
            request.Method = Method.PUT;
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("text/json", obj.ToString(), ParameterType.RequestBody);
            var response = await _restClient.ExecuteTaskAsync(request);
        }

        public async Task<CachedTestResult?> TryGetCachedTestResult(string checksum)
        {
            try
            {
                var request = new RestRequest($"api/testcache/{checksum}");
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
                    ellapsed: TimeSpan.FromSeconds(obj.Value<int>(NameEllapsedSeconds)));
                return result;
            }
            catch
            {
                return null;
            }
        }

        private static JObject CreateTestResultData(string resultsFileName, CachedTestResult testResult)
        {
            // TODO: we should remove ResultsFileName from the web storage.  It's redundant data at this 
            // point.
            var obj = new JObject();
            obj[NameExitCode] = testResult.ExitCode;
            obj[NameOutputStandard] = testResult.StandardOutput;
            obj[NameOutputStandard] = testResult.ErrorOutput;
            obj[NameResultsFileName] = resultsFileName;
            obj[NameResultsFileContent] = testResult.ResultsFileContent;
            obj[NameEllapsedSeconds] = (int)testResult.Ellapsed.TotalSeconds;
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
    }
}
