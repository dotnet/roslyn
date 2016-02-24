// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests.Cache
{
    internal sealed class WebDataStorage : IDataStorage
    {
        private const string NameExitCode = "exitCode";
        private const string NameOutputStandard = "outputStandard";
        private const string NameOutputError = "outputError";
        private const string NameResultsFileName = "resultsFileName";
        private const string NameResultsFileContent = "resultsFileContent";
        private const string DashboardUriString = "http://jdash.azurewebsites.net";

        private readonly RestClient _restClient = new RestClient(DashboardUriString);

        public Task AddCachedTestResult(ContentFile conentFile, CachedTestResult testResult)
        {
            var obj = new JObject();
            obj[NameExitCode] = testResult.ExitCode;
            obj[NameOutputStandard] = testResult.StandardOutput;
            obj[NameOutputStandard] = testResult.ErrorOutput;
            obj[NameResultsFileName] = testResult.ResultsFileName;
            obj[NameResultsFileContent] = testResult.ResultsFileContent;

            var json = obj.ToString();
            return Task.FromResult(true);
        }

        public async Task<CachedTestResult?> TryGetCachedTestResult(string checksum)
        {
            var request = new RestRequest($"api/testcache/{checksum}");
            var response = await _restClient.ExecuteGetTaskAsync(request);
            var obj = JObject.Parse(response.Content);
            var result = new CachedTestResult(
                exitCode: obj.Value<int>(NameExitCode),
                standardOutput: obj.Value<string>(NameOutputStandard),
                errorOutput: obj.Value<string>(NameOutputError),
                resultsFileName: obj.Value<string>(NameResultsFileName),
                resultsFileContent: obj.Value<string>(NameResultsFileContent));
            return result;
        }
    }
}
