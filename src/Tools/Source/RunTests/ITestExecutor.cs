// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using RunTests.Cache;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    internal struct TestResult
    {
        internal int ExitCode { get; }
        internal AssemblyInfo AssemblyInfo { get; }
        internal string AssemblyPath => AssemblyInfo.AssemblyPath;
        internal string AssemblyName => Path.GetFileName(AssemblyPath);
        internal string DisplayName => AssemblyInfo.DisplayName;
        internal string CommandLine { get; }
        internal TimeSpan Elapsed { get; }
        internal string StandardOutput { get; }
        internal string ErrorOutput { get; }
        internal bool IsResultFromCache { get; }

        /// <summary>
        /// Path to the results file.  Can be null in the case xunit error'd and did not create one. 
        /// </summary>
        internal string ResultsFilePath { get; }

        internal string ResultDir { get; }
        internal bool Succeeded => ExitCode == 0;

        internal TestResult(int exitCode, AssemblyInfo assemblyInfo, string resultDir, string resultsFilePath, string commandLine, TimeSpan elapsed, string standardOutput, string errorOutput, bool isResultFromCache)
        {
            ExitCode = exitCode;
            AssemblyInfo = assemblyInfo;
            CommandLine = commandLine;
            ResultDir = resultDir;
            ResultsFilePath = resultsFilePath;
            Elapsed = elapsed;
            StandardOutput = standardOutput;
            ErrorOutput = errorOutput;
            IsResultFromCache = isResultFromCache;
        }
    }

    internal interface ITestExecutor
    {
        IDataStorage DataStorage { get; }

        string GetCommandLine(AssemblyInfo assemblyInfo);

        Task<TestResult> RunTestAsync(AssemblyInfo assemblyInfo, CancellationToken cancellationToken);
    }
}
