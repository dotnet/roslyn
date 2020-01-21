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
    internal partial class WebDataStorage
    {
        internal sealed class TestResultData
        {
            [JsonProperty(Required = Required.Always)]
            public int ExitCode { get; set; }
            [JsonProperty(Required = Required.Always)]
            public string OutputStandard { get; set; }
            [JsonProperty(Required = Required.AllowNull)]
            public string OutputError { get; set; }
            public string ResultsFileName { get; set; }
            [JsonProperty(Required = Required.AllowNull)]
            public string ResultsFileContent { get; set; }
            public int ElapsedSeconds { get; set; }
            public int TestPassed { get; set; }
            public int TestFailed { get; set; }
            public int TestSkipped { get; set; }
        }

        internal sealed class TestSourceData
        {
            public string MachineName { get; set; }
            public string EnlistmentRoot { get; set; }
            public string AssemblyName { get; set; }
            public string Source { get; set; }
            public bool IsJenkins { get; set; }
            public string CommitSha { get; set; }
            public string MergeCommitSha { get; set; }
            public string Repository { get; set; }
            public bool IsPullRequest { get; set; }
            public int PullRequestId { get; set; }
            public string PullRequestUserName { get; set; }
        }

        internal sealed class TestCacheData
        {
            public TestResultData TestResultData { get; set; }
            public TestSourceData TestSourceData { get; set; }
        }
    }
}
