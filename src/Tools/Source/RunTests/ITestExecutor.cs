// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace RunTests
{
    internal struct TestResult
    {
        internal readonly bool Succeeded;
        internal readonly string AssemblyName;
        internal readonly TimeSpan Elapsed;
        internal readonly string ErrorOutput;

        internal TestResult(bool succeeded, string assemblyName, TimeSpan elapsed, string errorOutput)
        {
            Succeeded = succeeded;
            AssemblyName = assemblyName;
            Elapsed = elapsed;
            ErrorOutput = errorOutput;
        }
    }

    internal interface ITestExecutor
    {
        Task<TestResult> RunTest(string assemblyPath, CancellationToken cancellationToken);
    }
}
