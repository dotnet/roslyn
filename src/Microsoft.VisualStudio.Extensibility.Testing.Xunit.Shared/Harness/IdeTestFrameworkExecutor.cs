// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public class IdeTestFrameworkExecutor : XunitTestFrameworkExecutor
    {
        public IdeTestFrameworkExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink)
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        {
        }

        [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Follows pattern expected by Xunit framework.")]
        protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            try
            {
                using (var assemblyRunner = new IdeTestAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions))
                {
                    await assemblyRunner.RunAsync();
                }
            }
            catch
            {
            }
        }
    }
}
