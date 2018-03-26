﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using RunTests.Cache;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    internal struct TestExecutionOptions
    {
        internal string XunitPath { get; }
        internal ProcDumpInfo? ProcDumpInfo { get; }
        internal string LogsDirectory { get; }
        internal string Trait { get; }
        internal string NoTrait { get; }
        internal bool UseHtml { get; }
        internal bool Test64 { get; }
        internal bool TestVsi { get; }
        
        internal TestExecutionOptions(string xunitPath, ProcDumpInfo? procDumpInfo, string logsDirectory, string trait, string noTrait, bool useHtml, bool test64, bool testVsi)
        {
            XunitPath = xunitPath;
            ProcDumpInfo = procDumpInfo;
            LogsDirectory = logsDirectory;
            Trait = trait;
            NoTrait = noTrait;
            UseHtml = useHtml;
            Test64 = test64;
            TestVsi = testVsi;
        }
    }

    /// <summary>
    /// The actual results from running the xunit tests.
    /// </summary>
    /// <remarks>
    /// The difference between <see cref="TestResultInfo"/>  and <see cref="TestResult"/> is the former 
    /// is specifically for the actual test execution results while the latter can contain extra metadata
    /// about the results.  For example whether it was cached, or had diagonstic, output, etc ...
    /// </remarks>
    internal struct TestResultInfo
    {
        internal int ExitCode { get; }
        internal TimeSpan Elapsed { get; }
        internal string StandardOutput { get; }
        internal string ErrorOutput { get; }

        /// <summary>
        /// Path to the results file.  Can be null in the case xunit error'd and did not create one.
        /// </summary>
        internal string ResultsFilePath { get; }
        internal string ResultsDirectory { get; }

        internal TestResultInfo(int exitCode, string resultsDirectory, string resultsFilePath, TimeSpan elapsed, string standardOutput, string errorOutput)
        {
            ExitCode = exitCode;
            ResultsDirectory = resultsDirectory;
            ResultsFilePath = resultsFilePath;
            Elapsed = elapsed;
            StandardOutput = standardOutput;
            ErrorOutput = errorOutput;
        }
    }

    internal struct TestResult
    {
        internal TestResultInfo TestResultInfo { get; }
        internal AssemblyInfo AssemblyInfo { get; }
        internal string CommandLine { get; }
        internal bool IsFromCache { get; }
        internal string Diagnostics { get; }

        internal string AssemblyPath => AssemblyInfo.AssemblyPath;
        internal string AssemblyName => Path.GetFileName(AssemblyPath);
        internal string DisplayName => AssemblyInfo.DisplayName;
        internal bool Succeeded => ExitCode == 0;
        internal int ExitCode => TestResultInfo.ExitCode;
        internal TimeSpan Elapsed => TestResultInfo.Elapsed;
        internal string StandardOutput => TestResultInfo.StandardOutput;
        internal string ErrorOutput => TestResultInfo.ErrorOutput;
        internal string ResultsFilePath => TestResultInfo.ResultsFilePath;
        internal string ResultsDirectory => TestResultInfo.ResultsDirectory;

        internal TestResult(AssemblyInfo assemblyInfo, TestResultInfo testResultInfo, string commandLine, bool isFromCache, string diagnostics = null)
        {
            AssemblyInfo = assemblyInfo;
            TestResultInfo = testResultInfo;
            CommandLine = commandLine;
            IsFromCache = isFromCache;
            Diagnostics = diagnostics;
        }
    }

    internal interface ITestExecutor
    {
        IDataStorage DataStorage { get; }

        string GetCommandLine(AssemblyInfo assemblyInfo);

        Task<TestResult> RunTestAsync(AssemblyInfo assemblyInfo, CancellationToken cancellationToken);
    }
}
