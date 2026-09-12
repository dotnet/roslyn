// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Harness
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Sdk;
    using Xunit.v3;

    public class IdeTestFrameworkExecutor : XunitTestFrameworkExecutor
    {
        public IdeTestFrameworkExecutor(IXunitTestAssembly testAssembly)
            : base(testAssembly)
        {
        }

        public override async ValueTask RunTestCases(
            IReadOnlyCollection<IXunitTestCase> testCases,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions,
            CancellationToken cancellationToken)
        {
            await IdeTestAssemblyRunner.Instance.RunAsync(TestAssembly, testCases, executionMessageSink, executionOptions, cancellationToken).ConfigureAwait(false);
        }
    }
}
