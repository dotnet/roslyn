// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using Xunit.Harness;
    using Xunit.v3;

    public sealed class IdeTestCase : IdeTestCaseBase
    {
        public IdeTestCase(IXunitTestMethod testMethod, VisualStudioInstanceKey visualStudioInstanceKey, object?[]? testMethodArguments = null)
            : base(testMethod, visualStudioInstanceKey, includeRootSuffixInDisplayName: false, testMethodArguments)
        {
        }
    }
}
