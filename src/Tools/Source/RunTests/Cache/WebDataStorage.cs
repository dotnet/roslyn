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
        private const string DashboardUriString = "http://jdash.azurewebsites.net";

        private readonly RestClient _restClient = new RestClient(DashboardUriString);

        public async Task AddCachedTestResult(ContentFile contentFile, CachedTestResult testResult)
        {
            var obj = new JObject();
            obj[NameExitCode] = testResult.ExitCode;
            obj[NameOutputStandard] = testResult.StandardOutput;
            obj[NameOutputStandard] = testResult.ErrorOutput;
            obj[NameResultsFileName] = testResult.ResultsFileName;
            obj[NameResultsFileContent] = testResult.ResultsFileContent;

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
                    resultsFileName: obj.Value<string>(NameResultsFileName),
                    resultsFileContent: obj.Value<string>(NameResultsFileContent));
                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
