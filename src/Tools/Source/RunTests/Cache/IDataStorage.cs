// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests.Cache
{
    internal interface IDataStorage
    {
        Task<CachedTestResult?> TryGetCachedTestResult(string checksum);

        Task AddCachedTestResult(ContentFile conentFile, CachedTestResult testResult);
    }

    internal struct CachedTestResult
    {
        internal int ExitCode { get; }
        internal string StandardOutput { get; }
        internal string ErrorOutput { get; }
        internal string ResultsFileName { get; }
        internal string ResultsFileContent { get; }
        internal TimeSpan Ellapsed { get; }

        internal CachedTestResult(
            int exitCode,
            string standardOutput,
            string errorOutput,
            string resultsFileName,
            string resultsFileContent,
            TimeSpan ellapsed)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            ErrorOutput = errorOutput;
            ResultsFileName = resultsFileName;
            ResultsFileContent = resultsFileContent;
            Ellapsed = ellapsed;
        }
    }
}

