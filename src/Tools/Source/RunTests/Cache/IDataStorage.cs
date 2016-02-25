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
        bool TryGetCachedTestResult(string checksum, out CachedTestResult testResult);

        void AddCachedTestResult(ContentFile conentFile, CachedTestResult testResult);
    }

    internal struct CachedTestResult
    {
        internal int ExitCode { get; }
        internal string StandardOutput { get; }
        internal string ErrorOutput { get; }
        internal string ResultsFileName { get; }
        internal string ResultsFileContent { get; }

        internal CachedTestResult(
            int exitCode,
            string standardOutput,
            string errorOutput,
            string resultsFileName,
            string resultsFileContent)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            ErrorOutput = errorOutput;
            ResultsFileName = resultsFileName;
            ResultsFileContent = resultsFileContent;
        }
    }
}

