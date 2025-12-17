// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            using (var assemblyRunner = new IdeTestAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions))
            {
                await assemblyRunner.RunAsync().ConfigureAwait(true);
            }
        }
    }
}
