// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    internal struct TestResult
    {
        internal int ExitCode { get; }
        internal string AssemblyPath { get; }
        internal string AssemblyName { get; }
        internal string CommandLine { get; }
        internal TimeSpan Elapsed { get; }
        internal string StandardOutput { get; }
        internal string ErrorOutput { get; }

        /// <summary>
        /// Path to the results file.  Can be null in the case xunit error'd and did not create one. 
        /// </summary>
        internal string ResultsFilePath { get; }

        internal string ResultDir { get; }
        internal bool Succeeded => ExitCode == 0;

        internal TestResult(int exitCode, string assemblyPath, string resultDir, string resultsFilePath, string commandLine, TimeSpan elapsed, string standardOutput, string errorOutput)
        {
            ExitCode = exitCode;
            AssemblyName = Path.GetFileName(assemblyPath);
            AssemblyPath = assemblyPath;
            CommandLine = commandLine;
            ResultDir = resultDir;
            ResultsFilePath = resultsFilePath;
            Elapsed = elapsed;
            StandardOutput = standardOutput;
            ErrorOutput = errorOutput;
        }
    }

    internal interface ITestExecutor
    {
        Task<TestResult> RunTestAsync(string assemblyPath, CancellationToken cancellationToken);
    }
}
