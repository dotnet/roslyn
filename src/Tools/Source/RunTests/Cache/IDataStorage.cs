﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests.Cache
{
    internal interface IDataStorage
    {
        string Name { get; }

        Task<CachedTestResult?> TryGetCachedTestResult(string checksum);

        Task AddCachedTestResult(AssemblyInfo assemblyInfo, ContentFile conentFile, CachedTestResult testResult);
    }

    internal struct CachedTestResult
    {
        internal int ExitCode { get; }
        internal string StandardOutput { get; }
        internal string ErrorOutput { get; }
        internal string ResultsFileContent { get; }
        internal TimeSpan Elapsed { get; }

        internal CachedTestResult(
            int exitCode,
            string standardOutput,
            string errorOutput,
            string resultsFileContent,
            TimeSpan elapsed)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            ErrorOutput = errorOutput;
            ResultsFileContent = resultsFileContent;
            Elapsed = elapsed;
        }
    }
}

